using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using Emgu.CV;
using Emgu.CV.UI;
using System.Windows.Forms;
using Emgu.CV.Structure;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Drawing.Text;
using SharpDX;
using System.Runtime.InteropServices;
using Emgu.CV.Tracking;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel.Design;
using System.Security.Policy;
using System.CodeDom;

namespace FetchRig6
{

    public enum StreamArchitecture
    {
        TwoLevel,
        ThreeLevelBasic
    }

    public enum ThreadType
    {
        Camera,
        SingleCameraStream,
        MergeStreams
    }

    public class StreamManager
    {
        public ThreadType threadType { get; set; }
        public string sessionPath { get; set; }
        public bool isEncodable { get; set; }

        public class InputOutputManager
        {
            public bool[] isEncodable { get; set; }
        }

        public class StreamChannel
        {
            public Size imageSize { get; }
            public int enqueueOrDequeueRate { get; }
            public bool isEncodeable { get; }
            public int encodeRate { get; }
            public VideoEncoder videoEncoder;

            public StreamChannel(Size imageSize, int enqueueOrDequeueRate=1)
            {
                this.imageSize = imageSize;
                this.enqueueOrDequeueRate = enqueueOrDequeueRate;
                isEncodeable = false;
                encodeRate = 0;
                videoEncoder = null;
            }
            public StreamChannel(Size imageSize, int enqueueOrDequeueRate, int encodeRate)
            {
                this.imageSize = imageSize;
                this.enqueueOrDequeueRate = enqueueOrDequeueRate;
                isEncodeable = true;
                this.encodeRate = encodeRate;
                videoEncoder = null;
            }
        }
    }

    public class CamStreamManager : StreamManager
    {
        public ConcurrentQueue<ButtonCommands> messageQueue;
        public Input input { get; set; }
        public Output output { get; set; }

        public CamStreamManager(ConcurrentQueue<ButtonCommands> messageQueue, string sessionPath, CamStreamManagerSetup setupStyle)
        {
            this.messageQueue = messageQueue;
            this.sessionPath = sessionPath;
            threadType = ThreadType.Camera;

            if (setupStyle == CamStreamManagerSetup.BasicStream)
            {
                // Input Manager Setup:
                Size _camFrameSize = new Size(3208, 2200);
                StreamChannel inputChannel = new StreamChannel(imageSize: _camFrameSize, enqueueOrDequeueRate: 1);
                input = new Input(inputChannel: inputChannel);

                // Output Manager Setup:
                Size _outputChannelSize = new Size(802, 550);
                StreamChannel[] _outputChannels = new StreamChannel[] { new StreamChannel(imageSize: _outputChannelSize, enqueueOrDequeueRate: 2) };
                output = new Output(outputChannels: _outputChannels);
                isEncodable = false;
            }

            else if (setupStyle == CamStreamManagerSetup.BasicStreamAndRecord)
            {
                throw new ArgumentException(message: "Currently unsupported CamStreamManagerSetup Style");
            }

            else
            {
                throw new ArgumentException(message: "Currently unsupported CamStreamManagerSetup Style");
            }
        }

        public CamStreamManager(StreamChannel inputChannel, StreamChannel[] outputChannels, ConcurrentQueue<ButtonCommands> messageQueue, string sessionPath)
        {
            this.messageQueue = messageQueue;
            this.sessionPath = sessionPath;
            threadType = ThreadType.Camera;

            // Input Manager Setup:
            input = new Input(inputChannel: inputChannel);

            // Output Manager Setup:
            output = new Output(outputChannels: outputChannels);

            // Check Input and Output Managers for encodable channels
            isEncodable = Util.CheckForEncodables(input1: input.isEncodable, input2: output.isEncodable);
        }

        public class Input : InputOutputManager
        {
            public StreamChannel inputChannel { get; set; }
            public Input(StreamChannel inputChannel)
            {
                this.inputChannel = inputChannel;
                isEncodable = Util.CheckForEncodables(streamChannel: this.inputChannel);
            }
        }

