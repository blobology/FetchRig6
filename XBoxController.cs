using System;
using System.Linq;
using System.Collections.Concurrent;
using SharpDX.XInput;
using System.IO.Ports;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;

namespace FetchRig6
{
    public enum ControllableButtons
    {
        DPadUp = 0,
        LeftShoulder = 1,
        RightShoulder = 2,
        X = 3,
        Y = 4,
        A = 5,
        B = 6,
        DPadRight = 7,
        DPadDown = 8,
        DPadLeft = 9
    }

    public enum ButtonCommands
    {
        BeginAcquisition = 0,
        BeginStreaming = 1,
        EndStreaming = 2,
        StartRecording = 3,
        StopRecording = 4,
        PlayRewardTone = 5,
        PlayInitiateTrialTone = 6,
        ResetBackgroundImage = 7,
        SaveThisImage = 8,
        Exit = 9
    }

    public class XBoxController
    {
        public const int nControllableButtons = 10;
        private Form1 mainForm;
        private ConcurrentQueue<ButtonCommands>[][] messageQueues;
        private ThreadManager.StreamGraph streamGraph;
        private Controller controller;
        public ControllerState controllerState;
        private string serialPortName = "COM3";
        private SerialPort serialPort;

        public XBoxController(Form1 mainForm, ConcurrentQueue<ButtonCommands>[][] messageQueues, ThreadManager.StreamGraph streamGraph)
        {
            this.mainForm = mainForm;
            this.messageQueues = messageQueues;
            this.streamGraph = streamGraph;
            controller = new Controller(userIndex: UserIndex.One);
            serialPort = new SerialPort(portName: serialPortName, baudRate: 115200, parity: Parity.None, dataBits: 8, stopBits: StopBits.One);
            serialPort.Open();
            controllerState = new ControllerState(this);
        }

        public class ControllerState
        {
            XBoxController xBoxController;
            State state;
            bool[] prevButtonStates;
            bool[] currButtonStates;
            GamepadButtonFlags[] gamepadButtonFlags;
            string[] controllableButtonNames;
            string[] controllableButtonCommands;

            ButtonCommands[] soundButtons;
            ButtonCommands[] camButtons;
            ButtonCommands[] displayButtons;
            ButtonCommands[] streamProcessingButtons;

            List<(int, int)> camMsgIdx;
            List<(int, int)> streamMsgIdx;

            public ControllerState(XBoxController xBoxController)
            {
                this.xBoxController = xBoxController;
                state = new State();
                prevButtonStates = new bool[nControllableButtons];
                currButtonStates = new bool[nControllableButtons];
                controllableButtonNames = Enum.GetNames(typeof(ControllableButtons));
                controllableButtonCommands = Enum.GetNames(typeof(ButtonCommands));
                gamepadButtonFlags = new GamepadButtonFlags[nControllableButtons];

                for (int i = 0; i < nControllableButtons; i++)
                {
                    gamepadButtonFlags[i] = (GamepadButtonFlags)Enum.Parse(typeof(GamepadButtonFlags), controllableButtonNames[i]);
                }

                soundButtons = new ButtonCommands[3]
                {
                    ButtonCommands.PlayInitiateTrialTone,
                    ButtonCommands.PlayRewardTone,
                    ButtonCommands.Exit
                };

                camButtons = new ButtonCommands[6]
                {
                    ButtonCommands.BeginAcquisition,
                    ButtonCommands.BeginStreaming,
                    ButtonCommands.StartRecording,
                    ButtonCommands.StopRecording,
                    ButtonCommands.EndStreaming,
                    ButtonCommands.Exit
                };

                streamProcessingButtons = new ButtonCommands[5]
                {
                    ButtonCommands.BeginStreaming,
                    ButtonCommands.EndStreaming,
                    ButtonCommands.ResetBackgroundImage,
                    ButtonCommands.SaveThisImage,
                    ButtonCommands.Exit
                };

                displayButtons = new ButtonCommands[10]
                {
                    ButtonCommands.BeginAcquisition,
                    ButtonCommands.BeginStreaming,
                    ButtonCommands.EndStreaming,
                    ButtonCommands.StartRecording,
                    ButtonCommands.StopRecording,
                    ButtonCommands.PlayRewardTone,
                    ButtonCommands.PlayInitiateTrialTone,
                    ButtonCommands.ResetBackgroundImage,
                    ButtonCommands.SaveThisImage,
                    ButtonCommands.Exit
                };

                camMsgIdx = new List<(int, int)>();
                streamMsgIdx = new List<(int, int)>();
                SetMessageDirections();
            }

