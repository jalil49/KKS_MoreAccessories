#pragma warning disable CS0618 // Type or member is obsolete
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using ExtensibleSaveFormat;
using HarmonyLib;
using MoreAccessoriesKOI.Extensions;
using Sideloader.AutoResolver;
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
    [BepInPlugin(GUID: Guid, Name: "MoreAccessories", Version: VersionNum)]
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

            HasDarkness = typeof(ChaControl).GetMethod("ChangeShakeAccessory", AccessTools.all) != null;
            var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            var uarHooks = typeof(UniversalAutoResolver).GetNestedType("Hooks", AccessTools.all);
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
            // ExtendedSave.HEditDataBeingSaved += ExtendedSave_HEditDataBeingSaved;
            // ExtendedSave.HEditDataBeingLoaded += ExtendedSave_HEditDataBeingLoaded;

            SceneCreateAccessoryNames = Config.Bind("Scene Creation", "Use Accessory Name", true);
#endif
        }

        private static void SceneManager_sceneUnloaded(Scene arg0)
        {
            if (arg0.name == "CustomScene") MakerMode = null;
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
            if (!importedExtendedData.TryGetValue(ExtSaveKey, out var pluginData) || pluginData == null || !pluginData.data.TryGetValue("additionalAccessories", out var xmlData) || xmlData == null)
                return; //new version doesn't have anything but version number
            var data = new CharAdditionalData();
            var doc = new XmlDocument();
            doc.LoadXml((string)xmlData);
            var node = doc.FirstChild;

            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name == "accessorySet")
                {
                    if (childNode.Attributes != null)
                    {
                        var coordinateType = XmlConvert.ToInt32(childNode.Attributes["type"].Value);

                        if (data.rawAccessoriesInfos.TryGetValue(coordinateType, out var parts) == false)
                        {
                            parts = new List<ChaFileAccessory.PartsInfo>();
                            data.rawAccessoriesInfos.Add(coordinateType, parts);
                        }

                        foreach (XmlNode accessoryNode in childNode.ChildNodes)
                        {
                            if (accessoryNode.Attributes != null)
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
                                    if (HasDarkness)
                                        part.noShake = accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value);
                                }

                                parts.Add(part);
                            }
                        }
                    }
                }
            }

            var dict = data.rawAccessoriesInfos;
            var transferDict = new Dictionary<int, List<ChaFileAccessory.PartsInfo>>();
            foreach (var item in coordinateMapping)
            {
                if (!dict.TryGetValue(item.Key, out var list) || list == null || !item.Value.HasValue) continue;
                transferDict[item.Value.Value] = list;
            }

            data.rawAccessoriesInfos = transferDict;
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = new XmlTextWriter(stringWriter))
            {
                var maxCount = 0;
                xmlWriter.WriteStartElement("additionalAccessories");
                xmlWriter.WriteAttributeString("version", VersionNum);
                foreach (var pair in data.rawAccessoriesInfos)
                {
                    if (pair.Value.Count == 0)
                        continue;
                    xmlWriter.WriteStartElement("accessorySet");
                    xmlWriter.WriteAttributeString("type", XmlConvert.ToString(pair.Key));
                    if (maxCount < pair.Value.Count)
                        maxCount = pair.Value.Count;

                    foreach (var part in pair.Value)
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
                            if (HasDarkness)
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
            if (!importedExtendedData.TryGetValue(ExtSaveKey, out var pluginData) || pluginData == null || !pluginData.data.TryGetValue("additionalAccessories", out var xmlData)) return; //new version doesn't have anything but version number
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

                        if (data.rawAccessoriesInfos.TryGetValue(coordinateType, out var parts) == false)
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
                }
            }

            using (var stringWriter = new StringWriter())
            using (var xmlWriter = new XmlTextWriter(stringWriter))
            {
                var maxCount = 0;
                xmlWriter.WriteStartElement("additionalAccessories");
                xmlWriter.WriteAttributeString("version", VersionNum);
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
                            if (HasDarkness)
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

        // /// <summary>
        // /// previously unsupported by Joan
        // /// Save charstate data which determines which accessories should be kept/shown/hidden per node.
        // /// Unable to save directly to array due to how its being saved
        // /// </summary>
        // /// <param name="data"></param>
        // private void ExtendedSave_HEditDataBeingSaved(HEditData data)
        // {
        //     var dict = new Dictionary<string, List<List<int[]>>>();
        //     foreach (var advPart in data.nodes.Where(x => x.Value is HEdit.ADVPart))
        //     {
        //         var referenceList = dict[advPart.Key] = new List<List<int[]>>();
        //         foreach (var cut in ((HEdit.ADVPart)advPart.Value).cuts)
        //         {
        //             var cutList = new List<int[]>();
        //             referenceList.Add(cutList);
        //             foreach (var charState in cut.charStates)
        //             {
        //                 cutList.Add(charState.accessory.Skip(20).ToArray());
        //             }
        //         }
        //     }
        //     var pluginData = new PluginData
        //     {
        //         data =
        //         {
        //             [ExtSaveKey] = MessagePackSerializer.Serialize(dict)
        //         },
        //         version = 1
        //     };
        //
        //     ExtendedSave.SetExtendedDataById(data, ExtSaveKey, pluginData);
        // }
        //
        // /// <summary>
        // /// previously unsupported by Joan
        // /// Load charstate data which determines which accessories should be kept/shown/hidden per node.
        // /// </summary>
        // /// <param name="data"></param>
        // private void ExtendedSave_HEditDataBeingLoaded(HEditData data)
        // {
        //     var pluginData = ExtendedSave.GetExtendedDataById(data, ExtSaveKey);
        //
        //     if (pluginData == null)
        //     {
        //         //extremely unlikely to be used, but a build was provided for testing using this
        //         pluginData = ExtendedSave.GetExtendedDataById(data, Guid);
        //         if (pluginData == null) return;
        //     }
        //
        //     var cutsDict = new Dictionary<string, List<List<int[]>>>();
        //     switch (pluginData.version)
        //     {
        //         case 0:
        //             if (pluginData.data.TryGetValue(ExtSaveKey, out var byteArray) && byteArray != null)
        //             {
        //                 cutsDict = MessagePackSerializer.Deserialize<Dictionary<string, List<List<int[]>>>>((byte[])byteArray);
        //             }
        //             break;
        //         default:
        //             Print("New MoreAccessories Version found please update", LogLevel.Message);
        //             return;
        //     }
        //
        //     foreach (var cutsList in cutsDict)
        //     {
        //         var advPart = (HEdit.ADVPart)data.nodes[cutsList.Key];
        //         var advPartCutsList = cutsDict[cutsList.Key];
        //         var cutsCount = 0;
        //         foreach (var cut in advPart.cuts)
        //         {
        //             var charStatesCount = 0;
        //             foreach (var charState in cut.charStates)
        //             {
        //                 charState.accessory = charState.accessory.Concat(advPartCutsList[cutsCount][charStatesCount]).ToArray();
        //                 charStatesCount++;
        //             }
        //             cutsCount++;
        //         }
        //     }
        // }
#endif
        private void LevelLoaded(Scene scene, LoadSceneMode loadMode)
        {
#if KK || KKS
            var inStudio = Application.productName.StartsWith("CharaStudio");
#else
            var inStudio = Application.productName.StartsWith("CharaStudio");
#endif
#if DEBUG
            Print($"LoadSceneMode {loadMode} index {scene.buildIndex} ");
#endif
#if KK
            if (scene.buildIndex == 6) return;
#elif EC
            if (scene.buildIndex == 7) return;
#endif
            switch (loadMode)
            {
                case LoadSceneMode.Single:
                    if (!inStudio)
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
                        StudioMode = scene.buildIndex == 1 ? new StudioClass() : null; //Studio
#elif KKS
                        StudioMode = scene.buildIndex == 2 ? new StudioClass() : null; //Studio
#endif
                    }
#endif
                    break;
#if !EC
                case LoadSceneMode.Additive:

#if KKS
                    if (!inStudio && Game.initialized && scene.buildIndex == 3) //Class chara maker
                    {
                        MakerMode = new MakerMode();
                    }
#elif KK
                    if (!inStudio && Game.IsInstance() && scene.buildIndex == 2) //Class chara maker
                    {
                        MakerMode = new MakerMode();
                    }
#endif

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
                if (treeNodeObject)
                {
                    if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out var info))
                    {
                        var selected = info as OCIChar;
                        if (selected != StudioMode.SelectedStudioCharacter)
                        {
                            StudioMode.SelectedStudioCharacter = selected;
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
            var pluginData = ExtendedSave.GetExtendedDataById(file, ExtSaveKey);

            if (pluginData == null)
            {
                return;
            }

            //Trim if card was re-saved with version 1
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

#if KK || KKS
                if (InH || CharaMaker)
                    this.ExecuteDelayed(UpdateUI);
                else
                    UpdateUI();
#else
                if (CharaMaker)
                    this.ExecuteDelayed(UpdateUI);
                else
                    UpdateUI();
#endif
                return;
            }

            var additionalData = new CharAdditionalData(pluginData);

            //Print($"Loading Data for {file.parameter.fullname} current size {file.coordinate.accessory.parts.Length}");

            //Print($"Plugin Data has {PreviousMigratedData.rawAccessoriesInfos.Count} and version {pluginData.version}", LogLevel.Error);
#if KK || KKS
            foreach (var item in additionalData.rawAccessoriesInfos)
            {
                //Print($"raw data has key {item.Key}");
                if (!(item.Key < file.coordinate.Length)) continue;
                var accessory = file.coordinate[item.Key].accessory;
                accessory.parts = accessory.parts.Concat(item.Value).ToArray();
                //Print($"Settings coordinate {item.Key}");
            }
#else
            if (additionalData.rawAccessoriesInfos.TryGetValue(0, out var partsInfos))
            {
                var accessory = file.coordinate.accessory;
                accessory.parts = accessory.parts.Concat(partsInfos).ToArray();
            }
#endif
#if KK || KKS
            if (InStudio)
            {
                Patches.Common_Patches.Seal(false);
                file.status.showAccessory = file.status.showAccessory.Concat(additionalData.showAccessories).ToArray();
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

            var pluginData = new PluginData { version = SaveVersion };

            if (_backwardCompatibility)
            {
                var data = new CharAdditionalData(file);
                using (var stringWriter = new StringWriter())
                using (var xmlWriter = new XmlTextWriter(stringWriter))
                {
                    var maxCount = 0;
                    xmlWriter.WriteStartElement("additionalAccessories");
                    xmlWriter.WriteAttributeString("version", VersionNum);
                    foreach (var pair in data.rawAccessoriesInfos)
                    {
                        if (pair.Value.Count == 0)
                            continue;
                        xmlWriter.WriteStartElement("accessorySet");
                        xmlWriter.WriteAttributeString("type", XmlConvert.ToString(pair.Key));
                        if (maxCount < pair.Value.Count)
                            maxCount = pair.Value.Count;

                        foreach (var part in pair.Value)
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
                                if (HasDarkness)
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
            ExtendedSave.SetExtendedDataById(file, ExtSaveKey, pluginData);
        }

        internal void OnActualCoordLoad(ChaFileCoordinate file)
        {
            var pluginData = ExtendedSave.GetExtendedDataById(file, ExtSaveKey);

            if (pluginData == null || pluginData.version == 2) //escape data is already saved directly on card 
            {
                return;
            }

            if (pluginData.version == 1 && file.accessory.parts.Length > 20) //check if it was saved with old more accessories assume the worst and trim
            {
                file.accessory.parts = file.accessory.parts.Take(20).ToArray();
            }

            var additionalData = new CharAdditionalData();
            XmlNode node = null;
            if (pluginData.data.TryGetValue("additionalAccessories", out var xmlData) && xmlData != null)
            {
                var doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }

            if (node != null)
            {
                foreach (XmlNode accessoryNode in node.ChildNodes)
                {
                    if (accessoryNode.Attributes != null)
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
                            if (HasDarkness)
                                part.SetPrivateProperty("noShake", accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value));
                        }

                        additionalData.nowAccessories.Add(part);
                    }
                }
            }

            file.accessory.parts = file.accessory.parts.Concat(additionalData.nowAccessories).ToArray();

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

            if (_backwardCompatibility)
            {
                var data = new CharAdditionalData(CustomBase.instance.chaCtrl);

                using (var stringWriter = new StringWriter())
                using (var xmlWriter = new XmlTextWriter(stringWriter))
                {
                    xmlWriter.WriteStartElement("additionalAccessories");
                    xmlWriter.WriteAttributeString("version", VersionNum);
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
                            if (HasDarkness)
                                xmlWriter.WriteAttributeString("noShake", XmlConvert.ToString((bool)part.GetPrivateProperty("noShake")));
                        }

                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();

                    var backwardsPluginData = new PluginData { version = SaveVersion };
                    backwardsPluginData.data.Add("additionalAccessories", stringWriter.ToString());
                    ExtendedSave.SetExtendedDataById(file, ExtSaveKey, backwardsPluginData);
                }

                return;
            }

            var pluginData = new PluginData
            {
                version = SaveVersion
            };
            ExtendedSave.SetExtendedDataById(file, ExtSaveKey, pluginData);
        }

        #endregion
    }
}