        public class Output : InputOutputManager
        {
            public StreamChannel[] outputChannels { get; set; }
            public ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamQueue;
            public int nChannels { get; }
            public Output(StreamChannel[] outputChannels)
            {
                nChannels = outputChannels.Length;
                this.outputChannels = outputChannels;
                isEncodable = Util.CheckForEncodables(streamChannels: this.outputChannels);
                streamQueue = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>();
            }
        }

        public enum CamStreamManagerSetup
        {
            BasicStream,
            BasicStreamAndRecord,
            Advanced
        }
    }

    public class SingleStreamManager : StreamManager
    {
        public ConcurrentQueue<ButtonCommands> messageQueue;
        public Input input { get; set; }
        public Output output { get; set; }

        public SingleStreamManager(CamStreamManager.Output camOutputManager, ConcurrentQueue<ButtonCommands> messageQueue,
            string sessionPath, SingleStreamManangerSetup setupStyle)
        {
            this.messageQueue = messageQueue;
            this.sessionPath = sessionPath;

            if (setupStyle == SingleStreamManangerSetup.BasicStream)
            {
                Size _inputSize = new Size(802, 550);
                StreamChannel[] _inputChannel = new StreamChannel[1] { new StreamChannel(imageSize: _inputSize, enqueueOrDequeueRate: 1) };
                input = new Input(inputChannels: _inputChannel, streamQueue: camOutputManager.streamQueue);

                Size _outputChannelSize = new Size(width: 802, height: 550);
                StreamChannel[] _outputChannels = new StreamChannel[1] { new StreamChannel(imageSize: _outputChannelSize) };
                output = new Output(outputChannels: _outputChannels);
                isEncodable = false;
            }

            else if (setupStyle == SingleStreamManangerSetup.Advanced)
            {
                Size _inputSize = new Size(802, 550);
                StreamChannel[] _inputChannel = new StreamChannel[1] { new StreamChannel(imageSize: _inputSize, enqueueOrDequeueRate: 1) };
                input = new Input(inputChannels: _inputChannel, streamQueue: camOutputManager.streamQueue);

                Size _outputChannelSize = new Size(width: 802, height: 550);
                StreamChannel[] _outputChannels = new StreamChannel[2]
                {
                    new StreamChannel(imageSize: _outputChannelSize, enqueueOrDequeueRate: 1),
                    new StreamChannel(imageSize: _outputChannelSize, enqueueOrDequeueRate: 1)
                };
                output = new Output(outputChannels: _outputChannels);
                isEncodable = false;
            }

            else
            {
                throw new ArgumentException(message: "Currently unsupported SingleStreamManagerSetup Style");
            }
        }

        public SingleStreamManager(CamStreamManager.Output camOutputManager, ConcurrentQueue<ButtonCommands> messageQueue, string sessionPath)
        {
            this.messageQueue = messageQueue;
            this.sessionPath = sessionPath;

            input = new Input(inputChannels: camOutputManager.outputChannels, streamQueue: camOutputManager.streamQueue);

            Size _outputChannelSize = new Size(width: 802, height: 550);
            StreamChannel[] _outputChannels = new StreamChannel[] { new StreamChannel(imageSize: _outputChannelSize) };
            output = new Output(outputChannels: _outputChannels);
            isEncodable = Util.CheckForEncodables(input1: input.isEncodable, input2: output.isEncodable);
        }

        public class Input : InputOutputManager
        {
            public StreamChannel[] inputChannels { get; set; }
            public ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamQueue { get; }
            public int nChannels { get; set; }
            public Input(StreamChannel[] inputChannels, ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamQueue)
            {
                nChannels = inputChannels.Length;
                this.inputChannels = inputChannels;
                isEncodable = Util.CheckForEncodables(streamChannels: this.inputChannels);
                this.streamQueue = streamQueue;
            }
        }

        public class Output : InputOutputManager
        {
            public StreamChannel[] outputChannels { get; set; }
            public ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamQueue { get; }
            public int nChannels { get; }

