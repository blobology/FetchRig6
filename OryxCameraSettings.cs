using System;
using System.Collections.Generic;
using System.Text;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using System.Globalization;
using System.IO;

namespace FetchRig6
{
    public class OryxCameraSettings
    {
        private OryxCamera cam;
        private Dictionary<Util.OryxSettingName, Util.SettingInfo> settingsToLoad;
        private const int MAXCHARS = 35;
        private readonly string settingsFileName;

        public OryxCameraSettings(OryxCamera cam)
        {
            this.cam = cam;
            settingsFileName = this.cam.sessionPath + @"\" + "cam" + cam.camNumber.ToString() + @"_cameraSettings.txt";
            settingsToLoad = cam.setupInfo.settingsToLoad;

            foreach (KeyValuePair<Util.OryxSettingName, Util.SettingInfo> entry in settingsToLoad)
            {
                SetSetting(entry.Value);
            }

            EnableChunkData();
        }

        public void EnableChunkData()
        {
            cam.managedCamera.ChunkSelector.Value = ChunkSelectorEnums.Timestamp.ToString();
            cam.managedCamera.ChunkEnable.Value = true;
            cam.managedCamera.ChunkModeActive.Value = true;

            cam.managedCamera.ChunkSelector.Value = ChunkSelectorEnums.FrameID.ToString();
            cam.managedCamera.ChunkEnable.Value = true;
            cam.managedCamera.ChunkModeActive.Value = true;
        }

        private void SetSetting(Util.SettingInfo item)
        {
            string settingName = item._SettingName.ToString();
            Util.NodeType nodeType = item._NodeType;
            Util.NodeMap nodeMap = item._NodeMap;
            string value = item._Value;

            if (nodeType == Util.NodeType.String)
            {
                IString iNode = null;
                if (nodeMap == Util.NodeMap.GenICam)
                {
                    iNode = cam.nodeMap.GetNode<IString>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLDevice)
                {
                    iNode = cam.nodeMapTLDevice.GetNode<IString>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLStream)
                {
                    iNode = cam.nodeMapTLStream.GetNode<IString>(settingName);
                }

                string currentValue = string.Copy(iNode.Value.ToString());
                iNode.Value = value;
                string newValue = string.Copy(iNode.Value.ToString());
                printSettingChangeInfo(currentValue, newValue);
            }

            else if (nodeType == Util.NodeType.Integer)
            {
                IInteger iNode = null;
                if (nodeMap == Util.NodeMap.GenICam)
                {
                    iNode = cam.nodeMap.GetNode<IInteger>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLDevice)
                {
                    iNode = cam.nodeMapTLDevice.GetNode<IInteger>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLStream)
                {
                    iNode = cam.nodeMapTLStream.GetNode<IInteger>(settingName);
                }

                string currentValue = string.Copy(iNode.Value.ToString());
                iNode.Value = int.Parse(value);
                string newValue = string.Copy(iNode.Value.ToString());
                printSettingChangeInfo(currentValue, newValue);
            }

            else if (nodeType == Util.NodeType.Float)
            {
                IFloat iNode = null;
                if (nodeMap == Util.NodeMap.GenICam)
                {
                    iNode = cam.nodeMap.GetNode<IFloat>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLDevice)
                {
                    iNode = cam.nodeMapTLDevice.GetNode<IFloat>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLStream)
                {
                    iNode = cam.nodeMapTLStream.GetNode<IFloat>(settingName);
                }

                string currentValue = string.Copy(iNode.Value.ToString());
                iNode.Value = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                string newValue = string.Copy(iNode.Value.ToString());
                printSettingChangeInfo(currentValue, newValue);
            }

            else if (nodeType == Util.NodeType.Bool)
            {
                IBool iNode = null;
                if (nodeMap == Util.NodeMap.GenICam)
                {
                    iNode = cam.nodeMap.GetNode<IBool>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLDevice)
                {
                    iNode = cam.nodeMapTLDevice.GetNode<IBool>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLStream)
                {
                    iNode = cam.nodeMapTLStream.GetNode<IBool>(settingName);
                }

                string currentValue = string.Copy(iNode.Value.ToString());
                iNode.Value = bool.Parse(value);
                string newValue = string.Copy(iNode.Value.ToString());
                printSettingChangeInfo(currentValue, newValue);
            }

            else if (nodeType == Util.NodeType.Command)
            {
                ICommand iNode = null;
                if (nodeMap == Util.NodeMap.GenICam)
                {
                    iNode = cam.nodeMap.GetNode<ICommand>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLDevice)
                {
                    iNode = cam.nodeMapTLDevice.GetNode<ICommand>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLStream)
                {
                    iNode = cam.nodeMapTLStream.GetNode<ICommand>(settingName);
                }

                Console.WriteLine("Command to be executed: {0}: ", settingName);
                iNode.Execute();
            }

            else if (nodeType == Util.NodeType.Enumeration)
            {
                IEnum iNode = null;
                if (nodeMap == Util.NodeMap.GenICam)
                {
                    iNode = cam.nodeMap.GetNode<IEnum>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLDevice)
                {
                    iNode = cam.nodeMapTLDevice.GetNode<IEnum>(settingName);
                }
                else if (nodeMap == Util.NodeMap.TLStream)
                {
                    iNode = cam.nodeMapTLStream.GetNode<IEnum>(settingName);
                }

                string currentValue = string.Copy(iNode.Value);
                IEnumEntry iEntry = iNode.GetEntryByName(value);
                iNode.Value = iEntry.Symbolic;
                string newValue = string.Copy(iEntry.Symbolic);
                printSettingChangeInfo(currentValue, newValue);
            }



            void printSettingChangeInfo(string _currentValue, string _newValue, bool suppress=true)
            {
                if (!suppress)
                {
                    Console.WriteLine("{0} ({1}) changed from {2} to {3}", settingName, nodeType, _currentValue, _newValue);
                }
            }
        }

