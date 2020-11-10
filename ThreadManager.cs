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
        CameraStreamInputManager inputManager;
        CameraStreamOutputManager outputManager;
        ConcurrentQueue<ButtonCommands> commandQueue;

        public CameraStreamManager(CameraStreamInputManager inputManager, CameraStreamOutputManager outputManager, ConcurrentQueue<ButtonCommands> commandQueue)
        {
            this.inputManager = inputManager;
            this.outputManager = outputManager;
            this.commandQueue = commandQueue;
        }

        public class CameraStreamInputManager : StreamInputManager
        {
            public Size fullCameraFrameSize { get; }
            CameraInputChannelInfo inputChannelInfo;

            public CameraStreamInputManager(Size fullCameraFrameSize)
            {
                this.fullCameraFrameSize = fullCameraFrameSize;
                threadType = ThreadType.Camera;
                inputChannelInfo = new CameraInputChannelInfo(imageSize: fullCameraFrameSize, encodeRate: 0);
            }

            public class CameraInputChannelInfo : StreamChannelInfo
            {
                public int encodeRate { get; set; }
                public CameraInputChannelInfo(Size imageSize, int encodeRate=0)
                {
                    this.imageSize = imageSize;
                    this.encodeRate = encodeRate;
                    isEncodable = (encodeRate > 0) ? true : false;
                }
            }
        }

        public class CameraStreamOutputManager : StreamOutputManager
        {
            CameraOutputChannelInfo[] outputChannelInfos;
            ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamOutputQueue;

            public CameraStreamOutputManager(CameraOutputChannelInfo[] outputChannelInfos)
            {
                this.outputChannelInfos = outputChannelInfos;
                streamOutputQueue = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>();
            }

            public class CameraOutputChannelInfo : StreamChannelInfo
            {
                public int encodeRate { get; set; }
                public CameraOutputChannelInfo(Size imageSize, int encodeRate = 0)
                {
                    this.imageSize = imageSize;
                    this.encodeRate = encodeRate;
                    isEncodable = (encodeRate > 0) ? true : false;
                }
            }
        }
    }


    


    public class ThreadManager
    {
        Thread[][] threads;
        ConcurrentQueue<ButtonCommands>[][] commandQueues;
        StreamArchitecture architecture { get; }
        StreamGraph streamGraph { get; }


        public ThreadManager(StreamArchitecture architecture)
        {
            this.architecture = architecture;
            streamGraph = new StreamGraph(architecture);
            CommandQueueAndThreadSetup();
            ThreadArgumentSetup();
        }

        void CommandQueueAndThreadSetup()
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

        }

        void SingleCameraStreamThreadSetup(int camNumber, int layer=1)
        {

        }

        void MergeCameraStreamsThreadSetup(int layer=2)
        {

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