            public Output(StreamChannel[] outputChannels)
            {
                nChannels = outputChannels.Length;
                this.outputChannels = outputChannels;
                isEncodable = Util.CheckForEncodables(streamChannels: this.outputChannels);
                streamQueue = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>();
            }
        }

        public enum SingleStreamManangerSetup
        {
            BasicStream,
            BasicStreamAndRecord,
            Advanced
        }
    }

    public class MergeStreamsManager : StreamManager
    {
        public ConcurrentQueue<ButtonCommands> messageQueue;
        public Input input { get; set; }
        public Output output { get; set; }

        public MergeStreamsManager(SingleStreamManager.Output[] singleStreamOutputManagers,
            ConcurrentQueue<ButtonCommands> messageQueue, string sessionPath, MergeStreamManangerSetup setupStyle)
        {
            this.messageQueue = messageQueue;
            this.sessionPath = sessionPath;

            if (setupStyle == MergeStreamManangerSetup.BasicStream)
            {
                StreamChannel[][] _inputChannels = new StreamChannel[2][];
                ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>[] _streamQueues = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>[2];
                for (int i = 0; i < 2; i++)
                {
                    _inputChannels[i] = singleStreamOutputManagers[i].outputChannels;
                    _streamQueues[i] = singleStreamOutputManagers[i].streamQueue;
                }
                input = new Input(inputChannels: _inputChannels, streamQueues: _streamQueues);

                // Specify output specs:
                Size _outputChannelSize = new Size(width: 802, height: 1100);
                StreamChannel[] _outputChannels = new StreamChannel[] { new StreamChannel(imageSize: _outputChannelSize, enqueueOrDequeueRate: 1) };
                output = new Output(outputChannels: _outputChannels);

                isEncodable = false;
            }
            else if (setupStyle == MergeStreamManangerSetup.Advanced)
            {
                StreamChannel[][] _inputChannels = new StreamChannel[2][];
                ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>[] _streamQueues = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>[2];
                for (int i = 0; i < 2; i++)
                {
                    _inputChannels[i] = singleStreamOutputManagers[i].outputChannels;
                    _streamQueues[i] = singleStreamOutputManagers[i].streamQueue;
                }
                input = new Input(inputChannels: _inputChannels, streamQueues: _streamQueues);

                // Specify output specs:
                Size _outputChannelSize = new Size(width: 802, height: 1100);
                StreamChannel[] _outputChannels = new StreamChannel[2]
                {
                    new StreamChannel(imageSize: _outputChannelSize, enqueueOrDequeueRate: 1),
                    new StreamChannel(imageSize: _outputChannelSize, enqueueOrDequeueRate: 1)
                };

                output = new Output(outputChannels: _outputChannels);
                isEncodable = false;
            }

            else
            {
                throw new ArgumentException(message: "Currently unsupported SingleStreamManagerSetup Style");
            }
        }

        public MergeStreamsManager(SingleStreamManager.Output[] singleStreamOutputManagers,
            ConcurrentQueue<ButtonCommands> messageQueue, string sessionPath)
        {
            this.messageQueue = messageQueue;
            this.sessionPath = sessionPath;

            int nInputs = singleStreamOutputManagers.Length;
            StreamChannel[][] _inputChannels = new StreamChannel[nInputs][];
            ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>[] _streamQueues = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>[nInputs];
            for (int i = 0; i < nInputs; i++)
            {
                _inputChannels[i] = singleStreamOutputManagers[i].outputChannels;
                _streamQueues[i] = singleStreamOutputManagers[i].streamQueue;
            }
            input = new Input(inputChannels: _inputChannels, streamQueues: _streamQueues);

            // Specify output specs:
            Size _outputChannelSize = new Size(width: 802, height: 1100);
            StreamChannel[] _outputChannels = new StreamChannel[] { new StreamChannel(imageSize: _outputChannelSize) };
            output = new Output(outputChannels: _outputChannels);

            isEncodable = Util.CheckForEncodables(input: output.isEncodable);
        }

