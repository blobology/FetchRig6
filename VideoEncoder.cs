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

namespace FetchRig6
{
    public class VideoEncoder
    {
        private readonly string sessionPath;
        private string videoFileName;

        public VideoEncoder(string sessionPath)
        {
            this.sessionPath = sessionPath;
        }

        public class FFProcess
        {
            private string _pipeName;
            private string _videoFileName;
            private string inputArgs;
            private string outputArgs;
            private string fullArgs;
            public Process process;

            public FFProcess(string pipeName, string videoFileName)
            {
                _pipeName = @"\\.\pipe\" + pipeName;
                _videoFileName = videoFileName;
                inputArgs = "-nostats -y -vsync 0 -f rawvideo -s 3208x2200 -pix_fmt gray -framerate 100 -i " + _pipeName + " -an -sn";
                outputArgs = "-gpu 0 -vcodec h264_nvenc -r 100 -preset fast -qp 20 " + _videoFileName;
                fullArgs = inputArgs + " " + outputArgs;
            }

            public void OpenWithStartInfo()
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "ffmpeg.exe";
                startInfo.Arguments = fullArgs;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = true;
                process = Process.Start(startInfo);
            }
        }
    }
}
