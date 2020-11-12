using System;
using System.Collections.Generic;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using System.Threading;
using System.Drawing;
using Emgu.CV;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Windows.Forms;
using Emgu.CV.Shape;
using System.Reflection;

namespace FetchRig6
{
    public class OryxCamera
    {
        

        private CamStreamManager manager;
        private ConcurrentQueue<ButtonCommands> messageQueue;
        private ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamOutputQueue;
        private bool isEncodeable;

        // These fields will be accessed by an OryxCameraSettings object to set and save camera settings.
        public readonly int camNumber;
        public readonly string sessionPath;
        public IManagedCamera managedCamera;
        public Util.OryxSetupInfo setupInfo;
        public INodeMap nodeMapTLDevice;
        public INodeMap nodeMapTLStream;
        public INodeMap nodeMap;

        public OryxCamera(int camNumber, IManagedCamera managedCamera, CamStreamManager manager, Util.OryxSetupInfo setupInfo)
        {
            this.camNumber = camNumber;
            this.managedCamera = managedCamera;
            this.manager = manager;
            this.setupInfo = setupInfo;
            messageQueue = this.manager.messageQueue;
            sessionPath = this.manager.sessionPath;
            isEncodeable = (this.manager.input.isEncodable || this.manager.output.isEncodable) ? true : false;

            GetNodeMapsAndInitialize();
            LoadCameraSettings();

            StreamController.BasicStreamController controller = new StreamController.BasicStreamController(oryxCamera: this);
            controller.Run();
        }

        private void GetNodeMapsAndInitialize()
        {
            nodeMapTLDevice = managedCamera.GetTLDeviceNodeMap();
            nodeMapTLStream = managedCamera.GetTLStreamNodeMap();
            managedCamera.Init();
            nodeMap = managedCamera.GetNodeMap();
            Console.WriteLine("Camera number {0} opened and initialized on thread {1}", camNumber, Thread.CurrentThread.ManagedThreadId);
        }

        private void LoadCameraSettings()
        {
            OryxCameraSettings oryxCameraSettings = new OryxCameraSettings(this);
            oryxCameraSettings.SaveSettings(_printSettings: false);
        }


        public class MessageHandler
        {
            public MessageHandlerStyle style;
            public int nStates;
            public int nButtons;

            public class BasicMessageHandler : MessageHandler
            {
                BasicCamLoopState[,] nextState;
                public BasicMessageHandler(MessageHandlerStyle style)
                {
                    this.style = style;
                    nStates = Enum.GetValues(typeof(BasicCamLoopState)).Length;
                    nButtons = Enum.GetValues(typeof(ButtonCommands)).Length;
                    nextState = new BasicCamLoopState[nStates, nButtons];

                    GetStateTable();
                }

                void GetStateTable()
                {
                    if (style == MessageHandlerStyle.Basic)
                    {
                        nextState[(int)BasicCamLoopState.Waiting, (int)ButtonCommands.BeginAcquisition] = BasicCamLoopState.Streaming;
                        nextState[(int)BasicCamLoopState.Waiting, (int)ButtonCommands.BeginStreaming] = BasicCamLoopState.Streaming;
                        nextState[(int)BasicCamLoopState.Waiting, (int)ButtonCommands.Exit] = BasicCamLoopState.Exit;

                        nextState[(int)BasicCamLoopState.Streaming, (int)ButtonCommands.EndStreaming] = BasicCamLoopState.Waiting;
                        nextState[(int)BasicCamLoopState.Streaming, (int)ButtonCommands.Exit] = BasicCamLoopState.Exit;
                    }
                }

                public BasicCamLoopState UpdateState(BasicCamLoopState state, ButtonCommands button)
                {
                    int i = (int)state;
                    int j = (int)button;
                    return nextState[i, j];
                }
            }
        }

        public class StreamController
        {
            public int imageCtr;
            public CamStreamManager manager;
            public ConcurrentQueue<ButtonCommands> messageQueue;
            public IManagedCamera managedCamera;
            public FrameMetaData prevFrameMetaData;
            public FrameMetaData currFrameMetaData;
            public bool isMessageDequeueSuccess;

            public class BasicStreamController : StreamController
            {
                private Size inputSize;
                private Size outputSize;
                private int enqueueRate;
                private BasicCamLoopState state;
                private ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamQueue;
                private bool isResizeNeeded;
                private MessageHandler.BasicMessageHandler handler; 