        public class Input : InputOutputManager
        {
            public int nInputQueues { get; }
            public StreamChannel[][] inputChannels { get; set; }
            public ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>[] streamQueues { get; }
            public int[] nChannels { get; set; }
            public Input(StreamChannel[][] inputChannels, ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>[] streamQueues)
            {
                nInputQueues = inputChannels.Length;
                this.inputChannels = inputChannels;
                this.streamQueues = streamQueues;

                nChannels = new int[nInputQueues];
                for (int i = 0; i < nInputQueues; i++)
                {
                    nChannels[i] = inputChannels[i].Length;
                }

                // Disallow encoding of input streams on merge streams thread:
                isEncodable = null;
            }
        }

        public class Output : InputOutputManager
        {
            public StreamChannel[] outputChannels { get; set; }
            public ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> displayQueue { get; }
            public int nChannels { get; }

            public Output(StreamChannel[] outputChannels)
            {
                nChannels = outputChannels.Length;
                this.outputChannels = outputChannels;
                isEncodable = Util.CheckForEncodables(streamChannels: this.outputChannels);
                displayQueue = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>();
            }
        }

        public enum MergeStreamManangerSetup
        {
            BasicStream,
            BasicStreamAndRecord,
            Advanced
        }
    }

    public static class BasicStreamThreadSetups
    {
        public static void SingleStreamThreadInit(int idx, SingleStreamManager manager)
        {
            int imageCtr = 0;
            bool isResizeNeeded = false;
            Mat background = new Mat(size: manager.output.outputChannels[0].imageSize, type: Emgu.CV.CvEnum.DepthType.Cv8U, channels: 1);
            bool resetBackground = true;
            bool saveThisImage = false;
            bool isMessageDequeueSuccess;
            bool isInputDequeueSuccess;
            StreamLoopState state = StreamLoopState.Waiting;

            while (true)
            {
                isInputDequeueSuccess = manager.input.streamQueue.TryDequeue(out Tuple<Mat, FrameMetaData>[] input);
                if (isInputDequeueSuccess)
                {
                    ProcessInput();
                }

                UpdateLoopState();
                if (state == StreamLoopState.Streaming) { continue; }
                else if (state == StreamLoopState.Waiting) { Thread.Sleep(100); }
                else if (state == StreamLoopState.Exit) { return; }

                void ProcessInput()
                {
                    imageCtr += 1;
                    if (imageCtr % manager.input.inputChannels[0].enqueueOrDequeueRate != 0)
                    {
                        input[0].Item1.Dispose();
                        return;
                    }

                    if (isResizeNeeded)
                    {
                        // TODO: Implement resize operation
                    }

                    if (saveThisImage)
                    {
                        Console.WriteLine("SaveThisImage message received on SingleStreamThread");
                        saveThisImage = false;
                    }

                    if (resetBackground)
                    {
                        Console.WriteLine("ResetBackground message received on SingleStreamThread");
                        input[0].Item1.CopyTo(background);
                        resetBackground = false;
                        Console.WriteLine("resetBackground = false");
                    }

                    if (imageCtr % manager.output.outputChannels[0].enqueueOrDequeueRate != 0)
                    {
                        input[0].Item1.Dispose();
                        return;
                    }
                    else
                    {
                        manager.output.streamQueue.Enqueue(item: input);
                    }
                }

                void UpdateLoopState()
                {
                    isMessageDequeueSuccess = manager.messageQueue.TryDequeue(out ButtonCommands message);
                    if (isMessageDequeueSuccess)
                    {
                        if (message == ButtonCommands.BeginStreaming)
                        {
                            Console.WriteLine("BeginStreaming command received in SingleStreamThread");
                            state = StreamLoopState.Streaming;
                        }
                        else if (message == ButtonCommands.ResetBackgroundImage)
                        {
                            resetBackground = true;
                        }
                        else if (message == ButtonCommands.SaveThisImage)
                        {
                            saveThisImage = true;
                        }
                        else if (message == ButtonCommands.EndStreaming)
                        {
                            state = StreamLoopState.Waiting;
                        }
                        else if (message == ButtonCommands.Exit)
                        {
                            state = StreamLoopState.Exit;
                        }
                    }
                }
            }
        }

