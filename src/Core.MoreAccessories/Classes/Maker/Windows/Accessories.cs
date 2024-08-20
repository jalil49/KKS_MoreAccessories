using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using ChaCustom;
using Illusion.Extensions;
using MoreAccessoriesKOI.Extensions;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;


namespace MoreAccessoriesKOI
{
    /// <summary>
    /// Handles adjusting position of slots/windows and adds scrolling to them.
    /// If Unwanted dependency hook CustomAcsChangeSlot.Start for reference 
    /// </summary>
    public class Accessories
    {
        public CustomAcsChangeSlot AccessoryTab { get; }
        internal static MoreAccessories Plugin => MoreAccessories._self;
        internal GameObject ScrollTemplate;
        internal static int ShowSlot = 20;

        #region Properties

        private static bool Ready => MoreAccessories.MakerMode.Ready;

        internal CustomAcsParentWindow ParentWin => AccessoryTab.customAcsParentWin;

        internal CustomAcsMoveWindow[] MoveWin
        {
            get => AccessoryTab.customAcsMoveWin;
            set => AccessoryTab.customAcsMoveWin = value;
        }

        internal CustomAcsSelectKind[] SelectKind
        {
            get => AccessoryTab.customAcsSelectKind;
            set => AccessoryTab.customAcsSelectKind = value;
        }

        internal CvsAccessory[] CvsAccessoryArray
        {
            get => AccessoryTab.cvsAccessory;
            set => AccessoryTab.cvsAccessory = value;
        }

        internal bool WindowMoved; //wait for window to be moved to the left before allowing UpdateUI
        public static bool AddInProgress { get; private set; } //Don't allow spamming of the add buttons

        #endregion

        internal Accessories(CustomAcsChangeSlot instance)
        {
            AddInProgress = false; //in case of something going wrong and its static
            AccessoryTab = instance;
            PrepareScroll();
            MakeSlotsScrollable();
            Plugin.ExecuteDelayed(InitializeSlotNames, 5);
        }

        internal List<CharaMakerSlotData> AdditionalCharaMakerSlots
        {
            get => MoreAccessories.MakerMode.AdditionalCharaMakerSlots;
            set => MoreAccessories.MakerMode.AdditionalCharaMakerSlots = value;
        }

        private ScrollRect _scrollView;
        private const float ButtonWidth = 175f;
        private float _height;
        private float _slotUIPositionY;
        private RectTransform _addButtonsGroup;
        private VerticalLayoutGroup _parentGroup;

        private void PrepareScroll()
        {
            var originalScrollRect = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/03_ClothesTop/tglTop/TopTop/Scroll View").GetComponent<ScrollRect>();

            ScrollTemplate = DefaultControls.CreateScrollView(new DefaultControls.Resources());
            var templateScrollRect = ScrollTemplate.GetComponent<ScrollRect>();

            templateScrollRect.verticalScrollbar.GetComponent<Image>().sprite = originalScrollRect.verticalScrollbar.GetComponent<Image>().sprite;
            templateScrollRect.verticalScrollbar.image.sprite = originalScrollRect.verticalScrollbar.image.sprite;

            templateScrollRect.horizontal = false;
            templateScrollRect.scrollSensitivity = 40f;

            templateScrollRect.movementType = ScrollRect.MovementType.Clamped;
            templateScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            if (templateScrollRect.horizontalScrollbar != null)
                Object.DestroyImmediate(templateScrollRect.horizontalScrollbar.gameObject);
            Object.DestroyImmediate(templateScrollRect.GetComponent<Image>());
        }

        internal void ValidateToggles()
        {
            var index = AccessoryTab.GetSelectIndex();
            if (index < 0)
            {
                return;
            }

            var partCount = CustomBase.instance.chaCtrl.nowCoordinate.accessory.parts.Length;
            if (AccessoryTab.items[index].tglItem.isOn && AccessoryTab.items[index].cgItem.alpha < .1f)
            {
                AccessoryTab.items[index].tglItem.Set(false);
                AccessoryTab.items[0].tglItem.Set(true);
            }
#if KK || KKS
            if (index >= partCount && index < AccessoryTab.items.Length - 2)
#else
            if (index >= partCount && index < AccessoryTab.items.Length - 1)
#endif
            {
                AccessoryTab.CloseWindow();
                AccessoryTab.items[index].tglItem.Set(false);
                AccessoryTab.items[0].tglItem.Set(true);
            }

            FixWindowScroll();
        }