        string Indent(int level)
        {
            StringBuilder sb = new StringBuilder("");

            for (int i = 0; i < level; i++)
            {
                sb.Append("   ");
            }
            return sb.ToString();
        }


        // Retrieve display name and value of string node.
        private void PrintStringNode(INode node, int level, StreamWriter sw)
        {
            try
            {
                // Cast as string node
                IString iStringNode = (IString)node;

                // Retrieve display name
                string displayName = iStringNode.DisplayName;

                // Retrieve string node value
                string value = iStringNode.Value;

                // Check length is not excessive for printing
                if (value.Length > MAXCHARS)
                {
                    value = value.Substring(0, MAXCHARS) + "...";
                }

                sw.WriteLine(Indent(level) + displayName + ":  " + value + "  (String Node)");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error:   " + ex.ToString());
            }
        }

        private void PrintIntegerNode(INode node, int level, StreamWriter sw)
        {
            try
            {
                // Cast node as integer node
                IInteger iIntegerNode = (IInteger)node;

                // Retrieve display name
                string displayName = iIntegerNode.DisplayName;

                // Retrieve integer node value
                long value = iIntegerNode.Value;

                sw.WriteLine(Indent(level) + displayName + ":  " + value.ToString() + "  (Integer Node)");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error:   " + ex.ToString());
            }
        }

        private void PrintFloatNode(INode node, int level, StreamWriter sw)
        {
            try
            {
                // Cast node as integer node
                IFloat iFloatNode = (IFloat)node;

                // Retrieve display name
                string displayName = iFloatNode.DisplayName;

                // Retrieve integer node value
                double value = iFloatNode.Value;

                sw.WriteLine(Indent(level) + displayName + ":  " + value.ToString() + "  (Float Node)");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error:   " + ex.ToString());
            }
        }

        private void PrintBooleanNode(INode node, int level, StreamWriter sw)
        {
            try
            {
                // Cast as boolean node
                IBool iBooleanNode = (IBool)node;

                // Retrieve display name
                string displayName = iBooleanNode.DisplayName;

                // Retrieve value as a string representation
                string value = (iBooleanNode.Value ? "true" : "false");

                sw.WriteLine(Indent(level) + displayName + ":  " + value + "  (Bool Node)");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error:   " + ex.ToString());
            }
        }

        private void PrintCommandNode(INode node, int level, StreamWriter sw)
        {
            try
            {
                // Cast as command node
                ICommand iCommandNode = (ICommand)node;

                // Retrieve display name
                string displayName = iCommandNode.DisplayName;

                // Retrieve tooltip
                string tooltip = iCommandNode.ToolTip;

                // Ensure that the value length is not excessive for printing
                if (tooltip.Length > MAXCHARS)
                {
                    tooltip = tooltip.Substring(0, MAXCHARS) + "...";
                }
                sw.WriteLine(Indent(level) + displayName + ":  " + tooltip + "  (Command Node)");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error:   " + ex.ToString());
            }
        }

