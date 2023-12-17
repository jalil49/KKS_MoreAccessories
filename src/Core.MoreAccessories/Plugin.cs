using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using MoreAccessoriesKOI.Extensions;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;
#if KK || KKS
using Manager;
using Studio;
#endif
#if EC || KKS
using System.Collections.Generic;
#endif
namespace MoreAccessoriesKOI
{
    [BepInPlugin(GUID: GUID, Name: "MoreAccessories", Version: versionNum)]
    [BepInDependency(ExtendedSave.GUID)]
    [BepInDependency(Sideloader.Sideloader.GUID)]
    public partial class MoreAccessories : BaseUnityPlugin
    {
        #region Unity Methods
        private void Awake()
        {
            _self = this;
#if KKS
            ExtendedSave.CardBeingImported += CharacterBeingImported;
#elif EC
            ExtendedSave.CardBeingImported += CharacterBeingImported;
            ExtendedSave.CoordinateBeingImported += CoordinateBeingImported;
#endif

            SceneManager.sceneLoaded += LevelLoaded;

            _hasDarkness = typeof(ChaControl).GetMethod("ChangeShakeAccessory", AccessTools.all) != null;
            var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            var uarHooks = typeof(Sideloader.AutoResolver.UniversalAutoResolver).GetNestedType("Hooks", AccessTools.all);
            harmony.Patch(uarHooks.GetMethod("ExtendedCardLoad", AccessTools.all), new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCardLoad_Prefix)));
            harmony.Patch(uarHooks.GetMethod("ExtendedCoordinateLoad", AccessTools.all), new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCoordLoad_Prefix)));
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
#if DEBUG
            foreach (var item in harmony.GetPatchedMethods())
            {
                Print($"{item.ReflectedType}.{item.Name}");
            }
#endif
            ExtendedSave.CardBeingSaved += OnActualCharaSave;
            ExtendedSave.CoordinateBeingSaved += OnActualCoordSave;
#if EC
            ExtendedSave.HEditDataBeingSaved += ExtendedSave_HEditDataBeingSaved;
            ExtendedSave.HEditDataBeingLoaded += ExtendedSave_HEditDataBeingLoaded;

            SceneCreateAccessoryNames = Config.Bind("Scene Creation", "Use Accessory Name", true);
#endif
        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            switch (arg0.name)
            {
                case "CustomScene":
                    MakerMode = null;
                    break;
                default:
                    break;
            }
        }

#if DEBUG
        internal static void Print(string text, LogLevel logLevel = LogLevel.Warning)
#else
        internal static void Print(string text, LogLevel logLevel)
#endif
        {
            _self.Logger.Log(logLevel, text);
        }