        private void MakeSlotsScrollable()
        {
            var container = (RectTransform)AccessoryTab.transform;

            //adjust size of all buttons (shrunk to take less screen space/maker window wider)
            foreach (var slotTransform in container.Cast<Transform>())
            {
                var layout = slotTransform.GetComponent<LayoutElement>();
                layout.minWidth = ButtonWidth;
                layout.preferredWidth = ButtonWidth;
            }

            _scrollView = Object.Instantiate(ScrollTemplate, container).GetComponent<ScrollRect>();
            _scrollView.name = "Slots";
            _scrollView.onValueChanged.AddListener(x => { FixWindowScroll(); });
            _scrollView.movementType = ScrollRect.MovementType.Clamped;
            _scrollView.horizontal = false;
            _scrollView.scrollSensitivity = 18f;
            _scrollView.verticalScrollbarSpacing = -17f; //offset to avoid clipping window scroll because mask decreases in width to fit the vertical scrollbar (that is moved)

            var element = _scrollView.gameObject.AddComponent<LayoutElement>();
            _height = element.minHeight = 832f;
            element.minWidth = 600f;

            var vlg = _scrollView.content.gameObject.AddComponent<VerticalLayoutGroup>();
            _parentGroup = container.GetComponent<VerticalLayoutGroup>();

            vlg.childAlignment = _parentGroup.childAlignment;
            vlg.childControlHeight = _parentGroup.childControlHeight;
            vlg.childControlWidth = _parentGroup.childControlWidth;
            vlg.childForceExpandHeight = _parentGroup.childForceExpandHeight;
            vlg.childForceExpandWidth = _parentGroup.childForceExpandWidth;
            vlg.spacing = _parentGroup.spacing;

            _scrollView.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scrollView.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (var i = 0; i < CvsAccessoryArray.Length; i++)
            {
                var child = container.GetChild(0);
                MakeWindowScrollable(child);
                container.GetChild(0).SetParent(_scrollView.content);
            }

            _scrollView.transform.SetAsFirstSibling();
            var toggleChange = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/tglChange").GetComponent<Toggle>();
            _addButtonsGroup = UIUtility.CreateNewUIObject("Add Buttons Group", _scrollView.content);
            element = _addButtonsGroup.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = ButtonWidth;
            element.preferredHeight = 32;
            var textModel = toggleChange.transform.Find("imgOff").GetComponentInChildren<TextMeshProUGUI>().gameObject;

            var addOneButton = UIUtility.CreateButton("Add One Button", _addButtonsGroup, "+1");
            addOneButton.transform.SetRect(Vector2.zero, new Vector2(0.5f, 1f));
            addOneButton.colors = toggleChange.colors;
            ((Image)addOneButton.targetGraphic).sprite = toggleChange.transform.Find("imgOff").GetComponent<Image>().sprite;
            Object.Destroy(addOneButton.GetComponentInChildren<Text>().gameObject);
            var text = Object.Instantiate(textModel).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(addOneButton.transform);
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 4f), new Vector2(-5f, -4f));
            text.text = "+1";
            addOneButton.onClick.AddListener(delegate { AddSlot(1); });

            var addTenButton = UIUtility.CreateButton("Add Ten Button", _addButtonsGroup, "+10");
            addTenButton.transform.SetRect(new Vector2(0.5f, 0f), Vector2.one);
            addTenButton.colors = toggleChange.colors;
            ((Image)addTenButton.targetGraphic).sprite = toggleChange.transform.Find("imgOff").GetComponent<Image>().sprite;
            Object.Destroy(addTenButton.GetComponentInChildren<Text>().gameObject);
            text = Object.Instantiate(textModel).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(addTenButton.transform);
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 4f), new Vector2(-5f, -4f));
            text.text = "+10";
            addTenButton.onClick.AddListener(delegate { AddSlot(10); });

            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
#if KK || KKS
            for (int i = 0, j = AccessoryTab.items.Length - 1; i < 2; j--, i++)
#else
            for (int i = 0, j = AccessoryTab.items.Length - 1; i < 1; j--, i++)
