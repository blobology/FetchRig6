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

        public class InputOutputManager
        {
            public bool isEncodable { get; set; }
            public string[] encodeFileName { get; set; }
        }

        [Serializable]
        public class StreamChannel
        {
            public Size imageSize { get; set; }
            public bool isEncodeable { get; }
            public int encodeRate { get; }
            public StreamChannel(Size imageSize, int encodeRate=0)
            {
                this.imageSize = imageSize;
                this.encodeRate = encodeRate;
                isEncodeable = (encodeRate > 0) ? true : false;
            }
        }

        public class StreamQueue
        {
            public int nChannels { get; }
            public StreamChannel[] channels { get; }
        }
    }

    public class CamStreamManager : StreamManager
    {
        public ConcurrentQueue<ButtonCommands> messageQueue;
        public Input input { get; set; }
        public Output output { get; set; }
        
        public CamStreamManager(Size camFrameSize, ConcurrentQueue<ButtonCommands> messageQueue)
        {
            this.messageQueue = messageQueue;
            threadType = ThreadType.Camera;

            // Input Manager Setup:
            StreamChannel inputChannel = new StreamChannel(imageSize: camFrameSize);
            input = new Input(inputChannel: inputChannel);

            // Output Manager Setup:
            Size _outputChannelSize = new Size(width: 802, height: 550);
            StreamChannel[] _outputChannels = new StreamChannel[] { new StreamChannel(imageSize: _outputChannelSize) };
            output = new Output(outputChannels: _outputChannels);
        }

        public class Input : InputOutputManager
        {
            public StreamChannel inputChannel { get; set; }

            public Input(StreamChannel inputChannel)
            {
                this.inputChannel = inputChannel;
                if (inputChannel.isEncodeable)
                {
                    isEncodable = true;
                    encodeFileName = new string[1] { "test" };
                }
            }
        }

        [Serializable]
        public class Output : InputOutputManager
        {
            public StreamChannel[] outputChannels { get; set; }
            public ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamQueue;
            public int nChannels { get; }

            public Output(StreamChannel[] outputChannels)
            {
                nChannels = outputChannels.Length;
                this.outputChannels = outputChannels;
                encodeFileName = new string[nChannels];
                for (int i = 0; i < outputChannels.Length; i++)
                {
                    if (outputChannels[i].isEncodeable)
                    {
                        isEncodable = true;
                        encodeFileName[i] = "test_" + i.ToString();
                    }
                    else { encodeFileName[i] = null; }
                }
                streamQueue = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>();
            }
        }
    }

    public class SingleCamStreamManager : StreamManager
    {
        public ConcurrentQueue<ButtonCommands> messageQueue;
        public Input input { get; set; }
        public Output output { get; set; }

        public SingleCamStreamManager(CamStreamManager.Output camOutputManager, ConcurrentQueue<ButtonCommands> messageQueue)
        {
            this.messageQueue = messageQueue;

            // Inherit input specs from CamStreamManager
            input = new Input(inputChannels: camOutputManager.outputChannels, streamQueue: camOutputManager.streamQueue);

            // Specify output specs:
            Size _outputChannelSize = new Size(width: 802, height: 550);
            StreamChannel[] _outputChannels = new StreamChannel[] { new StreamChannel(imageSize: _outputChannelSize) };
            output = new Output(outputChannels: _outputChannels);
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
                encodeFileName = new string[nChannels];
                for (int i = 0; i < inputChannels.Length; i++)
                {
                    if (inputChannels[i].isEncodeable)
                    {
                        isEncodable = true;
                        encodeFileName[i] = "inputFileTest_" + i.ToString();
                    }
                    else { encodeFileName[i] = null; }
                }
                this.streamQueue = streamQueue;
            }
        }

        [Serializable]
        public class Output : InputOutputManager
        {
            public StreamChannel[] outputChannels { get; set; }
            public ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamQueue { get; }
            public int nChannels { get; }

            public Output(StreamChannel[] outputChannels)
            {
                nChannels = outputChannels.Length;
                this.outputChannels = outputChannels;
                encodeFileName = new string[nChannels];
                for (int i = 0; i < outputChannels.Length; i++)
                {
                    if (outputChannels[i].isEncodeable)
                    {
                        isEncodable = true;
                        encodeFileName[i] = "outputFileTest_" + i.ToString();
                    }
                    else { encodeFileName[i] = null; }
                }
                streamQueue = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>();
            }
        }
    }

    public class MergeStreamsManager : StreamManager
    {
        public ConcurrentQueue<ButtonCommands> messageQueue;
        public Input input { get; set; }
        public Output output { get; set; }

        public MergeStreamsManager(SingleCamStreamManager.Output[] singleStreamOutputManagers, ConcurrentQueue<ButtonCommands> messageQueue)
        {
            this.messageQueue = messageQueue;

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

                // Disallow encoding of input streams on this thread:
                isEncodable = false;
                encodeFileName = null;
            }
        }

        [Serializable]
        public class Output : InputOutputManager
        {
            StreamChannel[] outputChannels { get; set; }
            ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> displayQueue { get; }
            public int nChannels { get; }

            public Output(StreamChannel[] outputChannels)
            {
                nChannels = outputChannels.Length;
                this.outputChannels = outputChannels;
                encodeFileName = new string[nChannels];
                for (int i = 0; i < outputChannels.Length; i++)
                {
                    if (outputChannels[i].isEncodeable)
                    {
                        isEncodable = true;
                        encodeFileName[i] = "outputFileTest_" + i.ToString();
                    }
                    else { encodeFileName[i] = null; }
                }
                displayQueue = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>();
            }
        }
    }


    public class ThreadManager
    {
        Thread[][] threads;
        StreamArchitecture architecture { get; }
        string[] sessionPaths { get; }
        StreamGraph streamGraph { get; set; }
        ManagerBundle managerBundle { get; set; }
        ManagedCamera[] managedCameras;
        Util.OryxSetupInfo[] oryxSetups;

        public ThreadManager(StreamArchitecture architecture, string[] sessionPaths, ManagedCamera[] managedCameras, Util.OryxSetupInfo[] oryxSetups)
        {
            this.architecture = architecture;
            this.sessionPaths = sessionPaths;
            this.managedCameras = managedCameras;
            this.oryxSetups = oryxSetups;
            streamGraph = new StreamGraph(architecture);
            managerBundle = new ManagerBundle(graph: streamGraph);
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

        public class ManagerBundle
        {
            private StreamGraph graph;
            public CamStreamManager[] camStreamManagers { get; set; }
            public SingleCamStreamManager[] singleStreamManagers { get; set; }
            public MergeStreamsManager mergeStreamsManager { get; set; }
            
            public ConcurrentQueue<ButtonCommands>[][] messageQueues { get; private set; }
            public ManagerBundle(StreamGraph graph)
            {
                this.graph = graph;
                MessageQueueSetup();
                Setup();
            }

            void MessageQueueSetup()
            {
                for (int i = 0; i < graph.nThreadLayers; i++)
                {
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

                for (int j = 0; j < graph.nThreadsPerLayer[0]; j++)
                {
                    Size _camFrameSize = new Size(width: 3208, height: 2200);
                    camStreamManagers[j] = new CamStreamManager(camFrameSize: _camFrameSize, messageQueue: messageQueues[0][j]);
                }

                SingleCamStreamManager.Output[] _singleStreamOutputs = new SingleCamStreamManager.Output[graph.nThreadsPerLayer[1]];
                for (int j = 0; j < graph.nThreadsPerLayer[1]; j++)
                {
                    CamStreamManager.Output _camOutput = camStreamManagers[j].output;
                    singleStreamManagers[j] = new SingleCamStreamManager(camOutputManager: _camOutput, messageQueue: messageQueues[1][j]);

                    _singleStreamOutputs[j] = singleStreamManagers[j].output;
                }

                mergeStreamsManager = new MergeStreamsManager(singleStreamOutputManagers: _singleStreamOutputs, messageQueue: messageQueues[2][0]);
            }
        }
    }
}