#if KKS
        private void CharacterBeingImported(Dictionary<string, PluginData> importedExtendedData, Dictionary<int, int?> coordinateMapping)
        {
            ImportingCards = true;
            if (!importedExtendedData.TryGetValue(_extSaveKey, out var pluginData) || pluginData == null || !pluginData.data.TryGetValue("additionalAccessories", out var xmlData) || xmlData == null) return; //new version doesn't have anything but version number
            var data = new CharAdditionalData();
            var doc = new XmlDocument();
            doc.LoadXml((string)xmlData);
            var node = doc.FirstChild;

            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "accessorySet":
                        var coordinateType = XmlConvert.ToInt32(childNode.Attributes["type"].Value);
                        List<ChaFileAccessory.PartsInfo> parts;

                        if (data.rawAccessoriesInfos.TryGetValue(coordinateType, out parts) == false)
                        {
                            parts = new List<ChaFileAccessory.PartsInfo>();
                            data.rawAccessoriesInfos.Add(coordinateType, parts);
                        }

                        foreach (XmlNode accessoryNode in childNode.ChildNodes)
                        {
                            var part = new ChaFileAccessory.PartsInfo { type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value) };
                            if (part.type != 120)
                            {
                                part.id = XmlConvert.ToInt32(accessoryNode.Attributes["id"].Value);
                                part.parentKey = accessoryNode.Attributes["parentKey"].Value;

                                for (var i = 0; i < 2; i++)
                                {
                                    for (var j = 0; j < 3; j++)
                                    {
                                        part.addMove[i, j] = new Vector3
                                        {
                                            x = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}x"].Value),
                                            y = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}y"].Value),
                                            z = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}z"].Value)
                                        };
                                    }
                                }
                                for (var i = 0; i < 4; i++)
                                {
                                    part.color[i] = new Color
                                    {
                                        r = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}r"].Value),
                                        g = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}g"].Value),
                                        b = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}b"].Value),
                                        a = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}a"].Value)
                                    };
                                }
                                part.hideCategory = XmlConvert.ToInt32(accessoryNode.Attributes["hideCategory"].Value);
                                if (_hasDarkness)
                                    part.noShake = accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value);
                            }
                            parts.Add(part);
                        }
                        break;
                    default: break;
                }
            }

            var dict = data.rawAccessoriesInfos;
            var transferdict = new Dictionary<int, List<ChaFileAccessory.PartsInfo>>();
            foreach (var item in coordinateMapping)
            {
                if (!dict.TryGetValue(item.Key, out var list) || list == null || !item.Value.HasValue) continue;
                transferdict[item.Value.Value] = list;
            }
            data.rawAccessoriesInfos = transferdict;
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = new XmlTextWriter(stringWriter))
            {
                var maxCount = 0;
                xmlWriter.WriteStartElement("additionalAccessories");
                xmlWriter.WriteAttributeString("version", versionNum);
                foreach (var pair in data.rawAccessoriesInfos)
                {
                    if (pair.Value.Count == 0)
                        continue;
                    xmlWriter.WriteStartElement("accessorySet");
                    xmlWriter.WriteAttributeString("type", XmlConvert.ToString(pair.Key));
                    if (maxCount < pair.Value.Count)
                        maxCount = pair.Value.Count;

                    for (var index = 0; index < pair.Value.Count; index++)
                    {
                        var part = pair.Value[index];
                        xmlWriter.WriteStartElement("accessory");
                        xmlWriter.WriteAttributeString("type", XmlConvert.ToString(part.type));

                        if (part.type != 120)
                        {
                            xmlWriter.WriteAttributeString("id", XmlConvert.ToString(part.id));
                            xmlWriter.WriteAttributeString("parentKey", part.parentKey);

                            for (var i = 0; i < 2; i++)
                            {
                                for (var j = 0; j < 3; j++)
                                {
                                    var v = part.addMove[i, j];
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}x", XmlConvert.ToString(v.x));
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}y", XmlConvert.ToString(v.y));
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}z", XmlConvert.ToString(v.z));
                                }
                            }
                            for (var i = 0; i < 4; i++)
                            {
                                var c = part.color[i];
                                xmlWriter.WriteAttributeString($"color{i}r", XmlConvert.ToString(c.r));
                                xmlWriter.WriteAttributeString($"color{i}g", XmlConvert.ToString(c.g));
                                xmlWriter.WriteAttributeString($"color{i}b", XmlConvert.ToString(c.b));
                                xmlWriter.WriteAttributeString($"color{i}a", XmlConvert.ToString(c.a));
                            }
                            xmlWriter.WriteAttributeString("hideCategory", XmlConvert.ToString(part.hideCategory));
                            if (_hasDarkness)
                                xmlWriter.WriteAttributeString("noShake", XmlConvert.ToString(part.noShake));
                        }
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                }

#if KK || KKS
                if (InStudio)
                {
                    xmlWriter.WriteStartElement("visibility");
                    for (var i = 0; i < maxCount && i < data.showAccessories.Count; i++)
                    {
                        xmlWriter.WriteStartElement("visible");
                        xmlWriter.WriteAttributeString("value", XmlConvert.ToString(data.showAccessories[i]));
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }
#endif

                xmlWriter.WriteEndElement();

                pluginData.data["additionalAccessories"] = stringWriter.ToString();
            }
        }