        private void PrintEnumerationNodeAndCurrentEntry(INode node, int level, StreamWriter sw)
        {
            try
            {
                // Cast as enumeration node
                IEnum iEnumerationNode = (IEnum)node;

                // Retrieve current entry as enumeration node
                EnumValue iEnumEntryValue = iEnumerationNode.Value;

                // Retrieve display name
                string displayName = iEnumerationNode.DisplayName;

                // Retrieve current symbolic
                string currentEntrySymbolic = iEnumEntryValue.String;

                sw.WriteLine(Indent(level) + displayName + ":  " + currentEntrySymbolic + "  (Enumeration Node)");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error:   " + ex.ToString());
            }
        }

        private void PrintCategoryNodeAndAllFeatures(INode node, int level, StreamWriter sw)
        {
            try
            {
                // Cast as category node
                ICategory iCategoryNode = (ICategory)node;

                // Retrieve display name
                string displayName = iCategoryNode.DisplayName;

                sw.WriteLine(Indent(level) + displayName + "  (Category Node)");

                // Retrieve children
                //
                // *** NOTES ***
                // The two nodes that typically have children are category nodes
                // and enumeration nodes. Throughout the examples, the children
                // of category nodes are referred to as features while the
                // children of enumeration nodes are referred to as entries.
                // Keep in mind that enumeration nodes can be cast as category
                // nodes, but category nodes cannot be cast as enumerations.
                //
                INode[] features = iCategoryNode.Features;

                // Iterate through all children
                foreach (INode iFeatureNode in features)
                {
                    // Ensure node is available and readable
                    if (!iFeatureNode.IsAvailable || !iFeatureNode.IsReadable)
                    {
                        continue;
                    }

                    // Category nodes must be dealt with separately in order to
                    // retrieve subnodes recursively.
                    if (iFeatureNode.GetType() == typeof(Category))
                    {
                        PrintCategoryNodeAndAllFeatures(iFeatureNode, level + 1, sw);
                    }
                    else if (iFeatureNode.GetType() == typeof(StringReg))
                    {
                        PrintStringNode(iFeatureNode, level + 1, sw);
                    }
                    else if (iFeatureNode.GetType() == typeof(Integer))
                    {
                        PrintIntegerNode(iFeatureNode, level + 1, sw);
                    }
                    else if (iFeatureNode.GetType() == typeof(Float))
                    {
                        PrintFloatNode(iFeatureNode, level + 1, sw);
                    }
                    else if (iFeatureNode.GetType() == typeof(BoolNode))
                    {
                        PrintBooleanNode(iFeatureNode, level + 1, sw);
                    }
                    else if (iFeatureNode.GetType() == typeof(Command))
                    {
                        PrintCommandNode(iFeatureNode, level + 1, sw);
                    }
                    else if (iFeatureNode.GetType() == typeof(Enumeration))
                    {
                        PrintEnumerationNodeAndCurrentEntry(iFeatureNode, level + 1, sw);
                    }
                }
                sw.WriteLine();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error:   " + ex.ToString());
            }
        }

        public void SaveSettings(bool _printSettings = false)
        {
            try
            {
                // Check if file already exists. If yes, delete it.
                if (File.Exists(settingsFileName))
                {
                    File.Delete(settingsFileName);
                }

                // Create a new file.
                using (StreamWriter sw = File.CreateText(settingsFileName))
                {
                    int level = 0;

                    try
                    {
                        sw.WriteLine("\n*** TRANSPORT LAYER DEVICE NODEMAP ***\n");
                        PrintCategoryNodeAndAllFeatures(cam.nodeMapTLDevice.GetNode<ICategory>("Root"), level, sw);

                        sw.WriteLine("\n*** TRANSPORT LAYER STREAM NODEMAP ***\n");
                        PrintCategoryNodeAndAllFeatures(cam.nodeMapTLStream.GetNode<ICategory>("Root"), level, sw);

                        sw.WriteLine("\n*** GENICAM NODEMAP ***\n");
                        PrintCategoryNodeAndAllFeatures(cam.nodeMap.GetNode<ICategory>("Root"), level, sw);
                    }
                    catch (SpinnakerException ex)
                    {
                        Console.WriteLine("Error: {0}", ex.ToString());
                    }
                }

                // Write file contents on console if desired.     
                if (_printSettings)
                {
                    using (StreamReader sr = File.OpenText(settingsFileName))
                    {
                        string s = "";
                        while ((s = sr.ReadLine()) != null)
                        {
                            Console.WriteLine(s);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

}

