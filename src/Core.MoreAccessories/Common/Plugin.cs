﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using BepInEx;
using ChaCustom;
using ExtensibleSaveFormat;
using HarmonyLib;
#if EMOTIONCREATORS
using HPlay;
using ADVPart.Manipulate;
using ADVPart.Manipulate.Chara;
#endif
using Manager;
using Sideloader.AutoResolver;
#if KOIKATSU
using Studio;
#endif
using MoreAccessoriesKOI.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

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
            ExtendedSave.CardBeingImported += ExtendedSave_CardBeingImported;
            SceneManager.sceneLoaded += LevelLoaded;

            _hasDarkness = true;
            _isParty = Application.productName == "Koikatsu Party";

            var harmony = new Harmony(GUID);
            harmony.PatchAll();
            var uarHooks = typeof(UniversalAutoResolver).GetNestedType("Hooks", BindingFlags.NonPublic | BindingFlags.Static);
            ChaControl_Patches.ChaControl_ChangeAccessory_Patches.ManualPatch(harmony);
            harmony.Patch(uarHooks.GetMethod("ExtendedCardLoad", AccessTools.all), new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCardLoad_Prefix)));
            harmony.Patch(uarHooks.GetMethod("ExtendedCardSave", AccessTools.all), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCardSave_Postfix)));
            harmony.Patch(uarHooks.GetMethod("ExtendedCoordinateLoad", AccessTools.all), new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCoordLoad_Prefix)));
            harmony.Patch(uarHooks.GetMethod("ExtendedCoordinateSave", AccessTools.all), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCoordSave_Postfix)));
        }

        private void ExtendedSave_CardBeingImported(Dictionary<string, PluginData> importedExtendedData)
        {
            if (!importedExtendedData.TryGetValue(_extSaveKey, out var pluginData) || pluginData == null || !pluginData.data.TryGetValue("additionalAccessories", out var xmlData)) return;

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
#if EMOTIONCREATORS
                                    if (accessoryNode.Attributes["hideTiming"] != null)
                                        part.hideTiming = XmlConvert.ToInt32(accessoryNode.Attributes["hideTiming"].Value);
#endif
                                if (_hasDarkness)
                                    part.noShake = accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value);
                            }
                            parts.Add(part);
                        }
                        break;
#if KOIKATSU
                    case "visibility":
                        if (_inStudio)
                        {
                            data.showAccessories = new List<bool>();
                            foreach (XmlNode grandChildNode in childNode.ChildNodes)
                                data.showAccessories.Add(grandChildNode.Attributes?["value"] == null || XmlConvert.ToBoolean(grandChildNode.Attributes["value"].Value));
                        }
                        break;
#endif
                }
            }

            var dict = data.rawAccessoriesInfos;
            var keylist = data.rawAccessoriesInfos.Keys.ToList();
            keylist.Remove(0);
            foreach (var item in keylist)
            {
                dict.Remove(item);
            }
#if false
            //moreoutfits complete transfer
            var size = keylist.Count;
            keylist.Remove(5);
            keylist.Remove(3);
            var transferarray = new List<int[]> { new[] { 0, 5 }, new[] { 1, 3 } };
            var transferdict = new Dictionary<int, List<ChaFileAccessory.PartsInfo>>();
            foreach (var array in transferarray)
            {
                if (dict.TryGetValue(array[1], out var list))
                {
                    transferdict[array[0]] = list;
                }
            }

            for (int i = 2; i < 4; i++)
            {
                if (transferdict.TryGetValue(0, out var list))
                {
                    transferdict[i] = list.ToList();
                }
            }

            int key = 4;
            foreach (var item in keylist)
            {
                if (dict.TryGetValue(item, out var list))
                {
                    transferdict[key] = list;
                    key++;
                    continue;
                }
                transferdict[key] = new List<ChaFileAccessory.PartsInfo>();
                key++;
            }
            data.rawAccessoriesInfos.Clear();
            foreach (var item in transferdict)
            {
                data.rawAccessoriesInfos[item.Key] = item.Value;
            }