#endif
            {
                var data = AccessoryTab.items[j];
                var temp = i;
                var index = j;
                data.tglItem.onValueChanged.AddListener(b =>
                {
                    AccessoryTab.CloseWindow();
                    if (temp == 0) //Transfer Window
                    {
                        if (CvsAccessoryArray.Length > 20) //disable slot [index of transfer 22 kk/kks 21 EC] from showing
                        {
                            AccessoryTab.items[index].cgItem.Enable(false);
                        }

                        MoreAccessories.MakerMode.TransferWindow.WindowRefresh();
                    }
#if KK || KKS
                    if (temp == 1) //Copy Window
                    {
                        if (CvsAccessoryArray.Length > 21) //disable slot [index of copy 21 kk/kks] from showing
                        {
                            AccessoryTab.items[index].cgItem.Enable(false);
                        }

                        MoreAccessories.MakerMode.CopyWindow.WindowRefresh();
                    }
#endif
                    var t = data.cgItem.transform;
                    t.position = new Vector3(t.position.x, _slotUIPositionY);
                });
            }

            //moving this up results in not moving scrollbar
            var transform = _scrollView.verticalScrollbar.transform;
            var localPosition = transform.localPosition;
            localPosition = new Vector3(-105f, localPosition.y, localPosition.z);
            transform.localPosition = localPosition;

            AccessoryTab.ExecuteDelayed(() =>
            {
                CvsAccessoryArray[0].UpdateCustomUI();
                CvsAccessoryArray[0].tglTakeOverParent.Set(false);
                CvsAccessoryArray[0].tglTakeOverColor.Set(false);
                _scrollView.viewport.gameObject.SetActive(true);
                _slotUIPositionY = container.position.y;
            }, 5);

            _scrollView.viewport.gameObject.SetActive(false);
        }

        private void MakeWindowScrollable(Transform slotTransform)
        {
            var listParent = slotTransform.Cast<Transform>().First(x => x.name.EndsWith("Top"));

            var elements = new List<Transform>();
            foreach (Transform t in listParent)
                elements.Add(t);

            Plugin.ExecuteDelayed(delegate
            {
                listParent.localPosition -= new Vector3(50, 0, 0);
                WindowMoved = true;
            });

            var fitter = listParent.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollTransform = Object.Instantiate(ScrollTemplate, listParent);
            scrollTransform.name = "Scroll View";

            scrollTransform.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;


            var scrollLayoutElement = scrollTransform.AddComponent<LayoutElement>();

            scrollLayoutElement.preferredWidth = 400;
            scrollLayoutElement.preferredHeight = _height;

            var scroll = scrollTransform.GetComponent<ScrollRect>();
            var vlg = scroll.content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = _parentGroup.childAlignment;
            vlg.childControlHeight = _parentGroup.childControlHeight;
            vlg.childControlWidth = _parentGroup.childControlWidth;
            vlg.childForceExpandHeight = _parentGroup.childForceExpandHeight;
            vlg.childForceExpandWidth = _parentGroup.childForceExpandWidth;
            vlg.spacing = _parentGroup.spacing;

            scroll.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var item in elements)
            {
                var itemLayout = item.GetComponent<LayoutElement>();
                itemLayout.flexibleWidth = 1;
                item.SetParent(scroll.content);
            }

            slotTransform.SetParent(scroll.content);
        }

        internal void FixWindowScroll()
        {
            var selectedSlot = AccessoryTab.GetSelectIndex();
            if (selectedSlot < 0) return;
            var t = AccessoryTab.items[selectedSlot].cgItem.transform;
            t.position = new Vector3(t.position.x, _slotUIPositionY);
        }

        public void UpdateUI()
        {
            if (!Ready)
            {
                return;
            }

            var count = CustomBase.instance.chaCtrl.nowCoordinate.accessory.parts.Length - 20;
            if (count > AdditionalCharaMakerSlots.Count)
            {
                return;
            }

            var cvsColor = CvsColor(CustomBase.instance.chaCtrl.nowCoordinate.accessory.parts.Length + 1);

            var slotIndex = 0;
            for (; slotIndex < count; slotIndex++)
            {
                var info = AdditionalCharaMakerSlots[slotIndex];
                if (!info.AccessorySlot)
                {
                    var index = slotIndex + 20;
                    var customBase = CustomBase.instance;
                    var newSlot = Object.Instantiate(_scrollView.content.GetChild(0), _scrollView.content);

                    info.AccessorySlot = newSlot.gameObject;
                    var toggle = newSlot.GetComponent<Toggle>();
                    toggle.Set(false);

                    var canvasGroup = toggle.transform.GetChild(1).GetComponentInChildren<CanvasGroup>();
                    var cvsAccessory = toggle.GetComponentInChildren<CvsAccessory>();

                    cvsAccessory.textSlotName = toggle.GetComponentInChildren<TextMeshProUGUI>();

                    CvsAccessoryArray = CvsAccessoryArray.Concat(cvsAccessory).ToArray();

                    cvsAccessory.colorKind = cvsColor;
                    foreach (var item in CvsAccessoryArray)
                    {
                        item.colorKind = cvsColor;
                    }

                    var trans = canvasGroup.transform;
                    trans.position = new Vector3(trans.position.x, _slotUIPositionY);
#if KK || KKS
                    AccessoryTab.items = AccessoryTab.items.ConcatNearEnd(new UI_ToggleGroupCtrl.ItemInfo() { tglItem = toggle, cgItem = canvasGroup });
#elif EC
                    AccessoryTab.items = AccessoryTab.items.ConcatNearEnd(new UI_ToggleGroupCtrl.ItemInfo { tglItem = toggle, cgItem = canvasGroup }, 1);
#endif
                    foreach (var selectKind in SelectKind)
                    {
                        selectKind.cvsAccessory = CvsAccessoryArray;
                    }

                    foreach (var moveWindow in MoveWin)
                    {
                        moveWindow.cvsAccessory = CvsAccessoryArray;
                    }

                    ParentWin.cvsAccessory = CvsAccessoryArray;

                    canvasGroup.Enable(false);

                    cvsAccessory.textSlotName.text = $"スロット{index + 1:00}";
                    cvsAccessory.slotNo = (CvsAccessory.AcsSlotNo)index;
                    cvsAccessory.CalculateUI(); //fixes copying data over from original slot
                    Plugin.ExecuteDelayed(cvsAccessory.CalculateUI); //fixes copying data over from original slot
                    newSlot.name = "tglSlot" + (index + 1).ToString("00");

                    customBase.actUpdateCvsAccessory = customBase.actUpdateCvsAccessory.Concat(cvsAccessory.UpdateCustomUI).ToArray();
                    customBase.actUpdateAcsSlotName = customBase.actUpdateAcsSlotName.Concat(delegate { Plugin.ExecuteDelayed(cvsAccessory.UpdateSlotName); }).ToArray(); //delay to avoid an error when called early due to additional patches
                    var newReactive = new BoolReactiveProperty(false);
                    customBase._updateCvsAccessory = customBase._updateCvsAccessory.Concat(newReactive).ToArray();
                    AccessoryTab.textSlotNames = AccessoryTab.textSlotNames.Concat(cvsAccessory.textSlotName).ToArray();
                    newReactive.Subscribe(delegate
                    {
                        if (index == customBase.selectSlot && customBase.actUpdateCvsAccessory.Length > index)
                        {
                            customBase.actUpdateCvsAccessory[index]?.Invoke();
                        }

                        if (customBase.actUpdateAcsSlotName.Length > index)
                            customBase.actUpdateAcsSlotName[index].Invoke();
                        if (customBase._updateCvsAccessory.Length > index)
                            customBase._updateCvsAccessory[index].Value = false;
                    });

                    RestoreToggle(toggle, index);

                    _addButtonsGroup.SetAsLastSibling();

                    //Plugin.ExecuteDelayed(action);
                    try
                    {
                        Plugin.NewSlotAdded(index, newSlot.transform);
                    }
                    catch (Exception ex)
                    {
                        MoreAccessories.Print(ex.ToString(), LogLevel.Error);
                    }
                }

                var show = slotIndex < ShowSlot - 20;
                info.AccessorySlot.SetActive(show);

                if (show)
                {
                    if (slotIndex + 20 == CustomBase.Instance.selectSlot)
                        Plugin.ExecuteDelayed(() => { info.AccessorySlot.GetComponentInChildren<CvsAccessory>().UpdateCustomUI(); });
                    if (CvsAccessoryArray.Length > slotIndex + 20)
                        CvsAccessoryArray[slotIndex + 20].UpdateSlotName();
                }

                if (info.transferSlotObject) info.transferSlotObject.SetActive(show);
#if KK || KKS
                if (info.copySlotObject) info.copySlotObject.SetActive(true);
#endif
            }

            MoreAccessories.MakerMode.ValidateToggles();

            for (; slotIndex < AdditionalCharaMakerSlots.Count; slotIndex++)
            {
                var slot = AdditionalCharaMakerSlots[slotIndex];
                if (slot.AccessorySlot) slot.AccessorySlot.SetActive(false);
                if (slot.transferSlotObject) slot.transferSlotObject.SetActive(false);
#if KK || KKS
                if (slot.copySlotObject) slot.copySlotObject.SetActive(false);
#endif
            }

            _addButtonsGroup.SetAsLastSibling();
            FixWindowScroll();
        }

        private static int[,] CvsColor(int rank)
        {
            var newArray = new int[rank, 4];
            var value = 124;
            for (var i = 0; i < 20; i++)
            {
                for (var j = 0; j < 4; j++, value++)
                {
                    newArray[i, j] = value;
                }
            }

            //there is a break here with KKS since they appended to end of enum
            value = 5000;
            for (var i = 20; i < rank; i++)
            {
                for (var j = 0; j < 4; j++, value++)
                {
                    newArray[i, j] = value;
                }
            }

            return newArray;
        }

        private void RestoreToggle(Toggle toggle, int index)
        {
            toggle.OnValueChangedAsObservable().Subscribe(_ =>
            {
                if (index >= CustomBase.instance.chaCtrl.nowCoordinate.accessory.parts.Length)
                {
                    ParentWin.ChangeSlot(index, false);
                    var accessoryMoveWindows = AccessoryTab.customAcsMoveWin;
                    foreach (var window in accessoryMoveWindows)
                    {
                        window.ChangeSlot(index, false);
                    }

                    var accessorySelectKindWindows = AccessoryTab.customAcsSelectKind;
                    foreach (var window in accessorySelectKindWindows)
                    {
                        window.ChangeSlot(index, false);
                    }

                    return;
                }

                var open = 120 != CustomBase.instance.chaCtrl.nowCoordinate.accessory.parts[index].type;
                ParentWin.ChangeSlot(index, open);
                var moveWindows = AccessoryTab.customAcsMoveWin;
                foreach (var window in moveWindows)
                {
                    window.ChangeSlot(index, open);
                }

                var selectKindsWindows = AccessoryTab.customAcsSelectKind;
                foreach (var window in selectKindsWindows)
                {
                    window.ChangeSlot(index, open);
                }

                Singleton<CustomBase>.Instance.selectSlot = index;
                Singleton<CustomBase>.Instance.SetUpdateCvsAccessory(index, true);
                if (AccessoryTab.backIndex != index)
                {
                    AccessoryTab.ChangeColorWindow(index);
                }

                AccessoryTab.backIndex = index;
                FixWindowScroll();

                for (var i = 0; i < AccessoryTab.items.Length; i++)
                {
                    var info = AccessoryTab.items[i];
                    if (index == i && toggle.isOn)
                    {
                        info.cgItem.Enable(true);
                        continue;
                    }

                    info.cgItem.Enable(false);
                }
            });
        }

        public static void AddSlot(int num)
        {
            if (AddInProgress || !CustomBase.instance) return; //stop multi click or repeatedly triggering this while in progress just in case
            AddInProgress = true;
            var controller = CustomBase.instance.chaCtrl;
            var nowParts = controller.nowCoordinate.accessory.parts;
#if KK || KKS
            var accessory = controller.chaFile.coordinate[controller.chaFile.status.coordinateType].accessory;
#else
            var accessory = controller.chaFile.coordinate.accessory;
#endif
            var delta = num + ShowSlot - nowParts.Length;
            if (delta > 0)
            {
                var newParts = new ChaFileAccessory.PartsInfo[delta];
                for (var i = 0; i < delta; i++)
                {
                    newParts[i] = new ChaFileAccessory.PartsInfo();
                }

                accessory.parts = controller.nowCoordinate.accessory.parts = nowParts.Concat(newParts).ToArray();
            }

            ShowSlot += num;
            MoreAccessories.ArraySync(controller);
            AddInProgress = false;
        }

        private void InitializeSlotNames()
        {
            foreach (var item in CvsAccessoryArray)
            {
                item.UpdateSlotName();
            }
        }
    }
}
