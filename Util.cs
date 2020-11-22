using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using Emgu.CV.Util;
using System.Text;
using System.ComponentModel.Design;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace FetchRig6
{
    public enum MainDisplayLoopState
    {
        Waiting,
        Streaming,
        Recording,
        Exit
    }

    public enum BasicCamLoopState
    {
        Waiting,
        Streaming,
        Recording,
        Exit
    }

    public enum MessageHandlerStyle
    {
        Basic,
        Advanced
    }

    public enum CameraLoopState
    {
        WaitingForMessagesWhileNotStreaming,
        BeginStreaming,
        StreamingAndWaitingForMessages,
        InitiateFFProcessAndRecord,
        StreamingAndRecordingWhileWaitingForMessages,
        InitiateProcessTermination,
        EndAcquisition,
        Exit
    }

    public enum ProcessingLoopState
    {
        Waiting,
        Streaming,
        Exit
    }

    public struct FrameMetaData
    {
        public int streamCtr { get; }
        public long frameID { get; }
        public long timestamp { get; }

        public FrameMetaData(int streamCtr, long frameID, long timestamp)
        {
            this.streamCtr = streamCtr;
            this.frameID = frameID;
            this.timestamp = timestamp;
        }
    }

    public static class ObjectExtensions
    {
        // Your class MUST be marked as [Serializable] for this to work:
        public static T DeepClone<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }
    }

    public class Util
    {
        public static Tuple<Mat, FrameMetaData> GetMetaMatTuple(Mat mat, FrameMetaData metaData)
        {
            return Tuple.Create(item1: mat, item2: metaData);
        }

        public static Tuple<Mat, FrameMetaData>[] GetStreamOutput(Mat mat, FrameMetaData metaData)
        {
            return new Tuple<Mat, FrameMetaData>[1] { Tuple.Create(item1: mat, item2: metaData) };
        }

        public static Tuple<Mat, FrameMetaData>[] GetStreamOutput(Tuple<Mat, FrameMetaData> metaMat1, Tuple<Mat, FrameMetaData> metaMat2)
        {
            return new Tuple<Mat, FrameMetaData>[2] { metaMat1, metaMat2 };
        }

        public static bool CheckForEncodables(bool[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i]) { return true; }
            }
            return false;
        }

        public static bool CheckForEncodables(bool[] input1, bool[] input2)
        {
            for (int i = 0; i < input1.Length; i++)
            {
                if (input1[i]) { return true; }
            }
            for (int i = 0; i < input2.Length; i++)
            {
                if (input2[i]) { return true; }
            }
            return false;
        }

        public static bool[] CheckForEncodables(StreamManager.StreamChannel streamChannel)
        {
            bool[] result = new bool[1];
            if (streamChannel.isEncodeable) { result[0] = true; }
            else { result[0] = false; }
            return result;
        }

        public static bool[] CheckForEncodables(StreamManager.StreamChannel[] streamChannels)
        {
            bool[] result = new bool[streamChannels.Length];
            for (int i = 0; i < streamChannels.Length; i++)
            {
                if (streamChannels[i].isEncodeable) { result[i] = true; }
                else { result[i] = false; }
            }
            return result;
        }

        public static string[] SetDataWritePaths(AnimalName animalName, int nCameras=2)
        {
            string[] result = new string[nCameras];
            string[] drives = Enum.GetNames(typeof(HardDrive));
            string[] projectFolder = Enum.GetNames(typeof(ProjectFolder));
            string today = DateTime.Now.ToString("yyyy_MM_dd");
            string session = DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss");
            for (int i = 0; i < nCameras; i++)
            {
                string todayPath = drives[i] + @":\" + projectFolder[0] + @"\Animal\" + animalName + @"\" + today;
                string sessionPath = todayPath + @"\" + session;

                if (!Directory.Exists(todayPath))
                {
                    Directory.CreateDirectory(todayPath);
                    Console.WriteLine("Created Directory for today for writing video files:  {0}", todayPath);
                }
                if (!Directory.Exists(sessionPath))
                {
                    Directory.CreateDirectory(sessionPath);
                    Console.WriteLine("Created Directory for today for writing video files:  {0}", sessionPath);
                }
                result[i] = sessionPath;
            }
            return result;
        }

        public enum NodeType
        {
            String,
            Integer,
            Float,
            Bool,
            Command,
            Enumeration
        }

        public enum NodeMap
        {
            GenICam,
            TLDevice,
            TLStream,
        }

        public enum AnimalName
        {
            Charlie,
            Mac
        }

        public enum HardDrive
        {
            D,
            E
        }

        public enum ProjectFolder
        {
            FetchRig2
        }

        public enum CloseCameraMethod
        {
            DeInit,
            DeInitAndDeviceReset,
            DeInitAndFactoryReset
        }

        public enum OryxSettingName
        {
            PixelFormat,
            AdcBitDepth,
            StreamBufferCountMode,
            StreamBufferCountManual,
            StreamBufferHandlingMode,
            DeviceLinkThroughputLimit,
            AcquisitionMode,
            Width,
            Height,
            OffsetX,
            OffsetY,
            ExposureAuto,
            ExposureMode,
            ExposureTime,
            GainAuto,
            Gain,
            GammaEnable,
            Gamma,
            AcquisitionFrameRateEnable,
            AcquisitionFrameRate,
            GevDeviceAutoForceIP,
            DeviceReset,
            FactoryReset
        }

        public enum OryxSetupSettingName
        {
            PixelFormat,
            AdcBitDepth,
            StreamBufferHandlingMode,
            StreamBufferCountMode,
            StreamBufferCountManual,
            DeviceLinkThroughputLimit,
            AcquisitionMode,
            Width,
            Height,
            OffsetX,
            OffsetY,
            ExposureAuto,
            ExposureMode,
            ExposureTime,
            GainAuto,
            Gain,
            GammaEnable,
            Gamma,
            AcquisitionFrameRateEnable,
            AcquisitionFrameRate
        }

        public enum PixelFormat
        {
            Mono8,
            Mono16,
            Mono10Packed,
            Mono12Packed,
            Mono10p,
            Mono12p
        }

        public enum AdcBitDepth
        {
            Bit8,
            Bit10,
            Bit12
        }

        public enum StreamBufferCountMode
        {
            Manual,
            Auto
        }

        public enum StreamBufferHandlingMode
        {
            OldestFirst,
            OldestFirstOverwrite,
            NewestOnly,
            NewestFirst
        }

        public enum AcquisitionMode
        {
            Continuous,
            SingleFrame,
            MultiFrame
        }

        public enum ExposureAuto
        {
            Off,
            Once,
            Continuous
        }

        public enum ExposureMode
        {
            Timed,
            TriggerWidth
        }

        public enum GainAuto
        {
            Off,
            Once,
            Continuous
        }

        public enum GevDeviceAutoForceIP
        {
            Execute
        }

        public enum DeviceReset
        {
            Execute
        }

        public enum FactoryReset
        {
            Execute
        }

        public class SettingInfo
        {
            public SettingInfo(OryxSettingName settingName, NodeType nodeType, NodeMap nodeMap, string value)
            {
                _SettingName = settingName;
                _NodeType = nodeType;
                _NodeMap = nodeMap;
                _Value = value;
            }

            public OryxSettingName _SettingName { get; }
            public NodeType _NodeType { get; }
            public NodeMap _NodeMap { get; }
            public string _Value { get; set; }
        }

        [Serializable]
        public class OryxSetupInfo
        {
            public SetupStyleEnum setupStyle;
            public readonly Dictionary<OryxSettingName, SettingInfo> standardSetupDict;
            public Dictionary<OryxSettingName, SettingInfo> settingsToLoad;
            public Size maxFrameSize { get; }
            public Size frameSize { get; private set; }
            public bool centerROI { get; set; }


            public OryxSetupInfo()
            {
                maxFrameSize = new Size(width: 3208, height: 2200);
                frameSize = new Size(width: 3208, height: 2200);
                setupStyle = SetupStyleEnum.Standard;
                centerROI = true;
                standardSetupDict = new Dictionary<OryxSettingName, SettingInfo>(capacity: Enum.GetValues(typeof(OryxSetupSettingName)).Length);
                BuildStandardSetupDict();
                settingsToLoad = new Dictionary<OryxSettingName, SettingInfo>(standardSetupDict);
            }

            public OryxSetupInfo(Dictionary<OryxSettingName, SettingInfo> customSetupDict, SetupStyleEnum setupStyle, bool centerROI = true)
            {
                maxFrameSize = new Size(width: 3208, height: 2200);
                frameSize = new Size(width: 3208, height: 2200);
                this.setupStyle = setupStyle;
                this.centerROI = centerROI;

                if (setupStyle == SetupStyleEnum.AppendSettingsToStandard)
                {
                    standardSetupDict = new Dictionary<OryxSettingName, SettingInfo>(capacity: Enum.GetValues(typeof(OryxSetupSettingName)).Length);
                    BuildStandardSetupDict();

                    settingsToLoad = new Dictionary<OryxSettingName, SettingInfo>(standardSetupDict);
                    customSetupDict.ToList().ForEach(x => settingsToLoad.Add(x.Key, x.Value));
                    customSetupDict = null;
                    CheckFrameSettings();
                }
                else if (setupStyle == SetupStyleEnum.ReplaceStandard)
                {
                    settingsToLoad = new Dictionary<OryxSettingName, SettingInfo>(customSetupDict);
                    customSetupDict = null;
                    CheckFrameSettings();
                }
            }

            public enum SetupStyleEnum
            {
                Standard,
                AppendSettingsToStandard,
                ReplaceStandard
            }

            public void CheckFrameSettings()
            {
                int width = int.Parse(settingsToLoad[OryxSettingName.Width]._Value);
                int height = int.Parse(settingsToLoad[OryxSettingName.Height]._Value);
                int offsetX = int.Parse(settingsToLoad[OryxSettingName.OffsetX]._Value);
                int offsetY = int.Parse(settingsToLoad[OryxSettingName.OffsetY]._Value);

                Size _fullFrameSize = new Size(width: frameSize.Width, height: frameSize.Height);

                if (width < maxFrameSize.Width)
                {
                    (int updatedWidth, int updatedOffsetX) = GetDimLengthAndOffset(_nMaxPixels: maxFrameSize.Width, _nPixels: width, _offset: offsetX);
                    settingsToLoad[OryxSettingName.Width]._Value = updatedWidth.ToString();
                    settingsToLoad[OryxSettingName.OffsetX]._Value = updatedOffsetX.ToString();
                    _fullFrameSize.Width = updatedWidth;

                    if (width != updatedWidth)
                    {
                        Console.WriteLine("Invalid Width Setting. Changed from to {0} to {1} to be divisible by 16", width.ToString(), updatedWidth.ToString());
                    }
                    if (offsetX != updatedOffsetX)
                    {
                        Console.WriteLine("OffsetX changed from {0} to {1}.", offsetX.ToString(), updatedOffsetX.ToString());
                    }
                }

                if (height < maxFrameSize.Height)
                {
                    (int updatedHeight, int updatedOffsetY) = GetDimLengthAndOffset(_nMaxPixels: maxFrameSize.Height, _nPixels: height, _offset: offsetY);
                    settingsToLoad[OryxSettingName.Height]._Value = updatedHeight.ToString();
                    settingsToLoad[OryxSettingName.OffsetY]._Value = updatedOffsetY.ToString();
                    _fullFrameSize.Height = updatedHeight;

                    if (height != updatedHeight)
                    {
                        Console.WriteLine("Invalid Height Setting. Changed from to {0} to {1} to be divisible by 16", height.ToString(), updatedHeight.ToString());
                    }
                    if (offsetY != updatedOffsetY)
                    {
                        Console.WriteLine("OffsetY changed from {0} to {1}.", offsetY.ToString(), updatedOffsetY.ToString());
                    }
                }

                frameSize = _fullFrameSize;

                (int, int) GetDimLengthAndOffset(int _nMaxPixels, int _nPixels, int _offset)
                {
                    int nPixels = _nPixels / 16 * 16;
                    int offset;
                    if (centerROI)
                    {
                        offset = (_nMaxPixels - _nPixels) / 2;
                    }
                    else
                    {
                        offset = _offset / 8 * 8;
                    }

                    return (nPixels, offset);
                }
            }

            public void BuildStandardSetupDict()
            {
                standardSetupDict.Add(OryxSettingName.PixelFormat,
                    new SettingInfo(OryxSettingName.PixelFormat, NodeType.Enumeration, NodeMap.GenICam, value: nameof(PixelFormat.Mono8)));

                standardSetupDict.Add(OryxSettingName.AdcBitDepth,
                    new SettingInfo(OryxSettingName.AdcBitDepth, NodeType.Enumeration, NodeMap.GenICam, value: nameof(AdcBitDepth.Bit8)));

                standardSetupDict.Add(OryxSettingName.StreamBufferHandlingMode,
                    new SettingInfo(OryxSettingName.StreamBufferHandlingMode, NodeType.Enumeration, NodeMap.TLStream, value: nameof(StreamBufferHandlingMode.OldestFirst)));

                standardSetupDict.Add(OryxSettingName.StreamBufferCountMode,
                    new SettingInfo(OryxSettingName.StreamBufferCountMode, NodeType.Enumeration, NodeMap.TLStream, value: nameof(StreamBufferCountMode.Manual)));

                standardSetupDict.Add(OryxSettingName.StreamBufferCountManual,
                    new SettingInfo(OryxSettingName.StreamBufferCountManual, NodeType.Integer, NodeMap.TLStream, value: "256"));

                standardSetupDict.Add(OryxSettingName.AcquisitionMode,
                    new SettingInfo(OryxSettingName.AcquisitionMode, NodeType.Enumeration, NodeMap.GenICam, value: nameof(AcquisitionMode.Continuous)));

                standardSetupDict.Add(OryxSettingName.DeviceLinkThroughputLimit,
                    new SettingInfo(OryxSettingName.DeviceLinkThroughputLimit, NodeType.Integer, NodeMap.GenICam, value: "1250000000"));

                standardSetupDict.Add(OryxSettingName.OffsetX,
                    new SettingInfo(OryxSettingName.OffsetX, NodeType.Integer, NodeMap.GenICam, value: "0"));

                standardSetupDict.Add(OryxSettingName.OffsetY,
                    new SettingInfo(OryxSettingName.OffsetY, NodeType.Integer, NodeMap.GenICam, value: "0"));

                standardSetupDict.Add(OryxSettingName.Width,
                    new SettingInfo(OryxSettingName.Width, NodeType.Integer, NodeMap.GenICam, value: "3208"));

                standardSetupDict.Add(OryxSettingName.Height,
                    new SettingInfo(OryxSettingName.Height, NodeType.Integer, NodeMap.GenICam, value: "2200"));

                standardSetupDict.Add(OryxSettingName.ExposureAuto,
                    new SettingInfo(OryxSettingName.ExposureAuto, NodeType.Enumeration, NodeMap.GenICam, value: nameof(ExposureAuto.Off)));

                standardSetupDict.Add(OryxSettingName.ExposureMode,
                    new SettingInfo(OryxSettingName.ExposureMode, NodeType.Enumeration, NodeMap.GenICam, value: nameof(ExposureMode.Timed)));

                standardSetupDict.Add(OryxSettingName.ExposureTime,
                    new SettingInfo(OryxSettingName.ExposureTime, NodeType.Float, NodeMap.GenICam, value: "1250.0"));

                standardSetupDict.Add(OryxSettingName.GainAuto,
                    new SettingInfo(OryxSettingName.GainAuto, NodeType.Enumeration, NodeMap.GenICam, value: nameof(GainAuto.Off)));

                standardSetupDict.Add(OryxSettingName.Gain,
                    new SettingInfo(OryxSettingName.Gain, NodeType.Float, NodeMap.GenICam, value: "19.0"));

                standardSetupDict.Add(OryxSettingName.GammaEnable,
                    new SettingInfo(OryxSettingName.GammaEnable, NodeType.Bool, NodeMap.GenICam, value: "False"));

                standardSetupDict.Add(OryxSettingName.AcquisitionFrameRateEnable,
                    new SettingInfo(OryxSettingName.AcquisitionFrameRateEnable, NodeType.Bool, NodeMap.GenICam, value: "True"));

                standardSetupDict.Add(OryxSettingName.AcquisitionFrameRate,
                    new SettingInfo(OryxSettingName.AcquisitionFrameRate, NodeType.Float, NodeMap.GenICam, value: "100.0"));
            }

            public void PrintSettingsToLoad()
            {
                foreach (KeyValuePair<OryxSettingName, SettingInfo> entry in settingsToLoad)
                {
                    Console.WriteLine("OryxSettingName: {0}    NodeType: {1}    NodeMap: {2}    Value: {3}",
                        entry.Value._SettingName,
                        entry.Value._NodeType,
                        entry.Value._NodeMap,
                        entry.Value._Value);
                }
            }
        }

        public static void CloseOryxCamera(IManagedCamera managedCamera, INodeMap nodeMap, int camNumber, CloseCameraMethod closeMethod)
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

            if (closeMethod == CloseCameraMethod.DeInit)
            {
                managedCamera.DeInit();
                Console.WriteLine("Camera number {0} deinitialized.", camNumber.ToString());
            }
            else if (closeMethod == CloseCameraMethod.DeInitAndDeviceReset)
            {
                nodeMap.GetNode<ICommand>("DeviceReset").Execute();
                Console.WriteLine("DeviceReset command executed on camera number {0}.", camNumber.ToString());
            }
            else if (closeMethod == CloseCameraMethod.DeInitAndFactoryReset)
            {
                nodeMap.GetNode<ICommand>("FactoryReset").Execute();
                Console.WriteLine("FactoryReset command executed on camera number {0}.", camNumber.ToString());
            }
        }
    }
}