#endif
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
#if EMOTIONCREATORS
                            xmlWriter.WriteAttributeString("hideTiming", XmlConvert.ToString(part.hideTiming));
#endif
                            if (_hasDarkness)
                                xmlWriter.WriteAttributeString("noShake", XmlConvert.ToString(part.noShake));
                        }
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                }

#if KOIKATSU
                if (_inStudio)
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

                pluginData.version = _saveVersion;
                pluginData.data["additionalAccessories"] = stringWriter.ToString();
            }
        }

        private void LevelLoaded(Scene scene, LoadSceneMode loadMode)
        {
            var instudio = Application.productName.StartsWith("KoikatsuSunshineStudio");
            switch (loadMode)
            {
                case LoadSceneMode.Single:
                    if (!instudio)
                    {
                        _inCharaMaker = false;
#if KOIKATSU
                        _inH = false;
#elif EMOTIONCREATORS
                        _inPlay = false;
#endif
                        switch (scene.buildIndex)
                        {
                            //Chara maker
                            case 3: //sunshine uses 3 for chara
                                CustomBase.Instance.selectSlot = 0;
                                _additionalCharaMakerSlots = new List<CharaMakerSlotData>();
                                _raycastCtrls = new List<UI_RaycastCtrl>();
                                _inCharaMaker = true;
                                _loadCoordinatesWindow = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow").GetComponent<CustomFileWindow>();
                                break;

#if KOIKATSU
                            case 17: //Hscenes
                                _inH = true;
                                break;

                            case 1: //converted
                            case 4: //menu
                            default:
                                break;
#endif
                        }
                    }
#if KOIKATSU
                    else
                    {
                        if (scene.buildIndex == 1) //Studio
                        {
                            SpawnStudioUI();
                            _inStudio = true;
                        }
                        else
                            _inStudio = false;
                    }
#endif
                    _accessoriesByChar.Purge();
                    _charByCoordinate.Purge();
                    break;
                case LoadSceneMode.Additive:
                    if (Game.initialized && scene.buildIndex == 2) //Class chara maker
                    {
                        CustomBase.Instance.selectSlot = 0;
                        _additionalCharaMakerSlots = new List<CharaMakerSlotData>();
                        _raycastCtrls = new List<UI_RaycastCtrl>();
                        _inCharaMaker = true;
                        _loadCoordinatesWindow = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow").GetComponent<CustomFileWindow>();
                    }
                    break;
            }
        }

        private void Update()
        {
            if (_inCharaMaker)
            {
                if (_customAcsChangeSlot != null)
                {
                    if (CustomBase.Instance.updateCustomUI)
                    {
                        for (var i = 0; i < _additionalCharaMakerSlots.Count && i < _charaMakerData.nowAccessories.Count; i++)
                        {
                            var slot = _additionalCharaMakerSlots[i];
                            if (slot.toggle.gameObject.activeSelf == false)
                                continue;
                            if (i + 20 == CustomBase.Instance.selectSlot)
                                slot.cvsAccessory.UpdateCustomUI();
                            slot.cvsAccessory.UpdateSlotName();
                        }
                    }
                }
                if (_loadCoordinatesWindow == null) //Handling maker with additive loading
                    _inCharaMaker = false;
            }
#if KOIKATSU
            if (_inStudio)
            {
                var treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
                if (treeNodeObject != null)
                {
                    ObjectCtrlInfo info;
                    if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                    {
                        var selected = info as OCIChar;
                        if (selected != _selectedStudioCharacter)
                        {
                            _selectedStudioCharacter = selected;
                            UpdateStudioUI();
                        }
                    }
                }
            }
#endif
        }

        private void LateUpdate()
        {
            if (_inCharaMaker && _customAcsChangeSlot != null)
            {
                Transform t;
                if (CustomBase.Instance.selectSlot < 20)
                    t = _customAcsChangeSlot.items[CustomBase.Instance.selectSlot].cgItem.transform;
                else
                    t = _additionalCharaMakerSlots[CustomBase.Instance.selectSlot - 20].canvasGroup.transform;
                t.position = new Vector3(t.position.x, _slotUIPositionY);
            }
        }
        #endregion

        #region Public Methods (aka the stuff other plugins use)
        /// <summary>
        /// Returns the ChaAccessoryComponent of <paramref name="character"/> at <paramref name="index"/>.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public ChaAccessoryComponent GetChaAccessoryComponent(ChaControl character, int index)
        {
            if (index < 20)
                return character.cusAcsCmp[index];
            CharAdditionalData data;
            index -= 20;
            if (_accessoriesByChar.TryGetValue(character.chaFile, out data) && index < data.cusAcsCmp.Count)
                return data.cusAcsCmp[index];
            return null;
        }

        /// <summary>
        /// Returns the index of a certain ChaAccessoryComponent held by <paramref name="character"/>.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public int GetChaAccessoryComponentIndex(ChaControl character, ChaAccessoryComponent component)
        {
            var index = character.cusAcsCmp.IndexOf(component);
            if (index == -1)
            {
                CharAdditionalData data;
                if (_accessoriesByChar.TryGetValue(character.chaFile, out data) == false)
                    return -1;
                index = data.cusAcsCmp.IndexOf(component);
                if (index == -1)
                    return -1;
                index += 20;
            }
            return index;
        }
        #endregion

        #region Private Methods
