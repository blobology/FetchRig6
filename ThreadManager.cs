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
        ThreeLevel
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
        Size fullCameraFrameSize;
        CameraStreamInputManager inputManager;

        public CameraStreamManager(Util.OryxSetupInfo oryxSetupInfo)
        {
            fullCameraFrameSize = oryxSetupInfo.frameSize.DeepClone();
        }



        public class CameraStreamInputManager : StreamInputManager
        {
            StreamChannelInfo streamChannelInfo;

            public CameraStreamInputManager()
            {
                threadType = ThreadType.Camera;

            }
        }


    }


    //public class InputChannelInfo : StreamChannelInfo
    //{
    //    public bool encodeInput { get; set; }
    //    public int encodeRate { get; set; }

    //    public InputChannelInfo(Size imageSize, int encodeRate=0)
    //    {
    //        this.imageSize = imageSize;
    //        this.encodeRate = encodeRate;
    //        encodeInput = (encodeRate > 0) ? true : false;
    //    }
    //}

    //public class OutputChannelInfo : StreamChannelInfo
    //{
    //    public bool encodeOutput { get; set; }
    //    public int encodeRate { get; set; }
    //    public bool enqueueOutput { get; set; }
    //    public int enqueueRate { get; set; }

    //    public OutputChannelInfo(Size imageSize, int encodeRate=0, int enqueueRate=1)
    //    {
    //        this.imageSize = imageSize;

    //        this.encodeRate = encodeRate;
    //        encodeOutput = (encodeRate > 0) ? true : false;

    //        this.enqueueRate = enqueueRate;
    //        enqueueOutput = (enqueueRate > 0) ? true : false;
    //    }
    //}

    //public class StreamQueueInfo
    //{
    //    public int nChannels { get; set; }
    //    public ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamQueue;
    //}

    //public class InputQueueInfo : StreamQueueInfo
    //{
    //    public InputChannelInfo[] inputChannelInfos;
    //    public InputQueueInfo(InputChannelInfo[] inputChannelInfos, ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamQueue)
    //    {
    //        this.inputChannelInfos = inputChannelInfos;
    //        this.streamQueue = streamQueue;
    //        nChannels = inputChannelInfos.Length;
    //    }
    //}

    //public class OutputQueueInfo : StreamQueueInfo
    //{
    //    public OutputChannelInfo[] outputChannelInfos;
    //    public OutputQueueInfo(OutputChannelInfo[] outputChannelInfos, ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamQueue)
    //    {
    //        this.outputChannelInfos = outputChannelInfos;
    //        this.streamQueue = streamQueue;
    //        nChannels = outputChannelInfos.Length;
    //    }
    //}





    //public class StreamBlueprint
    //{
    //    public StreamArchitecture arch;
    //    public ThreadTypes[][] streamThreadTypes;
    //    public int nThreadLayers { get; private set; }
    //    public int[] threadsPerLayer { get; private set; }

    //    public StreamBlueprint(StreamArchitecture architecture)
    //    {
    //        arch = architecture;
    //        if (arch == StreamArchitecture.TwoLevel)
    //        {
    //            throw new ArgumentException("Two-Level Thread Architecture not yet supported", "StreamArchitecture");
    //        }
    //        else if (arch == StreamArchitecture.ThreeLevel)
    //        {
    //            // initialize ragged array to define stream flow graph
    //            streamThreadTypes = new ThreadTypes[3][];

    //            // camera threads:
    //            streamThreadTypes[0] = new ThreadTypes[2] { ThreadTypes.Camera, ThreadTypes.Camera };

    //            // single camera processing threads:
    //            streamThreadTypes[1] = new ThreadTypes[2] { ThreadTypes.SingleCameraStream, ThreadTypes.SingleCameraStream };

    //            // merge streams processing thread:
    //            streamThreadTypes[2] = new ThreadTypes[1] { ThreadTypes.MergeStreams };
    //        }

    //        Setup();
    //    }

    //    void Setup()
    //    {
    //        nThreadLayers = streamThreadTypes.Length;
    //        threadsPerLayer = new int[nThreadLayers];

    //        for (int i = 0; i < nThreadLayers; i++)
    //        {
    //            threadsPerLayer[i] = streamThreadTypes[i].Length;
    //        }
    //    }
    //}




    //public class ThreadSetup
    //{
    //    private StreamBlueprint blueprint;
    //    private Util.OryxSetupInfo[] oryxSetupInfos;
    //    public ConcurrentQueue<ButtonCommands>[][] commsQueues;
    //    public Thread[][] threads;

    //    public ThreadSetup(StreamBlueprint blueprint, Util.OryxSetupInfo[] oryxSetupInfos)
    //    {
    //        this.blueprint = blueprint;
    //        this.oryxSetupInfos = oryxSetupInfos;
    //        DeclareThreadsAndComms();
    //        SetupStreamQueues();
            
    //    }

    //    void DeclareThreadsAndComms()
    //    {
    //        commsQueues = new ConcurrentQueue<ButtonCommands>[blueprint.nThreadLayers][];
    //        threads = new Thread[blueprint.nThreadLayers][];
    //        for (int i = 0; i < blueprint.nThreadLayers; i++)
    //        {
    //            commsQueues[i] = new ConcurrentQueue<ButtonCommands>[blueprint.threadsPerLayer[i]];
    //            threads[i] = new Thread[blueprint.threadsPerLayer[i]];
    //            for (int j = 0; j < blueprint.threadsPerLayer[j]; j++)
    //            {
    //                commsQueues[i][j] = new ConcurrentQueue<ButtonCommands>();
    //            }
    //        }
    //    }

    //    void SetupStreamQueues()
    //    {
    //        if (blueprint.arch == StreamArchitecture.ThreeLevel)
    //        {
    //            // define channels and queues for camera threads:
    //            for (int i = 0; i < Form1.nCameras; i++)
    //            {
    //                CameraThreadSetup(camNumber: i, layer: 0);
    //            }
    //        }
    //    }

    //    void CameraThreadSetup(int camNumber, int layer=0)
    //    {
    //        int _camNumber = camNumber;   // all thread init arguments should be deep copies
    //        int _layer = layer;
    //        ConcurrentQueue<ButtonCommands> commsQueue = commsQueues[_camNumber][_layer];

    //        // define camera input channel info:
    //        InputChannelInfo _inputChannelInfo = GetCameraInputChannelInfo();

    //        // define camera output channel and queue info:

    //        // define output channel

            



    //        InputChannelInfo GetCameraInputChannelInfo()
    //        {
    //            Size _inputChannelSize = oryxSetupInfos[_camNumber].frameSize.DeepClone();
    //            int _inputEncodeRate = 0;
    //            return new InputChannelInfo(imageSize: _inputChannelSize, encodeRate: _inputEncodeRate);
    //        }

    //        OutputQueueInfo[] GetCameraOutputQueueInfo()
    //        {
    //            Size _outputChannelSize = new Size(width: 802, height: 550);
    //            int _outputEncodeRate = 0;
    //            int _enqueueRate = 2;
    //            OutputChannelInfo[] _outputChannelInfo = new OutputChannelInfo[1] { new OutputChannelInfo(imageSize: _outputChannelSize, encodeRate: _outputEncodeRate, enqueueRate: _enqueueRate) };

    //            // set up ConcurrentQueue to connect this camera to the downstream single camera processing thread:
    //            ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> _outputStreamQueue = new ConcurrentQueue<Tuple<Mat, FrameMetaData>[]>();
    //            OutputQueueInfo[] _outputQueueInfo = new OutputQueueInfo[1] { new OutputQueueInfo(outputChannelInfos: _outputChannelInfo ) }
    //        }
    //    }
    //}


    public class ThreadManager
    {




    }
}