                public BasicStreamController(OryxCamera oryxCamera)
                {
                    manager = oryxCamera.manager;
                    if (manager.output.nChannels != 1) { throw new Exception(message: "BasicStreamInfo accommodates only one output channel!"); }

                    messageQueue = oryxCamera.messageQueue;
                    managedCamera = oryxCamera.managedCamera;
                    inputSize = manager.input.inputChannel.imageSize;
                    streamQueue = manager.output.streamQueue;
                    outputSize = manager.output.outputChannels[0].imageSize;
                    enqueueRate = manager.output.outputChannels[0].enqueueOrDequeueRate;
                    isResizeNeeded = !Equals(objA: manager.input.inputChannel.imageSize, objB: manager.output.outputChannels[0].imageSize);
                    handler = new MessageHandler.BasicMessageHandler(style: MessageHandlerStyle.Basic);
                    state = BasicCamLoopState.Waiting;
                }

                public void ResetCounter()
                {
                    imageCtr = 0;
                    prevFrameMetaData = new FrameMetaData();
                    currFrameMetaData = new FrameMetaData();
                }

                public void Run()
                {
                    while (true)
                    {
                        if (state == BasicCamLoopState.Waiting)
                        {
                            isMessageDequeueSuccess = messageQueue.TryDequeue(out ButtonCommands message);
                            if (isMessageDequeueSuccess)
                            {
                                state = handler.UpdateState(state: state, button: message);
                                
                                if (state == BasicCamLoopState.Streaming && !managedCamera.IsStreaming())
                                {
                                    ResetCounter();
                                    managedCamera.BeginAcquisition();
                                }
                                continue;
                            }

                            // TODO: replace this waiting mechanism with a thread-synchronized timer
                            Thread.Sleep(100);
                            continue;
                        }

                        else if (state == BasicCamLoopState.Streaming)
                        {
                            try
                            {
                                using (IManagedImage rawImage = managedCamera.GetNextImage())
                                {
                                    imageCtr += 1;
                                    long frameID = rawImage.ChunkData.FrameID;
                                    long timestamp = rawImage.ChunkData.Timestamp;

                                    if (imageCtr == 1)
                                    {
                                        currFrameMetaData = new FrameMetaData(streamCtr: imageCtr, frameID: frameID, timestamp: timestamp);
                                        continue;
                                    }

                                    prevFrameMetaData = currFrameMetaData;
                                    currFrameMetaData = new FrameMetaData(streamCtr: imageCtr, frameID: frameID, timestamp: timestamp);

                                    if (imageCtr % enqueueRate == 0)
                                    {
                                        Mat fullMat = new Mat(rows: inputSize.Height, cols: inputSize.Width,
                                            type: Emgu.CV.CvEnum.DepthType.Cv8U, channels: 1, data: rawImage.DataPtr, step: inputSize.Width);

                                        if (isResizeNeeded)
                                        {
                                            Mat resizedMat = new Mat(size: outputSize, type: Emgu.CV.CvEnum.DepthType.Cv8U, channels: 1);
                                            CvInvoke.Resize(src: fullMat, dst: resizedMat, dsize: outputSize, interpolation: Emgu.CV.CvEnum.Inter.Linear);
                                            Tuple<Mat, FrameMetaData>[] output = Util.GetStreamOutput(mat: resizedMat, metaData: currFrameMetaData);
                                            streamQueue.Enqueue(item: output);
                                            fullMat.Dispose();
                                        }
                                        else
                                        {
                                            Tuple<Mat, FrameMetaData>[] output = Util.GetStreamOutput(mat: fullMat, metaData: currFrameMetaData);
                                            streamQueue.Enqueue(item: output);
                                        }
                                    }
                                }
                            }
                            catch (SpinnakerException ex)
                            {
                                Console.WriteLine("Error in SimpleStreamingLoop: {0}", ex.Message);
                            }

                            isMessageDequeueSuccess = messageQueue.TryDequeue(out ButtonCommands message);
                            if (isMessageDequeueSuccess) { state = handler.UpdateState(state: state, button: message); }
                        }

                        else if (state == BasicCamLoopState.Exit) { return; }
                    }
                }
            }
        }
    }
}