#if EMOTIONCREATORS
        //CharaUICtrl
        internal void SpawnPlayUI(HPlayHPartAccessoryCategoryUI ui)
        {
            _playUI = ui;
            _inPlay = true;
            _additionalPlaySceneSlots.Clear();
            HPlayHPartUI.SelectUITextMesh[] buttons = (HPlayHPartUI.SelectUITextMesh[])ui.accessoryCategoryUIs");
            _playButtonTemplate = (RectTransform)buttons[0].btn.transform;
            _playButtonTemplate.GetComponentInChildren<TextMeshProUGUI>().fontMaterial = new Material(_playButtonTemplate.GetComponentInChildren<TextMeshProUGUI>().fontMaterial);
            int index = _playButtonTemplate.parent.GetSiblingIndex();

            ScrollRect scrollView = UIUtility.CreateScrollView("ScrollView", _playButtonTemplate.parent.parent);
            scrollView.transform.SetSiblingIndex(index);
            scrollView.transform.SetRect(_playButtonTemplate.parent);
            ((RectTransform)scrollView.transform).offsetMax = new Vector2(_playButtonTemplate.offsetMin.x + 192f, -88f);
            ((RectTransform)scrollView.transform).offsetMin = new Vector2(_playButtonTemplate.offsetMin.x, -640f - 88f);
            scrollView.viewport.GetComponent<Image>().sprite = null;
            scrollView.movementType = ScrollRect.MovementType.Clamped;
            scrollView.horizontal = false;
            scrollView.scrollSensitivity = 18f;
            if (scrollView.horizontalScrollbar != null)
                Destroy(scrollView.horizontalScrollbar.gameObject);
            if (scrollView.verticalScrollbar != null)
                Destroy(scrollView.verticalScrollbar.gameObject);
            Destroy(scrollView.GetComponent<Image>());
            Destroy(scrollView.content.gameObject);
            scrollView.content = (RectTransform)_playButtonTemplate.parent;
            _playButtonTemplate.parent.SetParent(scrollView.viewport, false);
            _playButtonTemplate.parent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ((RectTransform)_playButtonTemplate.parent).anchoredPosition = Vector2.zero;
            _playButtonTemplate.parent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
            //foreach (HPlayHPartUI.SelectUITextMesh b in buttons)
            //    ((RectTransform)b.btn.transform).anchoredPosition = Vector2.zero;
        }

        internal void SpawnADVUI(AccessoryUICtrl ui)
        {
            _advUI = ui;
            _advToggleTemplate = (RectTransform)((Toggle)((Array)((Array)ui.toggles).GetValue(19).toggles).GetValue(0)).transform.parent.parent;

            Button[] buttons = (Button[])_advUI.buttonALL").buttons");
            for (int i = 0; i < buttons.Length; i++)
            {
                Button b = buttons[i];
                int i1 = i;
                b.onClick.AddListener(() =>
                {
                    CharAdditionalData ad = _accessoriesByChar[_advUI.chaControl.chaFile];
                    for (int j = 0; j < ad.advState.Count; j++)
                        ad.advState[j] = i1 - 1;
                    UpdateADVUI();
                });
            }
        }