#elif EC
        private void CharacterBeingImported(Dictionary<string, PluginData> importedExtendedData)
        {
            ImportingCards = true;
            if (!importedExtendedData.TryGetValue(_extSaveKey, out var pluginData) || pluginData == null || !pluginData.data.TryGetValue("additionalAccessories", out var xmlData)) return; //new version doesn't have anything but version number
            var data = new CharAdditionalData();
            var doc = new XmlDocument();
            doc.LoadXml((string)xmlData);
            var node = doc.FirstChild;

            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "accessorySet":
                        var coordinateType = XmlConvert.ToInt32(childNode.Attributes["type"].Value);
                        List<ChaFileAccessory.PartsInfo> parts;

                        if (data.rawAccessoriesInfos.TryGetValue(coordinateType, out parts) == false)
                        {
                            parts = new List<ChaFileAccessory.PartsInfo>();
                            data.rawAccessoriesInfos.Add(coordinateType, parts);
                        }

                        foreach (XmlNode accessoryNode in childNode.ChildNodes)
                        {
                            var part = new ChaFileAccessory.PartsInfo { type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value) };
                            if (part.type != 120)
                            {
                                part.id = XmlConvert.ToInt32(accessoryNode.Attributes["id"].Value);
                                part.parentKey = accessoryNode.Attributes["parentKey"].Value;

                                for (var i = 0; i < 2; i++)
                                {
                                    for (var j = 0; j < 3; j++)
                                    {
                                        part.addMove[i, j] = new Vector3
                                        {
                                            x = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}x"].Value),
                                            y = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}y"].Value),
                                            z = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}z"].Value)
                                        };
                                    }
                                }
                                for (var i = 0; i < 4; i++)
                                {
                                    part.color[i] = new Color
                                    {
                                        r = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}r"].Value),
                                        g = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}g"].Value),
                                        b = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}b"].Value),
                                        a = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}a"].Value)
                                    };
                                }
                                part.hideCategory = XmlConvert.ToInt32(accessoryNode.Attributes["hideCategory"].Value);
                                if (accessoryNode.Attributes["hideTiming"] != null)
                                {
                                    part.hideTiming = XmlConvert.ToInt32(accessoryNode.Attributes["hideTiming"].Value);
                                }
                                part.noShake = accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value);
                            }
                            parts.Add(part);
                        }
                        break;
                    default: break;
                }
            }

            using (var stringWriter = new StringWriter())
            using (var xmlWriter = new XmlTextWriter(stringWriter))
            {
                var maxCount = 0;
                xmlWriter.WriteStartElement("additionalAccessories");
                xmlWriter.WriteAttributeString("version", versionNum);
                foreach (var pair in data.rawAccessoriesInfos)
                {
                    if (pair.Value.Count == 0)
                        continue;
                    xmlWriter.WriteStartElement("accessorySet");
                    xmlWriter.WriteAttributeString("type", XmlConvert.ToString(pair.Key));
                    if (maxCount < pair.Value.Count)
                        maxCount = pair.Value.Count;

                    for (var index = 0; index < pair.Value.Count; index++)
                    {
                        var part = pair.Value[index];
                        xmlWriter.WriteStartElement("accessory");
                        xmlWriter.WriteAttributeString("type", XmlConvert.ToString(part.type));

                        if (part.type != 120)
                        {
                            xmlWriter.WriteAttributeString("id", XmlConvert.ToString(part.id));
                            xmlWriter.WriteAttributeString("parentKey", part.parentKey);

                            for (var i = 0; i < 2; i++)
                            {
                                for (var j = 0; j < 3; j++)
                                {
                                    var v = part.addMove[i, j];
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}x", XmlConvert.ToString(v.x));
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}y", XmlConvert.ToString(v.y));
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}z", XmlConvert.ToString(v.z));
                                }
                            }
                            for (var i = 0; i < 4; i++)
                            {
                                var c = part.color[i];
                                xmlWriter.WriteAttributeString($"color{i}r", XmlConvert.ToString(c.r));
                                xmlWriter.WriteAttributeString($"color{i}g", XmlConvert.ToString(c.g));
                                xmlWriter.WriteAttributeString($"color{i}b", XmlConvert.ToString(c.b));
                                xmlWriter.WriteAttributeString($"color{i}a", XmlConvert.ToString(c.a));
                            }
                            xmlWriter.WriteAttributeString("hideCategory", XmlConvert.ToString(part.hideCategory));
                            xmlWriter.WriteAttributeString("hideTiming", XmlConvert.ToString(part.hideTiming));
                            if (_hasDarkness)
                                xmlWriter.WriteAttributeString("noShake", XmlConvert.ToString(part.noShake));
                        }
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                }
                xmlWriter.WriteEndElement();

                pluginData.data["additionalAccessories"] = stringWriter.ToString();
            }
        }

        private void CoordinateBeingImported(Dictionary<string, PluginData> importedExtendedData)
        {
            ImportingCards = true;
        }

        /// <summary>
        /// previously unsupported by Joan
        /// Save charstate data which determines which accessories should be kept/shown/hidden per node.
        /// Unable to save directly to array due to how its being saved
        /// </summary>
        /// <param name="data"></param>
        private void ExtendedSave_HEditDataBeingSaved(HEdit.HEditData data)
        {
            var dict = new Dictionary<string, List<List<int[]>>>();
            foreach (var advpart in data.nodes.Where(x => ((HEdit.ADVPart)x.Value) != null))
            {
                var referencelist = dict[advpart.Key] = new List<List<int[]>>();
                foreach (var cut in ((HEdit.ADVPart)advpart.Value).cuts)
                {
                    var cutlist = new List<int[]>();
                    referencelist.Add(cutlist);
                    foreach (var charstate in cut.charStates)
                    {
                        cutlist.Add(charstate.accessory.Skip(20).ToArray());
                    }
                }
            }
            var plugindata = new PluginData();
            plugindata.data[_extSaveKey] = MessagePack.MessagePackSerializer.Serialize(dict);

            ExtendedSave.SetExtendedDataById(data, _extSaveKey, plugindata);
        }

        /// <summary>
        /// previously unsupported by Joan
        /// Load charstate data which determines which accessories should be kept/shown/hidden per node.
        /// </summary>
        /// <param name="data"></param>
        private void ExtendedSave_HEditDataBeingLoaded(HEdit.HEditData data)
        {
            var plugindata = ExtendedSave.GetExtendedDataById(data, _extSaveKey);

            if (plugindata == null)
            {
                //extremely unlikely to be used, but a build was provided for testing using this
                plugindata = ExtendedSave.GetExtendedDataById(data, GUID);
                if (plugindata == null) return;
            }

            var cutsdict = new Dictionary<string, List<List<int[]>>>();
            switch (plugindata.version)
            {
                case 0:
                    if (plugindata.data.TryGetValue(_extSaveKey, out var bytearry) && bytearry != null)
                    {
                        cutsdict = MessagePack.MessagePackSerializer.Deserialize<Dictionary<string, List<List<int[]>>>>((byte[])bytearry);
                    }
                    break;
                default:
                    Print("New MoreAccessories Version found please update", LogLevel.Message);
                    return;
            }

            foreach (var cutslist in cutsdict)
            {
                var advpart = (HEdit.ADVPart)data.nodes[cutslist.Key];
                var advpartcutslist = cutsdict[cutslist.Key];
                var cutscount = 0;
                foreach (var cut in advpart.cuts)
                {
                    var charStatescount = 0;
                    foreach (var charstate in cut.charStates)
                    {
                        charstate.accessory = charstate.accessory.Concat(advpartcutslist[cutscount][charStatescount]).ToArray();
                        charStatescount++;
                    }
                    cutscount++;
                }
            }
        }
