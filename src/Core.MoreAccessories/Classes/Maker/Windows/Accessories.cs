using ChaCustom;
using Illusion.Extensions;
using MoreAccessoriesKOI.Extensions;
using System.Collections.Generic;
using System.Linq;
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
        public CustomAcsChangeSlot AccessoryTab { get; private set; }
        internal static MoreAccessories Plugin => MoreAccessories._self;
        internal GameObject scrolltemplate;
        internal static int ShowSlot = 20;

        #region Properties

        private bool Ready => MoreAccessories.MakerMode.ready;

        internal CustomAcsParentWindow ParentWin
        {
            get { return AccessoryTab.customAcsParentWin; }
        }

        internal CustomAcsMoveWindow[] MoveWin
        {
            get { return AccessoryTab.customAcsMoveWin; }
            set { AccessoryTab.customAcsMoveWin = value; }
        }

        internal CustomAcsSelectKind[] SelectKind
        {
            get { return AccessoryTab.customAcsSelectKind; }
            set { AccessoryTab.customAcsSelectKind = value; }
        }

        internal CvsAccessory[] CvsAccessoryArray
        {
            get { return AccessoryTab.cvsAccessory; }
            set { AccessoryTab.cvsAccessory = value; }
        }

        internal bool WindowMoved; //wait for window to be moved to the left before allowing UpdateUI
        public static bool AddInProgress { get; private set; } //Don't allow spamming of the add buttons

        #endregion

        internal Accessories(CustomAcsChangeSlot _instance)
        {
            AddInProgress = false; //in case of something going wrong and its static
            AccessoryTab = _instance;
            PrepareScroll();
            MakeSlotsScrollable();
            Plugin.ExecuteDelayed(InitilaizeSlotNames, 5);
        }

        internal List<CharaMakerSlotData> AdditionalCharaMakerSlots
        {
            get { return MoreAccessories.MakerMode._additionalCharaMakerSlots; }
            set { MoreAccessories.MakerMode._additionalCharaMakerSlots = value; }
        }

        private ScrollRect ScrollView;
        private readonly float buttonwidth = 175f;
        private float height;
        private float _slotUIPositionY;
        private RectTransform _addButtonsGroup;
        private VerticalLayoutGroup parentGroup;

        private void PrepareScroll()
        {
            var original_scroll = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/03_ClothesTop/tglTop/TopTop/Scroll View").GetComponent<ScrollRect>();

            scrolltemplate = DefaultControls.CreateScrollView(new DefaultControls.Resources());
            var scrollrect = scrolltemplate.GetComponent<ScrollRect>();

            scrollrect.verticalScrollbar.GetComponent<Image>().sprite = original_scroll.verticalScrollbar.GetComponent<Image>().sprite;
            scrollrect.verticalScrollbar.image.sprite = original_scroll.verticalScrollbar.image.sprite;

            scrollrect.horizontal = false;
            scrollrect.scrollSensitivity = 40f;

            scrollrect.movementType = ScrollRect.MovementType.Clamped;
            scrollrect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            if (scrollrect.horizontalScrollbar != null)
                Object.DestroyImmediate(scrollrect.horizontalScrollbar.gameObject);
            Object.DestroyImmediate(scrollrect.GetComponent<Image>());
        }

        internal void ValidatateToggles()
        {
            var index = AccessoryTab.GetSelectIndex();
            if (index < 0)
            {
                return;
            }

            var partcount = CustomBase.instance.chaCtrl.nowCoordinate.accessory.parts.Length;
            if (AccessoryTab.items[index].tglItem.isOn && AccessoryTab.items[index].cgItem.alpha < .1f)
            {
                AccessoryTab.items[index].tglItem.Set(false,false);
            }
#if KK || KKS
            if (index >= partcount + 2)
#else
            if (index >= partcount + 1 )
#endif
            {
                AccessoryTab.CloseWindow();
                AccessoryTab.items[index].tglItem.Set(false);
                AccessoryTab.items[0].tglItem.Set(true);
                CustomBase.instance.selectSlot = 0;
            }

            FixWindowScroll();
        }

        private void MakeSlotsScrollable()
        {
            var container = (RectTransform)AccessoryTab.transform;

            //adjust size of all buttons (shrunk to take less screenspace/maker window wider)
            foreach (var slotTransform in container.Cast<Transform>())
            {
                var layout = slotTransform.GetComponent<LayoutElement>();
                layout.minWidth = buttonwidth;
                layout.preferredWidth = buttonwidth;
            }

            ScrollView = Object.Instantiate(scrolltemplate, container).GetComponent<ScrollRect>();
            ScrollView.name = "Slots";
            ScrollView.onValueChanged.AddListener(x => { FixWindowScroll(); });
            ScrollView.movementType = ScrollRect.MovementType.Clamped;
            ScrollView.horizontal = false;
            ScrollView.scrollSensitivity = 18f;
            ScrollView.verticalScrollbarSpacing = -17f; //offset to avoid clipping window scroll because mask decreases in width to fit the vertical scrollbar (that is moved)

            var _charaMakerSlotTemplate = container.GetChild(0).gameObject;

            var rootCanvas = (RectTransform)_charaMakerSlotTemplate.GetComponentInParent<Canvas>().transform;
            var element = ScrollView.gameObject.AddComponent<LayoutElement>();
            height = element.minHeight = 832f;
            element.minWidth = 600f;

            var vlg = ScrollView.content.gameObject.AddComponent<VerticalLayoutGroup>();
            parentGroup = container.GetComponent<VerticalLayoutGroup>();

            vlg.childAlignment = parentGroup.childAlignment;
            vlg.childControlHeight = parentGroup.childControlHeight;
            vlg.childControlWidth = parentGroup.childControlWidth;
            vlg.childForceExpandHeight = parentGroup.childForceExpandHeight;
            vlg.childForceExpandWidth = parentGroup.childForceExpandWidth;
            vlg.spacing = parentGroup.spacing;

            ScrollView.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollView.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (var i = 0; i < CvsAccessoryArray.Length; i++)
            {
                var child = container.GetChild(0);
                MakeWindowScrollable(child);
                container.GetChild(0).SetParent(ScrollView.content);
            }

            ScrollView.transform.SetAsFirstSibling();
            var toggleChange = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/tglChange").GetComponent<Toggle>();
            _addButtonsGroup = UIUtility.CreateNewUIObject("Add Buttons Group", ScrollView.content);
            element = _addButtonsGroup.gameObject.AddComponent<LayoutElement>();
            var childreference = ScrollView.content.GetChild(0).GetComponent<LayoutElement>();
            element.preferredWidth = buttonwidth;
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
            addOneButton.onClick.AddListener(delegate() { AddSlot(1); });

            var addTenButton = UIUtility.CreateButton("Add Ten Button", _addButtonsGroup, "+10");
            addTenButton.transform.SetRect(new Vector2(0.5f, 0f), Vector2.one);
            addTenButton.colors = toggleChange.colors;
            ((Image)addTenButton.targetGraphic).sprite = toggleChange.transform.Find("imgOff").GetComponent<Image>().sprite;
            Object.Destroy(addTenButton.GetComponentInChildren<Text>().gameObject);
            text = Object.Instantiate(textModel).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(addTenButton.transform);
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 4f), new Vector2(-5f, -4f));
            text.text = "+10";
            addTenButton.onClick.AddListener(delegate() { AddSlot(10); });

            LayoutRebuilder.ForceRebuildLayoutImmediate(container);

            SlotButtonFunctionality();