        public static void MergeStreamsThreadInit(MergeStreamsManager manager)
        {
            int imageCtr = 0;
            bool isMessageDequeueSuccess;
            int nQueues = manager.input.nInputQueues;
            bool[] isInputDequeueSuccess = new bool[nQueues];
            StreamLoopState state = StreamLoopState.Waiting;
            Tuple<Mat, FrameMetaData>[][] input;

            while (true)
            {
                UpdateLoopState();
                if (state == StreamLoopState.Waiting) { Thread.Sleep(100); continue; }
                else if (state == StreamLoopState.Exit) { return; }

                input = new Tuple<Mat, FrameMetaData>[nQueues][];
                for (int i = 0; i < nQueues; i++)
                {
                    while (true)
                    {
                        isInputDequeueSuccess[i] = manager.input.streamQueues[i].TryDequeue(out input[i]);
                        if (isInputDequeueSuccess[i])
                        {
                            isInputDequeueSuccess[i] = false;
                            break;
                        }
                        else
                        {
                            UpdateLoopState();
                            if (state == StreamLoopState.Streaming) { continue; }
                            else if (state == StreamLoopState.Waiting) { Thread.Sleep(100); }
                            else if (state == StreamLoopState.Exit) { return; }
                        }
                    }
                }

                ProcessInput();
                continue;

                void ProcessInput()
                {
                    imageCtr += 1;

                    if (imageCtr % manager.output.outputChannels[0].enqueueOrDequeueRate != 0)
                    {
                        input[0][0].Item1.Dispose();
                        input[1][0].Item1.Dispose();
                        return;
                    }

                    Mat outputMat = new Mat(size: manager.output.outputChannels[0].imageSize, type: Emgu.CV.CvEnum.DepthType.Cv8U, channels: 1);
                    CvInvoke.VConcat(src1: input[0][0].Item1, src2: input[1][0].Item1, dst: outputMat);
                    FrameMetaData outputMeta = input[0][0].Item2;
                    Tuple<Mat, FrameMetaData>[] output = Util.GetStreamOutput(mat: outputMat, metaData: outputMeta);
                    manager.output.displayQueue.Enqueue(item: output);
                }

                void UpdateLoopState()
                {
                    isMessageDequeueSuccess = manager.messageQueue.TryDequeue(out ButtonCommands message);
                    if (isMessageDequeueSuccess)
                    {
                        if (message == ButtonCommands.BeginStreaming)
                        {
                            Console.WriteLine("BeginStreaming command received in MergeStreamsThread");
                            state = StreamLoopState.Streaming;
                        }
                        else if (message == ButtonCommands.EndStreaming)
                        {
                            Console.WriteLine("EndStreaming command received in MergeStreamsThread");
                            state = StreamLoopState.Waiting;
                        }
                        else if (message == ButtonCommands.Exit)
                        {
                            state = StreamLoopState.Exit;
                        }
                    }
                }
            }
        }

        enum StreamLoopState
        {
            Waiting,
            Streaming,
            Exit
        }
    }