#endif
        internal void UpdateUI()
        {
            if (_inCharaMaker)
                UpdateMakerUI();
#if KOIKATSU
            else if (_inStudio)
                UpdateStudioUI();
            else if (_inH)
                this.ExecuteDelayed(UpdateHUI);
#elif EMOTIONCREATORS
            else if (_inPlay)
                UpdatePlayUI();
#endif
        }

#if EMOTIONCREATORS
        internal void UpdatePlayUI()
        {
            if (_playUI == null || _playButtonTemplate == null || _playUI.selectChara == null)
                return;
            if (_updatePlayUIHandler == null)
                _updatePlayUIHandler = StartCoroutine(UpdatePlayUI_Routine());
        }

        // So, this thing is actually a coroutine because if I don't do the following, TextMeshPro start disappearing because their material is destroyed.
        // IDK, fuck you unity I guess
        private IEnumerator UpdatePlayUI_Routine()
        {
            while (_playButtonTemplate.gameObject.activeInHierarchy == false)
                yield return null;
            ChaControl character = _playUI.selectChara;

            CharAdditionalData additionalData = _accessoriesByChar[character.chaFile];
            int j;
            for (j = 0; j < additionalData.nowAccessories.Count; j++)
            {
                PlaySceneSlotData slot;
                if (j < _additionalPlaySceneSlots.Count)
                    slot = _additionalPlaySceneSlots[j];
                else
                {
                    slot = new PlaySceneSlotData();
                    slot.slot = (RectTransform)Instantiate(_playButtonTemplate.gameObject).transform;
                    slot.text = slot.slot.GetComponentInChildren<TextMeshProUGUI>(true);
                    slot.text.fontMaterial = new Material(slot.text.fontMaterial);
                    slot.button = slot.slot.GetComponentInChildren<Button>(true);
                    slot.slot.SetParent(_playButtonTemplate.parent);
                    slot.slot.localPosition = Vector3.zero;
                    slot.slot.localScale = Vector3.one;
                    int i1 = j;
                    slot.button.onClick = new Button.ButtonClickedEvent();
                    slot.button.onClick.AddListener(() =>
                    {
                        additionalData.showAccessories[i1] = !additionalData.showAccessories[i1];
                    });
                    _additionalPlaySceneSlots.Add(slot);
                }
                GameObject objAccessory = additionalData.objAccessory[j];
                if (objAccessory == null)
                    slot.slot.gameObject.SetActive(false);
                else
                {
                    slot.slot.gameObject.SetActive(true);
                    ListInfoComponent component = objAccessory.GetComponent<ListInfoComponent>();
                    slot.text.text = component.data.Name;
                }
            }

            for (; j < _additionalPlaySceneSlots.Count; ++j)
                _additionalPlaySceneSlots[j].slot.gameObject.SetActive(false);
            _updatePlayUIHandler = null;
        }

        internal void UpdateADVUI()
        {
            if (_advUI == null)
                return;

            CharAdditionalData additionalData = _accessoriesByChar[_advUI.chaControl.chaFile];
            int i = 0;
            for (; i < additionalData.nowAccessories.Count; ++i)
            {
                ADVSceneSlotData slot;
                if (i < _additionalADVSceneSlots.Count)
                    slot = _additionalADVSceneSlots[i];
                else
                {
                    slot = new ADVSceneSlotData();
                    slot.slot = (RectTransform)Instantiate(_advToggleTemplate.gameObject).transform;
                    slot.slot.SetParent(_advToggleTemplate.parent);
                    slot.slot.localPosition = Vector3.zero;
                    slot.slot.localRotation = Quaternion.identity;
                    slot.slot.localScale = Vector3.one;
                    slot.text = slot.slot.Find("TextMeshPro").GetComponent<TextMeshProUGUI>();
                    slot.keep = slot.slot.Find("Root/Button -1").GetComponent<Toggle>();
                    slot.wear = slot.slot.Find("Root/Button 0").GetComponent<Toggle>();
                    slot.takeOff = slot.slot.Find("Root/Button 1").GetComponent<Toggle>();
                    slot.text.text = "スロット" + (21 + i);

                    slot.keep.onValueChanged = new Toggle.ToggleEvent();
                    int i1 = i;
                    slot.keep.onValueChanged.AddListener(b =>
                    {
                        CharAdditionalData ad = _accessoriesByChar[_advUI.chaControl.chaFile];
                        ad.advState[i1] = -1;
                    });
                    slot.wear.onValueChanged = new Toggle.ToggleEvent();
                    slot.wear.onValueChanged.AddListener(b =>
                    {
                        CharAdditionalData ad = _accessoriesByChar[_advUI.chaControl.chaFile];
                        ad.advState[i1] = 0;
                        _advUI.chaControl.SetAccessoryState(i1 + 20, true);
                    });
                    slot.takeOff.onValueChanged = new Toggle.ToggleEvent();
                    slot.takeOff.onValueChanged.AddListener(b =>
                    {
                        CharAdditionalData ad = _accessoriesByChar[_advUI.chaControl.chaFile];
                        ad.advState[i1] = 1;
                        _advUI.chaControl.SetAccessoryState(i1 + 20, false);
                    });

                    _additionalADVSceneSlots.Add(slot);
                }
                slot.slot.gameObject.SetActive(true);
                slot.keep.SetIsOnNoCallback(additionalData.advState[i] == -1);
                slot.keep.interactable = additionalData.objAccessory[i] != null;
                slot.wear.SetIsOnNoCallback(additionalData.advState[i] == 0);
                slot.wear.interactable = additionalData.objAccessory[i] != null;
                slot.takeOff.SetIsOnNoCallback(additionalData.advState[i] == 1);
                slot.takeOff.interactable = additionalData.objAccessory[i] != null;
            }
            for (; i < _additionalADVSceneSlots.Count; i++)
                _additionalADVSceneSlots[i].slot.gameObject.SetActive(false);
            RectTransform parent = (RectTransform)_advToggleTemplate.parent.parent;
            parent.offsetMin = new Vector2(0, parent.offsetMax.y - 66 - 34 * (additionalData.nowAccessories.Count + 21));
            ExecuteDelayed(() =>
            {
                //Fuck you I'm going to bed
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_advToggleTemplate.parent.parent);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_advToggleTemplate.parent.parent.parent);
            });
        }