#endif
        private void LevelLoaded(Scene scene, LoadSceneMode loadMode)
        {
            var instudio = false;

#if KK || KKS
            instudio = Application.productName.StartsWith("CharaStudio");
#endif
#if DEBUG
            Print($"loadmode {loadMode} index {scene.buildIndex} ");
#endif
#if KK
            if (scene.buildIndex == 6) return;
#elif EC
            if (scene.buildIndex == 7) return;
#endif
            switch (loadMode)
            {
                case LoadSceneMode.Single:
                    if (!instudio)
                    {
                        ImportingCards = false;
#if KK || KKS
                        HMode = null;
#elif EC
                        PlayMode = null;
#endif
                        switch (scene.buildIndex)
                        {
                            //Chara maker
#if KK
                            case 2:
#elif KKS || EC
                            case 3: //sunshine uses 3 for chara
#endif
                                MakerMode = new MakerMode();
                                break;

                            default:
                                break;
                        }
                    }
#if KK || KKS
                    else
                    {
#if KK
                        if (scene.buildIndex == 1) //Studio
#elif KKS
                        if (scene.buildIndex == 2) //Studio
#endif
                        {
                            StudioMode = new StudioClass();
                        }
                        else
                            StudioMode = null;
                    }
#endif
                    break;
#if !EC
                case LoadSceneMode.Additive:

#if KKS
                    if (!instudio && Game.initialized && scene.buildIndex == 3) //Class chara maker
#elif KK
                    if (!instudio && Game.IsInstance() && scene.buildIndex == 2) //Class chara maker
#endif
                    {
                        MakerMode = new MakerMode();
                    }
                    break;
#endif
            }
        }

