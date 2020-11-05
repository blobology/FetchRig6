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
    public class OryxCamera
    {
        readonly int camNumber;
        private string sessionPath;
        private ConcurrentQueue<ButtonCommands> camControlMessageQueue;
        private ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamOutputQueue;
        private readonly Size fullFrameSize;

        // These fields will be accessed by an OryxCameraSettings object to set and save camera settings.
        public IManagedCamera managedCamera;
        public Util.OryxSetupInfo setupInfo;
        public INodeMap nodeMapTLDevice;
        public INodeMap nodeMapTLStream;
        public INodeMap nodeMap;
        public string settingsFileName;

        public OryxCamera(int camNumber, IManagedCamera managedCamera, ConcurrentQueue<ButtonCommands> camControlMessageQueue,
            ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> streamOutputQueue, Util.OryxSetupInfo setupInfo, string sessionPath)
        {
            this.camNumber = camNumber;
            this.managedCamera = managedCamera;
            this.camControlMessageQueue = camControlMessageQueue;
            this.streamOutputQueue = streamOutputQueue;
            this.setupInfo = setupInfo;
            this.sessionPath = sessionPath;

            fullFrameSize = this.setupInfo.frameSize;
            settingsFileName = this.sessionPath + @"\" + "cam" + this.camNumber.ToString() + @"_cameraSettings.txt";


            GetNodeMapsAndInitialize();
            LoadCameraSettings();
            CameraStreamingLoop();
            Console.WriteLine("CameraStreamingLoop has exited on camera {0}. Will now return.", this.camNumber.ToString());
            return;
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

        private void CameraStreamingLoop()
        {

        }


            



        private void CloseOryxCamera(Util.CloseCameraMethod closeMethod)
        {
            if (!managedCamera.IsInitialized())
            {
                Console.WriteLine("Camera number {0} not initialized. Cannot execute DeviceReset or FactoryReset command", camNumber.ToString());
                return;
            }

            if (managedCamera.IsStreaming())
            {
                managedCamera.EndAcquisition();
                Console.WriteLine("EndAcquisition executed from CloseOryxCamera block on camera {0}", camNumber.ToString());
            }

            if (closeMethod == Util.CloseCameraMethod.DeInit)
            {
                managedCamera.DeInit();
                Console.WriteLine("Camera number {0} deinitialized.", camNumber.ToString());
            }
            else if (closeMethod == Util.CloseCameraMethod.DeInitAndDeviceReset)
            {
                nodeMap.GetNode<ICommand>("DeviceReset").Execute();
                Console.WriteLine("DeviceReset command executed on camera number {0}.", camNumber.ToString());
            }
            else if (closeMethod == Util.CloseCameraMethod.DeInitAndFactoryReset)
            {
                nodeMap.GetNode<ICommand>("FactoryReset").Execute();
                Console.WriteLine("FactoryReset command executed on camera number {0}.", camNumber.ToString());
            }
        }
    }
}