#endif
        internal void OnCoordTypeChange()
        {
            if (_inCharaMaker)
            {
                if (CustomBase.Instance.selectSlot >= 20 && !_additionalCharaMakerSlots[CustomBase.Instance.selectSlot - 20].toggle.gameObject.activeSelf)
                {
                    var toggle = _customAcsChangeSlot.items[0].tglItem;
                    toggle.isOn = true;
                    CustomBase.Instance.selectSlot = 0;
                }
            }
            UpdateUI();
        }
        #endregion

        #region Saves
        [HarmonyPatch(typeof(CustomControl), nameof(CustomControl.Entry))]
        private static class CustomScene_Initialize_Patches
        {
            private static void Prefix(ChaControl entryChara)
            {
                _self._overrideCharaLoadingFilePre = entryChara.chaFile;
            }
            private static void Postfix()
            {
                _self._overrideCharaLoadingFilePre = null;
            }
        }

        private static void UAR_ExtendedCardLoad_Prefix(ChaFile file)
        {
            _self.OnActualCharaLoad(file);
        }

        private static void UAR_ExtendedCardSave_Postfix(ChaFile file)
        {
            _self.OnActualCharaSave(file);
        }

        private static void UAR_ExtendedCoordLoad_Prefix(ChaFileCoordinate file)
        {
            _self.OnActualCoordLoad(file);
        }

        private static void UAR_ExtendedCoordSave_Postfix(ChaFileCoordinate file)
        {
            _self.OnActualCoordSave(file);
        }

        private void OnActualCharaLoad(ChaFile file)
        {
            if (_loadAdditionalAccessories == false)
                return;

            var pluginData = ExtendedSave.GetExtendedDataById(file, _extSaveKey);

            CharAdditionalData data;
            if (_accessoriesByChar.TryGetValue(file, out data) == false)
            {
                data = new CharAdditionalData();
                _accessoriesByChar.Add(file, data);
            }
            else
            {
                foreach (var pair in data.rawAccessoriesInfos)
                    pair.Value.Clear();
            }
            XmlNode node = null;
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out var xmlData))
            {
                var doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }
            if (node != null)
            {
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
                                var part = new ChaFileAccessory.PartsInfo();
                                part.type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value);
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
#if EMOTIONCREATORS
                                    if (accessoryNode.Attributes["hideTiming"] != null)
                                        part.hideTiming = XmlConvert.ToInt32(accessoryNode.Attributes["hideTiming"].Value);