//moving this up results in not moving scrollbar
            ScrollView.verticalScrollbar.transform.localPosition = new Vector3(-105f, ScrollView.verticalScrollbar.transform.localPosition.y, ScrollView.verticalScrollbar.transform.localPosition.z);

            AccessoryTab.ExecuteDelayed(() =>
            {
                CvsAccessoryArray[0].UpdateCustomUI();
                CvsAccessoryArray[0].tglTakeOverParent.Set(false);
                CvsAccessoryArray[0].tglTakeOverColor.Set(false);
                ScrollView.viewport.gameObject.SetActive(true);
                _slotUIPositionY = container.position.y;
            }, 5);

            ScrollView.viewport.gameObject.SetActive(false);
        }

        private void SlotButtonFunctionality()
        {
            var itemInfos = AccessoryTab.items;
            var index = 0;
            foreach (var itemInfo in itemInfos)
            {
                RestoreToggle(itemInfo, index++);
            }
        }

        private void MakeWindowScrollable(Transform slotTransform)
        {
            var listParent = slotTransform.Cast<Transform>().First(x => x.name.EndsWith("Top"));

            var elements = new List<Transform>();
            foreach (Transform t in listParent)
                elements.Add(t);

            Plugin.ExecuteDelayed(delegate()
            {
                listParent.localPosition -= new Vector3(50, 0, 0);
                WindowMoved = true;
            });

            var fitter = listParent.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollTransform = Object.Instantiate(scrolltemplate, listParent);
            scrollTransform.name = $"Scroll View";

            scrollTransform.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;


            var s_LE = scrollTransform.AddComponent<LayoutElement>();

            s_LE.preferredWidth = 400;
            s_LE.preferredHeight = height;

            var scroll = scrollTransform.GetComponent<ScrollRect>();
            var vlg = scroll.content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = parentGroup.childAlignment;
            vlg.childControlHeight = parentGroup.childControlHeight;
            vlg.childControlWidth = parentGroup.childControlWidth;
            vlg.childForceExpandHeight = parentGroup.childForceExpandHeight;
            vlg.childForceExpandWidth = parentGroup.childForceExpandWidth;
            vlg.spacing = parentGroup.spacing;

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
            FixWindowScroll(AccessoryTab.items[selectedSlot].cgItem);
        }

        internal void FixWindowScroll(CanvasGroup toggle)
        {
            var transform = toggle.transform;
            transform.position = new Vector3(transform.position.x, _slotUIPositionY);
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

            var cvscolor = CVSColor(CustomBase.instance.chaCtrl.nowCoordinate.accessory.parts.Length + 1);
            var slotindex = 0;
            for (; slotindex < count; slotindex++)
            {
                var info = AdditionalCharaMakerSlots[slotindex];
                if (!info.AccessorySlot)
                {
                    var index = slotindex + 20;
                    var custombase = CustomBase.instance;
                    var newSlot = Object.Instantiate(ScrollView.content.GetChild(0), ScrollView.content);

                    info.AccessorySlot = newSlot.gameObject;
                    var toggle = newSlot.GetComponent<Toggle>();
                    toggle.Set(false);

                    var canvasGroup = toggle.transform.GetChild(1).GetComponentInChildren<CanvasGroup>();
                    var cvsAccessory = toggle.GetComponentInChildren<CvsAccessory>();

                    cvsAccessory.textSlotName = toggle.GetComponentInChildren<TextMeshProUGUI>();

                    CvsAccessoryArray = CvsAccessoryArray.Concat(cvsAccessory).ToArray();

                    cvsAccessory.colorKind = cvscolor;
                    foreach (var item in CvsAccessoryArray)
                    {
                        item.colorKind = cvscolor;
                    }

                    var trans = canvasGroup.transform;
                    trans.position = new Vector3(trans.position.x, _slotUIPositionY);
                    var itemInfo = new UI_ToggleGroupCtrl.ItemInfo() { tglItem = toggle, cgItem = canvasGroup };
#if KK || KKS
                    AccessoryTab.items = AccessoryTab.items.ConcatNearEnd(itemInfo);
#elif EC
                    AccessoryTab.items = AccessoryTab.items.ConcatNearEnd(itemInfo, 1);
#endif
                    foreach (var _custom in SelectKind)
                    {
                        _custom.cvsAccessory = CvsAccessoryArray;
                    }

                    foreach (var _custom in MoveWin)
                    {
                        _custom.cvsAccessory = CvsAccessoryArray;
                    }

                    ParentWin.cvsAccessory = CvsAccessoryArray;

                    canvasGroup.Enable(false, false);

                    cvsAccessory.textSlotName.text = $"スロット{index + 1:00}";
                    cvsAccessory.slotNo = (CvsAccessory.AcsSlotNo)index;
                    cvsAccessory.CalculateUI(); //fixes copying data over from original slot
                    Plugin.ExecuteDelayed(cvsAccessory.CalculateUI); //fixes copying data over from original slot
                    newSlot.name = "tglSlot" + (index + 1).ToString("00");

                    custombase.actUpdateCvsAccessory = custombase.actUpdateCvsAccessory.Concat(new System.Action(cvsAccessory.UpdateCustomUI)).ToArray();
                    custombase.actUpdateAcsSlotName =
                        custombase.actUpdateAcsSlotName.Concat(new System.Action(delegate() { Plugin.ExecuteDelayed(cvsAccessory.UpdateSlotName); })).ToArray(); //delay to avoid an error when called early due to additional patches
                    var newreactive = new BoolReactiveProperty(false);
                    custombase._updateCvsAccessory = custombase._updateCvsAccessory.Concat(newreactive).ToArray();
                    AccessoryTab.textSlotNames = AccessoryTab.textSlotNames.Concat(cvsAccessory.textSlotName).ToArray();
                    newreactive.Subscribe(delegate(bool f)
                    {
                        if (index == custombase.selectSlot)
                        {
                            custombase.actUpdateCvsAccessory[index]?.Invoke();
                        }

                        custombase.actUpdateAcsSlotName[index]?.Invoke();
                        custombase._updateCvsAccessory[index].Value = false;
                    });

                    RestoreToggle(itemInfo, index);

                    _addButtonsGroup.SetAsLastSibling();
                    var action = new System.Action(delegate() { cvsAccessory.Start(); });

                    //Plugin.ExecuteDelayed(action);
                    try
                    {
                        Plugin.NewSlotAdded(index, newSlot.transform);
                    }
                    catch (System.Exception ex)
                    {
                        MoreAccessories.Print(ex.ToString(), BepInEx.Logging.LogLevel.Error);
                    }
                }

                var show = slotindex < ShowSlot - 20;
                info.AccessorySlot.SetActive(show);

                if (show)
                {
                    if (slotindex + 20 == CustomBase.Instance.selectSlot)
                        Plugin.ExecuteDelayed(() => info.AccessorySlot.GetComponentInChildren<CvsAccessory>().UpdateCustomUI());
                    CvsAccessoryArray[slotindex + 20].UpdateSlotName();
                }

                if (info.transferSlotObject) info.transferSlotObject.SetActive(show);
#if KK || KKS
                if (info.copySlotObject) info.copySlotObject.SetActive(true);
#endif
            }

            MoreAccessories.MakerMode.ValidatateToggles();

            for (; slotindex < AdditionalCharaMakerSlots.Count; slotindex++)
            {
                var slot = AdditionalCharaMakerSlots[slotindex];
                if (slot.AccessorySlot) slot.AccessorySlot.SetActive(false);
                if (slot.transferSlotObject) slot.transferSlotObject.SetActive(false);
#if KK || KKS
                if (slot.copySlotObject) slot.copySlotObject.SetActive(false);
#endif
            }

            _addButtonsGroup.SetAsLastSibling();
            FixWindowScroll();
        }

        private int[,] CVSColor(int rank)
        {
            var newarray = new int[rank, 4];
            var value = 124;
            for (var i = 0; i < 20; i++)
            {
                for (var j = 0; j < 4; j++, value++)
                {
                    newarray[i, j] = value;
                }
            }

            //there is a break here with KKS since they appended to end of enum
            value = 5000;
            for (var i = 20; i < rank; i++)
            {
                for (var j = 0; j < 4; j++, value++)
                {
                    newarray[i, j] = value;
                }
            }

            return newarray;
        }

        private void RestoreToggle(UI_ToggleGroupCtrl.ItemInfo toggleGroup, int index)
        {
            toggleGroup.tglItem.onValueChanged.RemoveAllListeners();
            toggleGroup.tglItem.Set(false, false);
            toggleGroup.tglItem.OnValueChangedAsObservable().Subscribe(toggleEnabled =>
            {
                if (!toggleEnabled) return;
                FixWindowScroll(toggleGroup.cgItem);
                foreach (var accessoryTabItem in AccessoryTab.items)
                {
                    accessoryTabItem.cgItem.Enable(accessoryTabItem.Equals(toggleGroup) && accessoryTabItem.tglItem.isOn);
                }

                if (toggleGroup.Equals(AccessoryTab.items[AccessoryTab.items.Length - 1]))
                {
                    AccessoryTab.CloseWindow();
                    Singleton<CustomBase>.Instance.updateCvsAccessoryChange = true;
                    AccessoryTab.backIndex = index;
                    return;
                }
#if KK || KKS
                if (toggleGroup.Equals(AccessoryTab.items[AccessoryTab.items.Length - 2]))
                {
                    AccessoryTab.CloseWindow();
                    Singleton<CustomBase>.Instance.updateCvsAccessoryCopy = true;
                    AccessoryTab.backIndex = index;
                    return;
                }
#endif
                if (CustomBase.instance.chaCtrl == null) return;

                var open = 120 != CustomBase.instance.chaCtrl.nowCoordinate.accessory.parts[index].type;
                ParentWin.ChangeSlot(index, open);
                foreach (var customAcsMoveWindow in AccessoryTab.customAcsMoveWin)
                {
                    if (customAcsMoveWindow)
                        customAcsMoveWindow.ChangeSlot(index, open);
                }

                foreach (var customAcsSelectKind in AccessoryTab.customAcsSelectKind)
                {
                    if (customAcsSelectKind)
                        customAcsSelectKind.ChangeSlot(index, open);
                }

                Singleton<CustomBase>.Instance.selectSlot = index;
                Singleton<CustomBase>.Instance.SetUpdateCvsAccessory(index, true);
                if (AccessoryTab.backIndex != index)
                {
                    AccessoryTab.ChangeColorWindow(index);
                }

                AccessoryTab.backIndex = index;
            });
        }

        public static void AddSlot(int num)
        {
            if (AddInProgress || !CustomBase.instance) return; //stop multiclick or repeatedly triggering this while in progress just in case
            AddInProgress = true;
            var controller = CustomBase.instance.chaCtrl;
            var nowparts = controller.nowCoordinate.accessory.parts;
#if KK || KKS
            var coordacc = controller.chaFile.coordinate[controller.chaFile.status.coordinateType].accessory;
#else
            var coordacc = controller.chaFile.coordinate.accessory;
#endif
            var delta = num + ShowSlot - nowparts.Length;
            if (delta > 0)
            {
                var newpart = new ChaFileAccessory.PartsInfo[delta];
                for (var i = 0; i < delta; i++)
                {
                    newpart[i] = new ChaFileAccessory.PartsInfo();
                }

                coordacc.parts = controller.nowCoordinate.accessory.parts = nowparts.Concat(newpart).ToArray();
            }

            ShowSlot += num;
            MoreAccessories.ArraySync(controller);
            AddInProgress = false;
        }

        private void InitilaizeSlotNames()
        {
            foreach (var item in CvsAccessoryArray)
            {
                item.UpdateSlotName();
            }
        }
    }
}