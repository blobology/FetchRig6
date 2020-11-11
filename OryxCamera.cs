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

            BasicStreamController controller = new BasicStreamController(oryxCamera: this);
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


        

        public class StreamController
        {
            public int imageCtr;
            public FrameMetaData prevFrameMetaData;
            public FrameMetaData currFrameMetaData;
            public bool isMessageDequeueSuccess;
            public CamStreamManager manager;
            public ConcurrentQueue<ButtonCommands> messageQueue;
            public IManagedCamera managedCamera;
        }

        public class BasicStreamController : StreamController
        {
            private Size inputSize;
            private Size outputSize;
            private int enqueueRate;
            private BasicCamLoopState state;
            private ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamQueue;
            private bool isResizeNeeded;

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
                state = BasicCamLoopState.Waiting;
            }

            public void Reset()
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
                        if (message == ButtonCommands.BeginAcquisition)
                        {
                            managedCamera.BeginAcquisition();
                            continue;
                        }
                        else if (message == ButtonCommands.BeginStreaming)
                        {
                            if (!managedCamera.IsStreaming()) { managedCamera.BeginAcquisition(); }
                            Reset();
                            state = BasicCamLoopState.Streaming;
                            continue;
                        }
                        else if (message == ButtonCommands.Exit)
                        {
                            state = BasicCamLoopState.Exit;
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
                    }
                }
            }
        }
    }
}