    public static class AdvancedStreamThreadSetups
    {
        public static void SingleStreamThreadInit(int idx, SingleStreamManager manager)
        {
            int imageCtr = 0;
            bool isResizeNeeded = false;
            Mat background = new Mat(size: manager.output.outputChannels[0].imageSize, type: Emgu.CV.CvEnum.DepthType.Cv8U, channels: 1);
            bool resetBackground = true;
            bool saveThisImage = false;
            bool isMessageDequeueSuccess;
            bool isInputDequeueSuccess;
            StreamLoopState state = StreamLoopState.Waiting;

            while (true)
            {
                isInputDequeueSuccess = manager.input.streamQueue.TryDequeue(out Tuple<Mat, FrameMetaData>[] input);
                if (isInputDequeueSuccess)
                {
                    ProcessInput();
                }

                UpdateLoopState();
                if (state == StreamLoopState.Streaming) { continue; }
                else if (state == StreamLoopState.Waiting) { Thread.Sleep(100); }
                else if (state == StreamLoopState.Exit) { return; }

                void ProcessInput()
                {
                    imageCtr += 1;
                    if (imageCtr % manager.input.inputChannels[0].enqueueOrDequeueRate != 0)
                    {
                        input[0].Item1.Dispose();
                        return;
                    }

                    if (isResizeNeeded)
                    {
                        // TODO: Implement resize operation
                    }

                    if (saveThisImage)
                    {
                        Console.WriteLine("SaveThisImage message received on SingleStreamThread");
                        saveThisImage = false;
                    }

                    if (resetBackground)
                    {
                        Console.WriteLine("ResetBackground message received on SingleStreamThread");
                        input[0].Item1.CopyTo(background);
                        resetBackground = false;
                        Console.WriteLine("resetBackground = false");
                    }

                    if (imageCtr % manager.output.outputChannels[0].enqueueOrDequeueRate != 0)
                    {
                        input[0].Item1.Dispose();
                        return;
                    }
                    else
                    {
                        manager.output.streamQueue.Enqueue(item: input);
                    }
                }

                void UpdateLoopState()
                {
                    isMessageDequeueSuccess = manager.messageQueue.TryDequeue(out ButtonCommands message);
                    if (isMessageDequeueSuccess)
                    {
                        if (message == ButtonCommands.BeginStreaming)
                        {
                            Console.WriteLine("BeginStreaming command received in SingleStreamThread");
                            state = StreamLoopState.Streaming;
                        }
                        else if (message == ButtonCommands.ResetBackgroundImage)
                        {
                            resetBackground = true;
                        }
                        else if (message == ButtonCommands.SaveThisImage)
                        {
                            saveThisImage = true;
                        }
                        else if (message == ButtonCommands.EndStreaming)
                        {
                            state = StreamLoopState.Waiting;
                        }
                        else if (message == ButtonCommands.Exit)
                        {
                            state = StreamLoopState.Exit;
                        }
                    }
                }
            }
        }

        public static void MergeStreamsThreadInit(MergeStreamsManager manager)
        {
            int imageCtr = 0;
            bool isMessageDequeueSuccess;
            int nQueues = manager.input.nInputQueues;
            bool[] isInputDequeueSuccess = new bool[nQueues];
            StreamLoopState state = StreamLoopState.Waiting;
            Tuple<Mat, FrameMetaData>[][] input;

            while (true)
            {
                UpdateLoopState();
                if (state == StreamLoopState.Waiting) { Thread.Sleep(100); continue; }
                else if (state == StreamLoopState.Exit) { return; }

                input = new Tuple<Mat, FrameMetaData>[nQueues][];
                for (int i = 0; i < nQueues; i++)
                {
                    while (true)
                    {
                        isInputDequeueSuccess[i] = manager.input.streamQueues[i].TryDequeue(out input[i]);
                        if (isInputDequeueSuccess[i])
                        {
                            isInputDequeueSuccess[i] = false;
                            break;
                        }
                        else
                        {
                            UpdateLoopState();
                            if (state == StreamLoopState.Streaming) { continue; }
                            else if (state == StreamLoopState.Waiting) { Thread.Sleep(100); }
                            else if (state == StreamLoopState.Exit) { return; }
                        }
                    }
                }

                ProcessInput();
                continue;

                void ProcessInput()
                {
                    imageCtr += 1;

                    if (imageCtr % manager.output.outputChannels[0].enqueueOrDequeueRate != 0)
                    {
                        input[0][0].Item1.Dispose();
                        input[1][0].Item1.Dispose();
                        return;
                    }

                    Mat outputMat = new Mat(size: manager.output.outputChannels[0].imageSize, type: Emgu.CV.CvEnum.DepthType.Cv8U, channels: 1);
                    CvInvoke.VConcat(src1: input[0][0].Item1, src2: input[1][0].Item1, dst: outputMat);
                    FrameMetaData outputMeta = input[0][0].Item2;
                    Tuple<Mat, FrameMetaData>[] output = Util.GetStreamOutput(mat: outputMat, metaData: outputMeta);
                    manager.output.displayQueue.Enqueue(item: output);
                }

                void UpdateLoopState()
                {
                    isMessageDequeueSuccess = manager.messageQueue.TryDequeue(out ButtonCommands message);
                    if (isMessageDequeueSuccess)
                    {
                        if (message == ButtonCommands.BeginStreaming)
                        {
                            Console.WriteLine("BeginStreaming command received in MergeStreamsThread");
                            state = StreamLoopState.Streaming;
                        }
                        else if (message == ButtonCommands.EndStreaming)
                        {
                            Console.WriteLine("EndStreaming command received in MergeStreamsThread");
                            state = StreamLoopState.Waiting;
                        }
                        else if (message == ButtonCommands.Exit)
                        {
                            state = StreamLoopState.Exit;
                        }
                    }
                }
            }
        }

