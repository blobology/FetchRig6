using System;
using System.Linq;
using System.Collections.Concurrent;
using SharpDX.XInput;
using System.IO.Ports;

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
        private ConcurrentQueue<ButtonCommands>[][] camControlMessageQueues;
        private Controller controller;
        public ControllerState controllerState;
        private string serialPortName = "COM3";
        private SerialPort serialPort;

        public XBoxController(Form1 mainForm, ConcurrentQueue<ButtonCommands>[][] camControlMessageQueues)
        {
            this.mainForm = mainForm;
            this.camControlMessageQueues = camControlMessageQueues;
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

                        if (camButtons.Contains(buttonCommand))
                        {
                            ButtonCommands message;
                            for (int j = 0; j < Form1.nCameras; j++)
                            {
                                message = buttonCommand;
                                xBoxController.camControlMessageQueues[0][j].Enqueue(message);
                            }
                        }

                        if (streamProcessingButtons.Contains(buttonCommand))
                        {
                            ButtonCommands message;
                            for (int j = 0; j < Form1.nCameras; j++)
                            {
                                message = buttonCommand;
                                xBoxController.camControlMessageQueues[1][j].Enqueue(message);
                            }

                            message = buttonCommand;
                            xBoxController.camControlMessageQueues[2][0].Enqueue(message);
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
