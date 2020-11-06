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

namespace FetchRig6
{

    public enum StreamArchitecture
    {
        TwoLevel,
        ThreeLevel
    }

    public enum ThreadTypes
    {
        Camera,
        SingleCameraStream,
        MergeStreams
    }


    public class StreamChannelInfo
    {
        public Size imageSize { get; set; }

    }

    public class ChannelInputInfo : StreamChannelInfo
    {
        public bool encodeInput { get; set; }
        public int encodeRate { get; set; }

        public ChannelInputInfo(Size imageSize, int encodeRate=0)
        {
            this.imageSize = imageSize;
            this.encodeRate = encodeRate;
            encodeInput = (encodeRate > 0) ? true : false;
        }
    }

    public class ChannelInputOutputMapping
    {
        public ChannelInputOutputMapping(ChannelInputInfo[] input, ChannelOutputInfo[] output)
        {

        }

        public enum ChannelMapping
        {
            OneToOne,
            OneToTwo
        }
    }

    public class ChannelOutputInfo : StreamChannelInfo
    {
        public int enqueueRate { get; set; }

        public ChannelOutputInfo(Size imageSize, int enqueueRate=1)
        {
            this.imageSize = imageSize;
            this.enqueueRate = enqueueRate;
        }
    }


    public class StreamQueueInfo
    {
        public int channels { get; }

        public StreamQueueInfo(int channels)
        {
            this.channels = channels;
        }

    }

    public class StreamThreadInfo
    {
        public InOut inOut { get; }
        public int nQueues { get; }
        public int[] nChannels { get; }
        public bool[] isEncodeChannel { get; }

        public StreamThreadInfo(InOut inOrOut)
        {

        }
    }



    public class StreamBlueprint
    {
        ThreadTypes[][] streamThreadTypes;

        public StreamBlueprint(StreamArchitecture architecture)
        {
            if (architecture == StreamArchitecture.TwoLevel)
            {
                throw new ArgumentException("Two-Level Thread Architecture not yet supported", "StreamArchitecture");
            }
            else if (architecture == StreamArchitecture.ThreeLevel)
            {
                // initialize ragged array to define stream flow graph
                streamThreadTypes = new ThreadTypes[3][];

                // camera threads:
                streamThreadTypes[0] = new ThreadTypes[2] { ThreadTypes.Camera, ThreadTypes.Camera };

                // single camera processing threads:
                streamThreadTypes[1] = new ThreadTypes[2] { ThreadTypes.SingleCameraStream, ThreadTypes.SingleCameraStream };

                // merge streams processing thread:
                streamThreadTypes[2] = new ThreadTypes[1] { ThreadTypes.MergeStreams };
            }
        }
    }




    public class ThreadComs
    {
        public ThreadComs()
        {

        }
    }


    class ThreadManager
    {





    }
}