        enum StreamLoopState
        {
            Waiting,
            Streaming,
            Exit
        }
    }

    public class ThreadManager
    {
        private Thread[][] threads;
        public ManagerBundle managerBundle { get; set; }
        private StreamArchitecture architecture { get; }
        private string[] sessionPaths { get; }
        internal StreamGraph streamGraph { get; set; }
        private IList<IManagedCamera> managedCameras { get; }
        private Util.OryxSetupInfo[] oryxSetups { get; }

        public ThreadManager(StreamArchitecture architecture, string[] sessionPaths, IList<IManagedCamera> managedCameras, Util.OryxSetupInfo[] oryxSetups)
        {
            this.architecture = architecture;
            this.sessionPaths = sessionPaths;
            this.managedCameras = managedCameras;
            this.oryxSetups = oryxSetups;
            streamGraph = new StreamGraph(this.architecture);
            managerBundle = new ManagerBundle(graph: streamGraph, sessionPaths: this.sessionPaths, camFrameSize: oryxSetups[0].frameSize);
            SetupThreads();
        }

        void SetupThreads()
        {
            threads = new Thread[streamGraph.nThreadLayers][];
            for (int i = 0; i < streamGraph.nThreadLayers; i++)
            {
                threads[i] = new Thread[streamGraph.nThreadsPerLayer[i]];
                for (int j = 0; j < streamGraph.nThreadsPerLayer[i]; j++)
                {
                    int _i = i;
                    int _j = j;

                    if (streamGraph.graph[i][j] == ThreadType.Camera)
                    {
                        CamStreamManager _manager = managerBundle.camStreamManagers[_j];
                        threads[i][j] = new Thread(() => new OryxCamera(camNumber: _j, managedCamera: managedCameras[_j],
                            manager: _manager, setupInfo: oryxSetups[_j]));
                    }
                    else if (streamGraph.graph[i][j] == ThreadType.SingleCameraStream)
                    {
                        SingleStreamManager _manager = managerBundle.singleStreamManagers[_j];
                        threads[i][j] = new Thread(() => BasicStreamThreadSetups.SingleStreamThreadInit(idx: _j, manager: _manager));
                    }
                    else if (streamGraph.graph[i][j] == ThreadType.MergeStreams)
                    {
                        MergeStreamsManager _manager = managerBundle.mergeStreamsManager;
                        threads[i][j] = new Thread(() => BasicStreamThreadSetups.MergeStreamsThreadInit(manager: _manager));
                    }
                }
            }
        }

        public void StartThreads()
        {
            for (int i = 0; i < streamGraph.nThreadLayers; i++)
            {
                for (int j = 0; j < streamGraph.nThreadsPerLayer[i]; j++)
                {
                    threads[i][j].Start();
                }
            }
        }

