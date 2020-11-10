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

    public enum SetupType
    {
        Default,
        Custom
    }

    public class StreamChannelInfo
    {
        public Size imageSize { get; set; }
        public bool isEncodable { get; set; }

    }

    public class StreamQueueInfo
    {
        public int nChannels { get; set; }
    }


    public class StreamInputManager
    {
        public ThreadType threadType { get; set; }
    }

    public class StreamOutputManager
    {
        public ThreadType threadType { get; set; }
    }

    public class CameraStreamManager
    {
        public InputManager inputManager { get; }
        public OutputManager outputManager { get; }
        public ConcurrentQueue<ButtonCommands> commandQueue { get; }

        public CameraStreamManager(ConcurrentQueue<ButtonCommands> commandQueue)
        {
            this.commandQueue = commandQueue;

            InputManager.InputChannel _inputChannel = new InputManager.InputChannel();
            inputManager = new InputManager(inputChannelInfo: _inputChannel);

            OutputManager.OutputChannel[] _outputChannels = new OutputManager.OutputChannel[1] { new OutputManager.OutputChannel() };
            outputManager = new OutputManager(outputChannelInfos: _outputChannels);
        }

        public CameraStreamManager(InputManager inputManager, OutputManager outputManager,
            ConcurrentQueue<ButtonCommands> commandQueue)
        {
            this.inputManager = inputManager;
            this.outputManager = outputManager;
            this.commandQueue = commandQueue;
        }

        public class InputManager : StreamInputManager
        {
            InputChannel inputChannelInfo;

            public InputManager(InputChannel inputChannelInfo)
            {
                threadType = ThreadType.Camera;
                this.inputChannelInfo = inputChannelInfo;
            }

            public class InputChannel : StreamChannelInfo
            {
                public int encodeRate { get; set; }

                public InputChannel()
                {
                    Size _camFrameSize = new Size(width: 3208, height: 2200);
                    imageSize = _camFrameSize;
                    encodeRate = 0;
                    isEncodable = (encodeRate > 0) ? true : false;
                }
                public InputChannel(Size imageSize, int encodeRate=0)
                {
                    this.imageSize = imageSize;
                    this.encodeRate = encodeRate;
                    isEncodable = (encodeRate > 0) ? true : false;
                }
            }
        }

        public class OutputManager : StreamOutputManager
        {
            OutputChannel[] outputChannelInfos;
            ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamOutputQueue;

            public OutputManager(OutputChannel[] outputChannelInfos)
            {
                this.outputChannelInfos = outputChannelInfos;
                streamOutputQueue = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>();
            }

            public class OutputChannel : StreamChannelInfo
            {
                public int encodeRate { get; set; }
                public int enqueueRate { get; set; }

                public OutputChannel()
                {
                    imageSize = new Size(width: 3208, height: 2200);
                    encodeRate = 0;
                    isEncodable = (encodeRate > 0) ? true : false;
                    enqueueRate = 1;
                }
                public OutputChannel(Size imageSize, int encodeRate=0, int enqueueRate=1)
                {
                    this.imageSize = imageSize;
                    this.encodeRate = encodeRate;
                    isEncodable = (encodeRate > 0) ? true : false;
                    this.enqueueRate = enqueueRate;
                }
            }
        }
    }


    


    public class ThreadManager
    {
        Thread[][] threads;
        ConcurrentQueue<ButtonCommands>[][] commandQueues;
        StreamArchitecture architecture { get; }
        string[] sessionPaths { get; }
        StreamGraph streamGraph { get; }
        ManagedCamera[] managedCameras;
        Util.OryxSetupInfo[] oryxSetups;



        public ThreadManager(StreamArchitecture architecture, string[] sessionPaths, ManagedCamera[] managedCameras, Util.OryxSetupInfo[] oryxSetups)
        {
            this.architecture = architecture;
            this.sessionPaths = sessionPaths;
            this.managedCameras = managedCameras;
            this.oryxSetups = oryxSetups;
            streamGraph = new StreamGraph(architecture);
            CommandQueueAndThreadDeclaration();
            ThreadArgumentSetup();
        }

        void CommandQueueAndThreadDeclaration()
        {
            commandQueues = new ConcurrentQueue<ButtonCommands>[streamGraph.nThreadLayers][];
            threads = new Thread[streamGraph.nThreadLayers][];
            for (int i = 0; i < streamGraph.nThreadLayers; i++)
            {
                commandQueues[i] = new ConcurrentQueue<ButtonCommands>[streamGraph.nThreadsPerLayer[i]];
                threads[i] = new Thread[streamGraph.nThreadsPerLayer[i]];
                for (int j = 0; j < streamGraph.nThreadsPerLayer[i]; j++)
                {
                    commandQueues[i][j] = new ConcurrentQueue<ButtonCommands>();
                }
            }
        }

        void ThreadArgumentSetup()
        {
            
            for (int i = 0; i < streamGraph.nThreadLayers; i++)
            {
                
                for (int j = 0; j < streamGraph.nThreadsPerLayer[i]; j++)
                {
                    if (streamGraph.graph[i][j] == ThreadType.Camera)
                    {
                        CameraThreadSetup(camNumber: j);
                    }
                    else if (streamGraph.graph[i][j] == ThreadType.SingleCameraStream)
                    {
                        SingleCameraStreamThreadSetup(camNumber: j);
                    }
                    else if (streamGraph.graph[i][j] == ThreadType.MergeStreams)
                    {
                        MergeCameraStreamsThreadSetup();
                    }
                }
            }
        }

        void CameraThreadSetup(int camNumber, int layer=0)
        {
            int _camNumber = camNumber;
            int _layer = layer;
            string _sessionPath = string.Copy(sessionPaths[_camNumber]);

            // setup input channel specs
            CameraStreamManager _camManager = new CameraStreamManager(commandQueue: commandQueues[_layer][_camNumber]);

            threads[_layer][_camNumber] = new Thread(() => new OryxCamera(camNumber: _camNumber, managedCamera: managedCameras[_camNumber],
                sessionPath: _sessionPath, manager: _camManager, setupInfo: oryxSetups[_camNumber]));

            threads[_layer][_camNumber].IsBackground = false;
            threads[_layer][_camNumber].Priority = ThreadPriority.Highest;
        }

        void SingleCameraStreamThreadSetup(int camNumber, int layer=1)
        {

        }

        void MergeCameraStreamsThreadSetup(int layer=2)
        {

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

            public StreamGraph(StreamArchitecture architecture)
            {
                if (architecture == StreamArchitecture.ThreeLevelBasic)
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
    }
}