#if KK || KKS
        internal void Update()
        {
            if (InStudio)
            {
                var treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
                if (treeNodeObject != null)
                {
                    if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out var info))
                    {
                        var selected = info as OCIChar;
                        if (selected != StudioMode._selectedStudioCharacter)
                        {
                            StudioMode._selectedStudioCharacter = selected;
                            StudioMode.UpdateStudioUI();
                        }
                    }
                }
            }
        }
#endif
        
        #endregion

        #region Private Methods
        internal void UpdateUI()
        {
            if (CharaMaker)
                MakerMode.UpdateMakerUI();
#if KK || KKS
            else if (InStudio)
                StudioMode.UpdateStudioUI();
#elif EC
            else if (PlayMode != null)
            {
                PlayMode.UpdatePlayUI();
            }
#endif
        }
        #endregion

        private static void UAR_ExtendedCardLoad_Prefix(ChaFile file)
        {
            _self.OnActualCharaLoad(file);
        }

        private static void UAR_ExtendedCoordLoad_Prefix(ChaFileCoordinate file)
        {
            _self.OnActualCoordLoad(file);
        }

        #region Saves
        private void OnActualCharaLoad(ChaFile file)
        {
            var pluginData = ExtendedSave.GetExtendedDataById(file, _extSaveKey);

            if (pluginData == null)
            {
                return;
            }

            //Trim if card was resaved with version 1
#if KK || KKS
            if (pluginData.version == 1)
            {
                foreach (var item in file.coordinate)
                {
                    if (item.accessory.parts.Length > 20)
                    {
                        item.accessory.parts = item.accessory.parts.Take(20).ToArray();
                    }
                }
            }
#else
            if (pluginData.version == 1 && file.coordinate.accessory.parts.Length > 20)
            {
                file.coordinate.accessory.parts = file.coordinate.accessory.parts.Take(20).ToArray();
            }
#endif
            if (pluginData.version == 2)
            {

#if KK || KKS
                if (InStudio && pluginData.data.TryGetValue("ShowAccessories", out var bytearray) && bytearray != null)
                {
                    Patches.Common_Patches.Seal(false);
                    file.status.showAccessory = file.status.showAccessory.Concat(MessagePack.MessagePackSerializer.Deserialize<bool[]>((byte[])bytearray)).ToArray();
                    Patches.Common_Patches.Seal(true);
                }
#endif

                if (
#if KK || KKS
                    InH ||
#endif
                    CharaMaker
                    )
                    this.ExecuteDelayed(UpdateUI);
                else
                    UpdateUI();

                return;
            }

            var additionaldata = new CharAdditionalData(pluginData);

            //Print($"Loading Data for {file.parameter.fullname} current size {file.coordinate.accessory.parts.Length}");

            //Print($"Plugin Data has {PreviousMigratedData.rawAccessoriesInfos.Count} and version {pluginData.version}", LogLevel.Error);
#if KK || KKS
            foreach (var item in additionaldata.rawAccessoriesInfos)
            {
                //Print($"raw data has key {item.Key}");
                if (!(item.Key < file.coordinate.Length)) continue;
                var accessory = file.coordinate[item.Key].accessory;
                accessory.parts = accessory.parts.Concat(item.Value).ToArray();
                //Print($"Settings coordinate {item.Key}");
            }
#else
            if (additionaldata.rawAccessoriesInfos.TryGetValue(0, out var partsInfos))
            {
                var accessory = file.coordinate.accessory;
                accessory.parts = accessory.parts.Concat(partsInfos).ToArray();
            }
#endif
#if KK || KKS
            if (InStudio)
            {
                Patches.Common_Patches.Seal(false);
                file.status.showAccessory = file.status.showAccessory.Concat(additionaldata.showAccessories).ToArray();
                Patches.Common_Patches.Seal(true);
            }
#endif
            //Print($"finished Loading Data for {file.parameter.fullname} current size {file.coordinate.accessory.parts.Length} {System.Environment.StackTrace}");

            if (
#if KK || KKS
                    InH ||
#endif
                    CharaMaker
            )
                this.ExecuteDelayed(UpdateUI);
            else
                UpdateUI();
        }

        private void OnActualCharaSave(ChaFile file)
        {
            if (ImportingCards)
            {
                if (CharaMaker) ImportingCards = false;
#if KK || KKS
                if (InStudio) ImportingCards = false;
#endif
                OnActualCharaLoad(file);
            }
            var pluginData = new PluginData { version = _saveVersion };

            if (BackwardCompatibility)
            {
                var data = new CharAdditionalData(file);
                using (var stringWriter = new StringWriter())
                using (var xmlWriter = new XmlTextWriter(stringWriter))
                {
                    var maxCount = 0;
                    xmlWriter.WriteStartElement("additionalAccessories");
                    xmlWriter.WriteAttributeString("version", versionNum);
                    foreach (var pair in data.rawAccessoriesInfos)
                    {
                        if (pair.Value.Count == 0)
                            continue;
                        xmlWriter.WriteStartElement("accessorySet");
                        xmlWriter.WriteAttributeString("type", XmlConvert.ToString(pair.Key));
                        if (maxCount < pair.Value.Count)
                            maxCount = pair.Value.Count;

                        for (var index = 0; index < pair.Value.Count; index++)
                        {
                            var part = pair.Value[index];
                            xmlWriter.WriteStartElement("accessory");
                            xmlWriter.WriteAttributeString("type", XmlConvert.ToString(part.type));

                            if (part.type != 120)
                            {
                                xmlWriter.WriteAttributeString("id", XmlConvert.ToString(part.id));
                                xmlWriter.WriteAttributeString("parentKey", part.parentKey);

                                for (var i = 0; i < 2; i++)
                                {
                                    for (var j = 0; j < 3; j++)
                                    {
                                        var v = part.addMove[i, j];
                                        xmlWriter.WriteAttributeString($"addMove{i}{j}x", XmlConvert.ToString(v.x));
                                        xmlWriter.WriteAttributeString($"addMove{i}{j}y", XmlConvert.ToString(v.y));
                                        xmlWriter.WriteAttributeString($"addMove{i}{j}z", XmlConvert.ToString(v.z));
                                    }
                                }
                                for (var i = 0; i < 4; i++)
                                {
                                    var c = part.color[i];
                                    xmlWriter.WriteAttributeString($"color{i}r", XmlConvert.ToString(c.r));
                                    xmlWriter.WriteAttributeString($"color{i}g", XmlConvert.ToString(c.g));
                                    xmlWriter.WriteAttributeString($"color{i}b", XmlConvert.ToString(c.b));
                                    xmlWriter.WriteAttributeString($"color{i}a", XmlConvert.ToString(c.a));
                                }
                                xmlWriter.WriteAttributeString("hideCategory", XmlConvert.ToString(part.hideCategory));
#if EC
                                xmlWriter.WriteAttributeString("hideTiming", XmlConvert.ToString(part.hideTiming));
#endif
                                if (_hasDarkness)
                                    xmlWriter.WriteAttributeString("noShake", XmlConvert.ToString((bool)part.GetPrivateProperty("noShake")));
                            }
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteEndElement();
                    }

#if KK || KKS
                    if (InStudio)
                    {
                        xmlWriter.WriteStartElement("visibility");
                        for (var i = 0; i < maxCount && i < data.showAccessories.Count; i++)
                        {
                            xmlWriter.WriteStartElement("visible");
                            xmlWriter.WriteAttributeString("value", XmlConvert.ToString(data.showAccessories[i]));
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteEndElement();
                    }
#endif

                    xmlWriter.WriteEndElement();

                    pluginData.data.Add("additionalAccessories", stringWriter.ToString());
                }
            }

#if KK || KKS
            if (InStudio)
            {
                pluginData.data.Add("ShowAccessories", MessagePack.MessagePackSerializer.Serialize(file.status.showAccessory.Skip(20).ToArray()));
            }
#endif
            ExtendedSave.SetExtendedDataById(file, _extSaveKey, pluginData);
        }

        internal void OnActualCoordLoad(ChaFileCoordinate file)
        {
            var pluginData = ExtendedSave.GetExtendedDataById(file, _extSaveKey);

            if (pluginData == null || pluginData.version == 2) //escape data is already saved directly on card 
            {
                return;
            }

            if (pluginData.version == 1 && file.accessory.parts.Length > 20)//check if it was saved with old moreaccessories assume the worst and trim
            {
                file.accessory.parts = file.accessory.parts.Take(20).ToArray();
            }

            var additionaldata = new CharAdditionalData();
            XmlNode node = null;
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out var xmlData) && xmlData != null)
            {
                var doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }
            if (node != null)
            {
                foreach (XmlNode accessoryNode in node.ChildNodes)
                {
                    var part = new ChaFileAccessory.PartsInfo
                    {
                        type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value)
                    };
                    if (part.type != 120)
                    {
                        part.id = XmlConvert.ToInt32(accessoryNode.Attributes["id"].Value);
                        part.parentKey = accessoryNode.Attributes["parentKey"].Value;

                        for (var i = 0; i < 2; i++)
                        {
                            for (var j = 0; j < 3; j++)
                            {
                                part.addMove[i, j] = new Vector3
                                {
                                    x = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}x"].Value),
                                    y = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}y"].Value),
                                    z = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}z"].Value)
                                };
                            }
                        }
                        for (var i = 0; i < 4; i++)
                        {
                            part.color[i] = new Color
                            {
                                r = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}r"].Value),
                                g = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}g"].Value),
                                b = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}b"].Value),
                                a = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}a"].Value)
                            };
                        }
                        part.hideCategory = XmlConvert.ToInt32(accessoryNode.Attributes["hideCategory"].Value);