        public class StreamGraph
        {
            public int nThreadLayers { get; }
            public int[] nThreadsPerLayer { get; }
            public ThreadType[][] graph { get; }
            public StreamArchitecture architecture { get; private set; }

            public StreamGraph(StreamArchitecture architecture)
            {
                this.architecture = architecture;
                if (this.architecture == StreamArchitecture.ThreeLevelBasic)
                {
                    nThreadLayers = 3;
                    nThreadsPerLayer = new int[] { 2, 2, 1 };
                    graph = new ThreadType[nThreadLayers][];
                    graph[0] = new ThreadType[2] { ThreadType.Camera, ThreadType.Camera };
                    graph[1] = new ThreadType[2] { ThreadType.SingleCameraStream, ThreadType.SingleCameraStream };
                    graph[2] = new ThreadType[1] { ThreadType.MergeStreams };
                }
                else
                {
                    throw new ArgumentException(message: "Only ThreeLevelBasic StreamArchitecture is currently supported.");
                }
            }
        }

        public class ManagerBundle
        {
            private StreamGraph graph;
            private string[] sessionPaths { get; }
            public CamStreamManager[] camStreamManagers { get; set; }
            public SingleStreamManager[] singleStreamManagers { get; set; }
            public MergeStreamsManager mergeStreamsManager { get; set; }
            public ConcurrentQueue<ButtonCommands>[][] messageQueues { get; private set; }
            private Size camFrameSize { get; }
            public ManagerBundle(StreamGraph graph, string[] sessionPaths, Size camFrameSize)
            {
                this.graph = graph;
                this.sessionPaths = sessionPaths;
                this.camFrameSize = camFrameSize;
                MessageQueueSetup();
                Setup();
            }

            void MessageQueueSetup()
            {
                messageQueues = new ConcurrentQueue<ButtonCommands>[graph.nThreadLayers][];
                for (int i = 0; i < graph.nThreadLayers; i++)
                {
                    messageQueues[i] = new ConcurrentQueue<ButtonCommands>[graph.nThreadsPerLayer[i]];
                    for (int j = 0; j < graph.nThreadsPerLayer[i]; j++)
                    {
                        messageQueues[i][j] = new ConcurrentQueue<ButtonCommands>();
                    }
                }
            }

            void Setup()
            {
                if (graph.architecture != StreamArchitecture.ThreeLevelBasic)
                {
                    throw new ArgumentException(message: "Only StreamArchitecture.ThreeLevelBasic is currently supported.");
                }

                camStreamManagers = new CamStreamManager[graph.nThreadsPerLayer[0]];
                for (int j = 0; j < graph.nThreadsPerLayer[0]; j++)
                {
                    camStreamManagers[j] = new CamStreamManager(messageQueue: messageQueues[0][j], sessionPath: sessionPaths[j], setupStyle: CamStreamManager.CamStreamManagerSetup.BasicStream);
                }

                SingleStreamManager.Output[] _singleStreamOutputs = new SingleStreamManager.Output[graph.nThreadsPerLayer[1]];
                singleStreamManagers = new SingleStreamManager[graph.nThreadsPerLayer[1]];
                for (int j = 0; j < graph.nThreadsPerLayer[1]; j++)
                {
                    CamStreamManager.Output _camOutput = camStreamManagers[j].output;
                    singleStreamManagers[j] = new SingleStreamManager(camOutputManager: _camOutput,
                        messageQueue: messageQueues[1][j], sessionPath: sessionPaths[j], setupStyle: SingleStreamManager.SingleStreamManangerSetup.BasicStream);

                    _singleStreamOutputs[j] = singleStreamManagers[j].output;
                }

                mergeStreamsManager = new MergeStreamsManager(singleStreamOutputManagers: _singleStreamOutputs,
                    messageQueue: messageQueues[2][0], sessionPath: sessionPaths[0], setupStyle: MergeStreamsManager.MergeStreamManangerSetup.BasicStream);
            }
        }
    }
}
