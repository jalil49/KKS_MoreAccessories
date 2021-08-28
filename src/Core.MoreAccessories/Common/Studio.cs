﻿#if EMOTIONCREATORS
using HPlay;
using ADVPart.Manipulate;
using ADVPart.Manipulate.Chara;
#endif
#if KOIKATSU
#endif
using MoreAccessoriesKOI.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace MoreAccessoriesKOI
{
    public partial class MoreAccessories
    {
#if KOIKATSU
        private void SpawnStudioUI()
        {
            var accList = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Slot").transform;
            _studioToggleTemplate = accList.Find("Slot20") as RectTransform;

            var ctrl = Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.m_MPCharCtrl;

            _studioToggleAll = new StudioSlotData();
            _studioToggleAll.slot = (RectTransform)Instantiate(_studioToggleTemplate.gameObject).transform;
            _studioToggleAll.name = _studioToggleAll.slot.GetComponentInChildren<Text>();
            _studioToggleAll.onButton = _studioToggleAll.slot.GetChild(1).GetComponent<Button>();
            _studioToggleAll.offButton = _studioToggleAll.slot.GetChild(2).GetComponent<Button>();
            _studioToggleAll.name.text = "全て";
            _studioToggleAll.slot.SetParent(_studioToggleTemplate.parent);
            _studioToggleAll.slot.localPosition = Vector3.zero;
            _studioToggleAll.slot.localScale = Vector3.one;
            _studioToggleAll.onButton.onClick = new Button.ButtonClickedEvent();
            _studioToggleAll.onButton.onClick.AddListener(() =>
            {
                _selectedStudioCharacter.charInfo.SetAccessoryStateAll(true);
                ctrl.CallPrivate("UpdateInfo");
                UpdateStudioUI();
            });
            _studioToggleAll.offButton.onClick = new Button.ButtonClickedEvent();
            _studioToggleAll.offButton.onClick.AddListener(() =>
            {
                _selectedStudioCharacter.charInfo.SetAccessoryStateAll(false);
                ctrl.CallPrivate("UpdateInfo");
                UpdateStudioUI();
            });
            _studioToggleAll.slot.SetAsLastSibling();

            _studioToggleMain = new StudioSlotData();
            _studioToggleMain.slot = (RectTransform)Instantiate(_studioToggleTemplate.gameObject).transform;
            _studioToggleMain.name = _studioToggleMain.slot.GetComponentInChildren<Text>();
            _studioToggleMain.onButton = _studioToggleMain.slot.GetChild(1).GetComponent<Button>();
            _studioToggleMain.offButton = _studioToggleMain.slot.GetChild(2).GetComponent<Button>();
            _studioToggleMain.name.text = "メイン";
            _studioToggleMain.slot.SetParent(_studioToggleTemplate.parent);
            _studioToggleMain.slot.localPosition = Vector3.zero;
            _studioToggleMain.slot.localScale = Vector3.one;
            _studioToggleMain.onButton.onClick = new Button.ButtonClickedEvent();
            _studioToggleMain.onButton.onClick.AddListener(() =>
            {
                _selectedStudioCharacter.charInfo.SetAccessoryStateCategory(0, true);
                ctrl.CallPrivate("UpdateInfo");
                UpdateStudioUI();
            });
            _studioToggleMain.offButton.onClick = new Button.ButtonClickedEvent();
            _studioToggleMain.offButton.onClick.AddListener(() =>
            {
                _selectedStudioCharacter.charInfo.SetAccessoryStateCategory(0, false);
                ctrl.CallPrivate("UpdateInfo");
                UpdateStudioUI();
            });
            _studioToggleMain.slot.SetAsLastSibling();

            _studioToggleSub = new StudioSlotData();
            _studioToggleSub.slot = (RectTransform)Instantiate(_studioToggleTemplate.gameObject).transform;
            _studioToggleSub.name = _studioToggleSub.slot.GetComponentInChildren<Text>();
            _studioToggleSub.onButton = _studioToggleSub.slot.GetChild(1).GetComponent<Button>();
            _studioToggleSub.offButton = _studioToggleSub.slot.GetChild(2).GetComponent<Button>();
            _studioToggleSub.name.text = "サブ";
            _studioToggleSub.slot.SetParent(_studioToggleTemplate.parent);
            _studioToggleSub.slot.localPosition = Vector3.zero;
            _studioToggleSub.slot.localScale = Vector3.one;
            _studioToggleSub.onButton.onClick = new Button.ButtonClickedEvent();
            _studioToggleSub.onButton.onClick.AddListener(() =>
            {
                _selectedStudioCharacter.charInfo.SetAccessoryStateCategory(1, true);
                ctrl.CallPrivate("UpdateInfo");
                UpdateStudioUI();
            });
            _studioToggleSub.offButton.onClick = new Button.ButtonClickedEvent();
            _studioToggleSub.offButton.onClick.AddListener(() =>
            {
                _selectedStudioCharacter.charInfo.SetAccessoryStateCategory(1, false);
                ctrl.CallPrivate("UpdateInfo");
                UpdateStudioUI();
            });
            _studioToggleSub.slot.SetAsLastSibling();

        }

        internal void UpdateStudioUI()
        {
            if (_selectedStudioCharacter == null)
                return;
            var additionalData = _accessoriesByChar[_selectedStudioCharacter.charInfo.chaFile];
            int i;
            for (i = 0; i < additionalData.nowAccessories.Count; i++)
            {
                StudioSlotData slot;
                var accessory = additionalData.nowAccessories[i];
                if (i < _additionalStudioSlots.Count)
                {
                    slot = _additionalStudioSlots[i];
                }
                else
                {
                    slot = new StudioSlotData();
                    slot.slot = (RectTransform)Instantiate(_studioToggleTemplate.gameObject).transform;
                    slot.name = slot.slot.GetComponentInChildren<Text>();
                    slot.onButton = slot.slot.GetChild(1).GetComponent<Button>();
                    slot.offButton = slot.slot.GetChild(2).GetComponent<Button>();
                    slot.name.text = "スロット" + (21 + i);
                    slot.slot.SetParent(_studioToggleTemplate.parent);
                    slot.slot.localPosition = Vector3.zero;
                    slot.slot.localScale = Vector3.one;
                    var i1 = i;
                    slot.onButton.onClick = new Button.ButtonClickedEvent();
                    slot.onButton.onClick.AddListener(() =>
                    {
                        _accessoriesByChar[_selectedStudioCharacter.charInfo.chaFile].showAccessories[i1] = true;
                        slot.onButton.image.color = Color.green;
                        slot.offButton.image.color = Color.white;
                    });
                    slot.offButton.onClick = new Button.ButtonClickedEvent();
                    slot.offButton.onClick.AddListener(() =>
                    {
                        _accessoriesByChar[_selectedStudioCharacter.charInfo.chaFile].showAccessories[i1] = false;
                        slot.offButton.image.color = Color.green;
                        slot.onButton.image.color = Color.white;
                    });
                    _additionalStudioSlots.Add(slot);
                }
                slot.slot.gameObject.SetActive(true);
                slot.onButton.interactable = accessory != null && accessory.type != 120;
                slot.onButton.image.color = slot.onButton.interactable && additionalData.showAccessories[i] ? Color.green : Color.white;
                slot.offButton.interactable = accessory != null && accessory.type != 120;
                slot.offButton.image.color = slot.onButton.interactable && !additionalData.showAccessories[i] ? Color.green : Color.white;
            }
            for (; i < _additionalStudioSlots.Count; ++i)
                _additionalStudioSlots[i].slot.gameObject.SetActive(false);
            _studioToggleSub.slot.SetAsFirstSibling();
            _studioToggleMain.slot.SetAsFirstSibling();
            _studioToggleAll.slot.SetAsFirstSibling();
        }
#endif
    }
}