#endif
                                    if (_hasDarkness)
                                        part.SetPrivateProperty("noShake", accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value));
                                }
                                parts.Add(part);
                            }
                            break;
#if KOIKATSU
                        case "visibility":
                            if (_inStudio)
                            {
                                data.showAccessories = new List<bool>();
                                foreach (XmlNode grandChildNode in childNode.ChildNodes)
                                    data.showAccessories.Add(grandChildNode.Attributes?["value"] == null || XmlConvert.ToBoolean(grandChildNode.Attributes["value"].Value));
                            }
                            break;
#endif
                    }
                }

            }
            if (data.rawAccessoriesInfos.TryGetValue(file.status.GetCoordinateType(), out data.nowAccessories) == false)
            {
                data.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                data.rawAccessoriesInfos.Add(file.status.GetCoordinateType(), data.nowAccessories);
            }
            while (data.infoAccessory.Count < data.nowAccessories.Count)
                data.infoAccessory.Add(null);
            while (data.objAccessory.Count < data.nowAccessories.Count)
                data.objAccessory.Add(null);
            while (data.objAcsMove.Count < data.nowAccessories.Count)
                data.objAcsMove.Add(new GameObject[2]);
            while (data.cusAcsCmp.Count < data.nowAccessories.Count)
                data.cusAcsCmp.Add(null);
            while (data.showAccessories.Count < data.nowAccessories.Count)
                data.showAccessories.Add(true);
#if EMOTIONCREATORS
            while (data.advState.Count < data.nowAccessories.Count)
                data.advState.Add(-1);
#endif


            if (
#if KOIKATSU
                    _inH ||
#endif
                    _inCharaMaker
            )
                this.ExecuteDelayed(UpdateUI);
            else
                UpdateUI();
            _accessoriesByChar.Purge();
            _charByCoordinate.Purge();
        }

        private void OnActualCharaSave(ChaFile file)
        {
            CharAdditionalData data;
            if (_accessoriesByChar.TryGetValue(file, out data) == false)
                return;

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
#if EMOTIONCREATORS
                            xmlWriter.WriteAttributeString("hideTiming", XmlConvert.ToString(part.hideTiming));
#endif
                            if (_hasDarkness)
                                xmlWriter.WriteAttributeString("noShake", XmlConvert.ToString(part.noShake));
                        }
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                }