            public void SetMessageDirections()
            {
                ThreadType[][] g = xBoxController.streamGraph.graph;
                for (int i = 0; i < g.Length; i++)
                {
                    for (int j = 0; j < g[i].Length; j++)
                    {
                        int _i = i;
                        int _j = j;
                        if (g[i][j] == ThreadType.Camera)
                        {
                            camMsgIdx.Add((_i, _j));
                        }
                        else if (g[i][j] == ThreadType.SingleCameraStream || g[i][j] == ThreadType.MergeStreams)
                        {
                            streamMsgIdx.Add((_i, _j));
                        }
                    }
                }
            }

            public void Update()
            {
                xBoxController.controller.GetState(state: out state);
                currButtonStates.CopyTo(array: prevButtonStates, index: 0);
                for (int i = 0; i < nControllableButtons; i++)
                {
                    currButtonStates[i] = state.Gamepad.Buttons.HasFlag(gamepadButtonFlags[i]);
                }

                for (int i = 0; i < nControllableButtons; i++)
                {
                    if (prevButtonStates[i] == false && currButtonStates[i] == true)
                    {
                        
                        ButtonCommands buttonCommand = (ButtonCommands)Enum.Parse(typeof(ButtonCommands), controllableButtonCommands[i]);

                        Console.WriteLine(buttonCommand);

                        if (camButtons.Contains(buttonCommand))
                        {
                            ButtonCommands message;
                            foreach(var idx in camMsgIdx)
                            {
                                message = buttonCommand;
                                xBoxController.messageQueues[idx.Item1][idx.Item2].Enqueue(message);
                                Console.WriteLine("sending message from xBox controller to camera: {0}", message);
                            }
                        }

                        if (streamProcessingButtons.Contains(buttonCommand))
                        {
                            ButtonCommands message;
                            foreach (var idx in streamMsgIdx)
                            {
                                message = buttonCommand;
                                xBoxController.messageQueues[idx.Item1][idx.Item2].Enqueue(message);
                            }
                        }

                        if (soundButtons.Contains(buttonCommand))
                        {
                            string serialMessage;
                            if (buttonCommand == ButtonCommands.PlayInitiateTrialTone)
                            {
                                serialMessage = "initiate_trial";
                                xBoxController.serialPort.Write(text: serialMessage);
                            }
                            else if (buttonCommand == ButtonCommands.PlayRewardTone)
                            {
                                serialMessage = "reward";
                                xBoxController.serialPort.Write(text: serialMessage);
                            }
                            else if (buttonCommand == ButtonCommands.Exit)
                            {
                                serialMessage = "exit";
                                xBoxController.serialPort.Write(text: serialMessage);
                                xBoxController.serialPort.Close();
                            }
                        }

                        if (displayButtons.Contains(buttonCommand))
                        {
                            if (buttonCommand == ButtonCommands.BeginStreaming)
                            {
                                xBoxController.mainForm.mainDisplayLoopState = MainDisplayLoopState.Streaming;
                            }
                            else if (buttonCommand == ButtonCommands.StartRecording)
                            {
                                xBoxController.mainForm.mainDisplayLoopState = MainDisplayLoopState.Recording;
                            }
                            else if (buttonCommand == ButtonCommands.StopRecording)
                            {
                                xBoxController.mainForm.mainDisplayLoopState = MainDisplayLoopState.Streaming;
                            }
                            else if (buttonCommand == ButtonCommands.EndStreaming)
                            {
                                xBoxController.mainForm.mainDisplayLoopState = MainDisplayLoopState.Waiting;
                            }
                            else if (buttonCommand == ButtonCommands.Exit)
                            {
                                xBoxController.mainForm.Exit();
                            }
                        }
                    }
                }
            }
        }
    }
}