#if EC
                        if (accessoryNode.Attributes["hideTiming"] != null)
                            part.hideTiming = XmlConvert.ToInt32(accessoryNode.Attributes["hideTiming"].Value);
#endif
                        if (_hasDarkness)
                            part.SetPrivateProperty("noShake", accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value));
                    }
                    additionaldata.nowAccessories.Add(part);
                }
            }

            file.accessory.parts = file.accessory.parts.Concat(additionaldata.nowAccessories).ToArray();

            if (
#if KK || KKS
                    InH ||
#endif
                    CharaMaker
            )
                this.ExecuteDelayed(UpdateUI);
            else
                UpdateUI();
        }

        private void OnActualCoordSave(ChaFileCoordinate file)
        {
            if (ImportingCards)
            {
                if (CharaMaker) ImportingCards = false;
#if KK || KKS
                if (InStudio) ImportingCards = false;
#endif
                OnActualCoordLoad(file);
            }

            if (BackwardCompatibility)
            {
                var data = new CharAdditionalData(ChaCustom.CustomBase.instance.chaCtrl);

                using (var stringWriter = new StringWriter())
                using (var xmlWriter = new XmlTextWriter(stringWriter))
                {
                    xmlWriter.WriteStartElement("additionalAccessories");
                    xmlWriter.WriteAttributeString("version", versionNum);
                    foreach (var part in data.nowAccessories)
                    {
                        xmlWriter.WriteStartElement("accessory");
                        xmlWriter.WriteAttributeString("type", XmlConvert.ToString(part.type));
                        if (part.type != 120)
                        {
                            xmlWriter.WriteAttributeString("id", XmlConvert.ToString(part.id));
                            xmlWriter.WriteAttributeString("parentKey", part.parentKey);
                            for (var i = 0; i < 2; i++)
                            {
                                for (var j = 0; j < 3; j++)
                                {
                                    var v = part.addMove[i, j];
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}x", XmlConvert.ToString(v.x));
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}y", XmlConvert.ToString(v.y));
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}z", XmlConvert.ToString(v.z));
                                }
                            }
                            for (var i = 0; i < 4; i++)
                            {
                                var c = part.color[i];
                                xmlWriter.WriteAttributeString($"color{i}r", XmlConvert.ToString(c.r));
                                xmlWriter.WriteAttributeString($"color{i}g", XmlConvert.ToString(c.g));
                                xmlWriter.WriteAttributeString($"color{i}b", XmlConvert.ToString(c.b));
                                xmlWriter.WriteAttributeString($"color{i}a", XmlConvert.ToString(c.a));
                            }
                            xmlWriter.WriteAttributeString("hideCategory", XmlConvert.ToString(part.hideCategory));
#if EC
                            xmlWriter.WriteAttributeString("hideTiming", XmlConvert.ToString(part.hideTiming));
#endif
                            if (_hasDarkness)
                                xmlWriter.WriteAttributeString("noShake", XmlConvert.ToString((bool)part.GetPrivateProperty("noShake")));
                        }
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    var backwardspluginData = new PluginData { version = _saveVersion };
                    backwardspluginData.data.Add("additionalAccessories", stringWriter.ToString());
                    ExtendedSave.SetExtendedDataById(file, _extSaveKey, backwardspluginData);
                }
                return;
            }
            var pluginData = new PluginData
            {
                version = _saveVersion
            };
            ExtendedSave.SetExtendedDataById(file, _extSaveKey, pluginData);
        }
        #endregion
    }
}
