﻿using System;
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


namespace FetchRig6
{
    public partial class Form1 : Form
    {
        internal const int nCameras = 2;
        internal MainDisplayLoopState mainDisplayLoopState;

        private ManagedSystem system;
        private IList<IManagedCamera> managedCameras;
        private Util.OryxSetupInfo[] oryxSetupInfos;
        private ThreadManager threadManager;
        private XBoxController xBoxController;
        private string[] sessionPaths;
        private System.Windows.Forms.Timer displayTimer;
        private ConcurrentQueue<Tuple<Mat, FrameMetaData>[]> displayQueue;

        public Form1()
        {
            InitializeComponent();

            system = new ManagedSystem();

            // Print current Spinnaker library version info:
            LibraryVersion spinVersion = system.GetLibraryVersion();
            Console.WriteLine(
                "Spinnaker library version: {0}.{1}.{2}.{3}\n\n",
                spinVersion.major,
                spinVersion.minor,
                spinVersion.type,
                spinVersion.build);

            // Find all Flir cameras on the system:
            managedCameras = system.GetCameras();

            // Assert that exactly two cameras are found:
            int nCamsFound = managedCameras.Count;
            if (nCamsFound != nCameras)
            {
                Console.WriteLine("Need exactly two cameras, but {0} cameras were found. Disposing system.", nCamsFound.ToString());
                managedCameras.Clear();
                system.Dispose();
            }

            // Create or select folder to write video data:
            sessionPaths = Util.SetDataWritePaths(animalName: Util.AnimalName.Charlie);

            // Initialize OryxSetupInfo Object to pass to camera constructors upon initialization:
            oryxSetupInfos = new Util.OryxSetupInfo[nCameras];
            for (int i = 0; i < nCameras; i++)
            {
                oryxSetupInfos[i] = new Util.OryxSetupInfo();
            }

            bool areAllCamSettingsIdentical = true;
            if (areAllCamSettingsIdentical)
            {
                Console.WriteLine("\n\n");
                Console.WriteLine("Cameras have identical settings, shown here:");
                oryxSetupInfos[0].PrintSettingsToLoad();
                Console.WriteLine("\n\n");
            }

            StreamArchitecture architecture = StreamArchitecture.ThreeLevelBasic;

            threadManager = new ThreadManager(architecture: architecture, sessionPaths: sessionPaths,
                managedCameras: managedCameras, oryxSetups: oryxSetupInfos);

            xBoxController = new XBoxController(mainForm: this,
                messageQueues: threadManager.managerBundle.messageQueues, streamGraph: threadManager.streamGraph);

            displayTimer = new System.Windows.Forms.Timer();
            displayTimer.Interval = 2;
            displayTimer.Tick += DisplayTimerEventProcessor;
            displayTimer.Enabled = true;

            displayQueue = threadManager.managerBundle.mergeStreamsManager.output.displayQueue;
            threadManager.StartThreads();
        }

        internal void Exit()
        {
            // TODO: call this when exiting program
        }

        public void DisplayTimerEventProcessor(Object sender, EventArgs e)
        {
            xBoxController.controllerState.Update();
            bool isDequeueSuccess;

            isDequeueSuccess = displayQueue.TryDequeue(out Tuple<Mat, FrameMetaData>[] result);
            if (isDequeueSuccess)
            {
                imageBox1.Image = result[0].Item1;
            }
        }
    }
}