#if KOIKATSU
                if (_inStudio)
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

                var pluginData = new PluginData();
                pluginData.version = _saveVersion;
                pluginData.data.Add("additionalAccessories", stringWriter.ToString());
                ExtendedSave.SetExtendedDataById(file, _extSaveKey, pluginData);
            }
        }

        private void OnActualCoordLoad(ChaFileCoordinate file)
        {
            if (_inCharaMaker && _loadCoordinatesWindow != null && _loadCoordinatesWindow.tglCoordeLoadAcs != null && _loadCoordinatesWindow.tglCoordeLoadAcs.isOn == false)
                _loadAdditionalAccessories = false;
            if (_loadAdditionalAccessories == false) // This stuff is done this way because some user might want to change _loadAdditionalAccessories manually through reflection.
            {
                _loadAdditionalAccessories = true;
                return;
            }

            WeakReference o;
            ChaFileControl chaFile = null;
            if (_self._charByCoordinate.TryGetValue(file, out o) == false || o.IsAlive == false)
            {
                foreach (var pair in Character.dictEntryChara)
                {
                    if (pair.Value.nowCoordinate == file)
                    {
                        chaFile = pair.Value.chaFile;
                        break;
                    }
                }
            }
            else
                chaFile = (ChaFileControl)o.Target;
            if (chaFile == null)
                return;
            CharAdditionalData data;
            if (_accessoriesByChar.TryGetValue(chaFile, out data) == false)
            {
                data = new CharAdditionalData();
                _accessoriesByChar.Add(chaFile, data);
            }
#if KOIKATSU
            if (_inH)
                data.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
            else
#endif
            {
                if (data.rawAccessoriesInfos.TryGetValue(chaFile.status.GetCoordinateType(), out data.nowAccessories) == false)
                {
                    data.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                    data.rawAccessoriesInfos.Add(chaFile.status.GetCoordinateType(), data.nowAccessories);
                }
                else
                    data.nowAccessories.Clear();
            }

            XmlNode node = null;
            var pluginData = ExtendedSave.GetExtendedDataById(file, _extSaveKey);
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out var xmlData))
            {
                var doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }
            if (node != null)
            {
                foreach (XmlNode accessoryNode in node.ChildNodes)
                {
                    var part = new ChaFileAccessory.PartsInfo();
                    part.type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value);
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
#if EMOTIONCREATORS
                        if (accessoryNode.Attributes["hideTiming"] != null)
                            part.hideTiming = XmlConvert.ToInt32(accessoryNode.Attributes["hideTiming"].Value);
#endif
                        if (_hasDarkness)
                            part.SetPrivateProperty("noShake", accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value));
                    }
                    data.nowAccessories.Add(part);
                }
            }

            while (data.infoAccessory.Count < data.nowAccessories.Count)
                data.infoAccessory.Add(null);
            while (data.objAccessory.Count < data.nowAccessories.Count)
                data.objAccessory.Add(null);
            while (data.objAcsMove.Count < data.nowAccessories.Count)
                data.objAcsMove.Add(new GameObject[2]);
            while (data.cusAcsCmp.Count < data.nowAccessories.Count)
                data.cusAcsCmp.Add(null);
            while (data.showAccessories.Count < data.nowAccessories.Count)
                data.showAccessories.Add(true);
#if EMOTIONCREATORS
            while (data.advState.Count < data.nowAccessories.Count)
                data.advState.Add(-1);
#endif

            if (
#if KOIKATSU
                    _inH ||
#endif
                    _inCharaMaker
            )
                this.ExecuteDelayed(UpdateUI);
            else
                UpdateUI();
            _accessoriesByChar.Purge();
            _charByCoordinate.Purge();
        }

        private void OnActualCoordSave(ChaFileCoordinate file)
        {
            WeakReference o;
            ChaFileControl chaFile = null;
            if (_self._charByCoordinate.TryGetValue(file, out o) == false || o.IsAlive == false)
            {
                foreach (var pair in Character.dictEntryChara)
                {
                    if (pair.Value.nowCoordinate == file)
                    {
                        chaFile = pair.Value.chaFile;
                        break;
                    }
                }
            }
            else
                chaFile = (ChaFileControl)o.Target;
            if (chaFile == null)
                return;

            CharAdditionalData data;
            if (_accessoriesByChar.TryGetValue(chaFile, out data) == false)
                return;
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
#if EMOTIONCREATORS
                        xmlWriter.WriteAttributeString("hideTiming", XmlConvert.ToString(part.hideTiming));
#endif
                        if (_hasDarkness)
                            xmlWriter.WriteAttributeString("noShake", XmlConvert.ToString(part.noShake));
                    }
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();

                var pluginData = new PluginData();
                pluginData.version = _saveVersion;
                pluginData.data.Add("additionalAccessories", stringWriter.ToString());
                ExtendedSave.SetExtendedDataById(file, _extSaveKey, pluginData);
            }
        }
        #endregion
    }
}