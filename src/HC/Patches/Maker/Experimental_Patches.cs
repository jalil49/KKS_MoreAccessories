using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BepInEx.Logging;
using CharacterCreation;
using CharacterCreation.ConfirmDialog;
using CharacterCreation.UI;
using CharacterCreation.UI.View.Accessory;
using H;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using ILLGames.Unity;
using UniRx;
using UniRx.Operators;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using IntPtr = System.IntPtr;

namespace HC.Maker;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public static class Experimental_Patches
{
    [HarmonyPatch(typeof(BaseCategorySelectParts), nameof(BaseCategorySelectParts.Start))]
    private static class BaseCategorySelectParts_Patch
    {
        private static void Postfix(BaseCategorySelectParts __instance)
        {
            MoreAccessories.Print(GetGameObjectPath(__instance.gameObject));
            var i = 0;
            MoreAccessories.Print("BaseCategorySelectParts_Patch Patched:");
            foreach (var instanceToggle in __instance._toggles)
            {
                var index = i++;
                instanceToggle.OnValueChangedAsObservable().Subscribe((Il2CppSystem.Action<bool>)Function);
                continue;

                void Function(bool isOn)
                {
                    MoreAccessories.Print("BaseCategorySelectParts_Patch Toggle: " + index);
                }
            }

            foreach (var instanceToggle in __instance._toggleGroup.onList)
            {
                var index = i++;
                instanceToggle.OnValueChangedAsObservable().Subscribe((Il2CppSystem.Action<bool>)Function);
                continue;

                void Function(bool isOn)
                {
                    MoreAccessories.Print("BaseCategorySelectParts_Patch Toggle: " + index);
                }
            }
        }

        public static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }

            return path;
        }
    }

    [HarmonyPatch(typeof(CustomToggle), nameof(CustomToggle.CreateToggles), typeof(Il2CppStringArray))]
    private static class CreateToggles_Patch
    {
        private static void Postfix(Il2CppStringArray names)
        {
            foreach (var name in names)
            {
                MoreAccessories.Print("CustomToggle was added" + name);
            }
        }
    }

    [HarmonyPatch]
    private static class CreateToggles2_Patch
    {
        private static MethodBase TargetMethod()
        {
            return typeof(CustomToggle).GetMethods().First(x => x.Name.StartsWith(nameof(CustomToggle.CreateToggles)) && x.GetParameters().Length == 2);
        }

        private static void Postfix(Il2CppStringArray names)
        {
            foreach (var name in names)
            {
                MoreAccessories.Print("CustomToggle was added" + name + " with action");
            }
        }
    }

    [HarmonyPatch(typeof(AcsGroupEdit), nameof(AcsGroupEdit.Initialize))]
    public static class AcsGroupEdit_Patch
    {
        private static void Postfix(AcsGroupEdit __instance)
        {
            MoreAccessories.Print("ACS group edit");
            var i = 1;
            foreach (var toggle in __instance._toggleGroup.onList)
            {
                var index = i++;
                toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
                continue;

                void Function(bool isOn)
                {
                    MoreAccessories.Print("AcsGroupEdit Toggle: " + index);
                }
            }
        }
    }

    [HarmonyPatch(typeof(AcsGroupEdit), nameof(AcsGroupEdit.Refresh))]
    public static class AcsGroupEditRefresh_Patch
    {
        private static void Postfix(AcsGroupEdit __instance)
        {
            MoreAccessories.Print("ACS group Refresh");
            var i = 1;
            foreach (var toggle in __instance._toggleGroup.onList)
            {
                var index = i++;
                toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
                continue;

                void Function(bool isOn)
                {
                    MoreAccessories.Print("AcsGroupEdit Toggle: " + index);
                }
            }
        }
    }

    [HarmonyPatch(typeof(AcsEdit), nameof(AcsEdit.Initialize))]
    public static class AcsEdit_Patch
    {
        private static void Postfix(AcsEdit __instance)
        {
            MoreAccessories.Print("AcsEdit Init", LogLevel.Message);
            __instance.SetTitle("AcsEdit", "AcsEdit");
        }
    }

    // [HarmonyPatch(typeof(Utils.UI.TagSelection.ToggleGroup), nameof(Utils.UI.TagSelection.ToggleGroup.Refresh))]
    // public static class ToggleGroup_Patch
    // {
    //     private static void Postfix(Utils.UI.TagSelection.ToggleGroup __instance)
    //     {
    //         MoreAccessories.Print("Utils.UI.TagSelection.ToggleGroup constructor", LogLevel.Message);
    //         var i = 1;
    //         foreach (var toggle in __instance.onList)
    //         {
    //             var index = i++;
    //             toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
    //             continue;
    //
    //             void Function(bool isOn)
    //             {
    //                 MoreAccessories.Print("Utils.UI.TagSelection.ToggleGroup Toggle: " + index);
    //             }
    //         }
    //     }
    // }

    [HarmonyPatch(typeof(ImageCustom), nameof(ImageCustom.Awake))]
    public static class ImageCustom_Patch
    {
        private static ImageCustom test;

        private static void Postfix(ImageCustom __instance)
        {
            MoreAccessories.Print("ImageCustom constructor", LogLevel.Message);
            var i = 1;
            foreach (var toggle in __instance._accessoryToggles)
            {
                var index = i++;
                toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
                continue;

                void Function(bool isOn)
                {
                    MoreAccessories.Print("ImageCustom Toggle: " + index);
                }
            }
        }

        public static void Manual()
        {
            MoreAccessories.Print("ImageCustom Manual", LogLevel.Message);
            var i = 1;
            foreach (var toggle in test._accessoryToggles)
            {
                var index = i++;
                toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
                continue;

                void Function(bool isOn)
                {
                    MoreAccessories.Print("ImageCustom Toggle: " + index);
                }
            }
        }
    }
    //
    // [HarmonyPatch]
    // public static class ToggleGroup2_Patch
    // {
    //     private static MethodBase TargetMethod()
    //     {
    //         foreach (var constructorInfo in typeof(Utils.UI.TagSelection.ToggleGroup).GetConstructors())
    //         {
    //             MoreAccessories.Print(constructorInfo.FullDescription());
    //         }
    //
    //         return typeof(Utils.UI.TagSelection.ToggleGroup).GetConstructor(new[] { typeof(int), typeof(bool), typeof(Il2CppSystem.Collections.Generic.IReadOnlyList<>).MakeGenericType(typeof(Toggle)) })!;
    //     }
    //
    //     private static void Postfix(Utils.UI.TagSelection.ToggleGroup __instance)
    //     {
    //         MoreAccessories.Print("Utils.UI.TagSelection.ToggleGroup constructor2 ", LogLevel.Message);
    //         var i = 1;
    //         foreach (var toggle in __instance.onList)
    //         {
    //             var index = i++;
    //             toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
    //             continue;
    //
    //             void Function(bool isOn)
    //             {
    //                 MoreAccessories.Print("Utils.UI.TagSelection.ToggleGroup 2 Toggle: " + index);
    //             }
    //         }
    //     }
    // }

    // [HarmonyPatch(typeof(CustomToggle.TogglePool), nameof(CustomToggle.TogglePool.s()))]
    // public static class TogglePool_Patch
    // {
    //     private static void Postfix(CustomToggle.TogglePool __instance)
    //     {
    //         MoreAccessories.Print("TogglePool constructor ", LogLevel.Message);
    //         var i = 1;
    //         foreach (var toggle in __instance.List)
    //         {
    //             var index = i++;
    //             toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
    //             continue;
    //
    //             void Function(bool isOn)
    //             {
    //                 MoreAccessories.Print("TogglePool : " + index);
    //             }
    //         }
    //     }
    // }

    [HarmonyPatch(typeof(CategoryKindButton), nameof(CategoryKindButton.Start))]
    public static class CategoryKindButton_Patch
    {
        private static void Postfix(CategoryKindButton __instance)
        {
            MoreAccessories.Print("TogglePool constructor ", LogLevel.Message);

            __instance._button.onClick.AddListener((UnityAction)Function);

            void Function()
            {
                MoreAccessories.Print($"CategoryKindButton : {__instance.name} {__instance._title} {__instance.transform.name}");
            }
        }
    }

    /*[HarmonyPatch(typeof(CategoryEdit.CategoryKindTogglePool), nameof(CategoryEdit.CategoryKindTogglePool.Awake))]
    public static class CategoryKindTogglePool_Patch
    {
        private static void Postfix(CategoryEdit.CategoryKindTogglePool __instance)
        {
            MoreAccessories.Print("CategoryKindTogglePool patch", LogLevel.Message);

            var i = 1;
            foreach (var toggle in __instance.List)
            {
                var index = i++;
                toggle.Toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
                continue;

                void Function(bool isOn)
                {
                    MoreAccessories.Print("CategoryKindTogglePool : " + index);
                }
            }
        }
    }*/

    // [HarmonyPatch(typeof(CategoryEdit.CustomTogglePool), nameof(CategoryEdit.CustomTogglePool)))]
    // public static class CustomTogglePool_Patch
    // {
    //     private static void Postfix(CategoryEdit.CustomTogglePool __instance)
    //     {
    //         MoreAccessories.Print("CustomTogglePool patch", LogLevel.Message);
    //
    //         var i = 1;
    //         foreach (var toggle in __instance.List)
    //         {
    //             var index = i++;
    //             toggle.Toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
    //             continue;
    //
    //             void Function(bool isOn)
    //             {
    //                 MoreAccessories.Print("CustomTogglePool : " + index);
    //             }
    //         }
    //     }
    // }

    [HarmonyPatch(typeof(UI_ToggleButton), nameof(UI_ToggleButton.Start))]
    public static class UI_ToggleButton_Patch
    {
        private static void Postfix(UI_ToggleButton __instance)
        {
            MoreAccessories.Print("UI_ToggleButton patch", LogLevel.Message);


            __instance.OnValueChangedAsObservable().Subscribe((Il2CppSystem.Action<bool>)Function);
            return;

            void Function(bool isOn)
            {
                MoreAccessories.Print($"UI_ToggleButton : {__instance.name} {(__instance.transform.parent != null ? __instance.transform.parent.name : null)}");
            }
        }
    }

    [HarmonyPatch(typeof(UI_ToggleGroupCtrl), nameof(UI_ToggleGroupCtrl.Start))]
    public static class UI_ToggleGroupCtrln_Patch
    {
        private static void Postfix(UI_ToggleGroupCtrl __instance)
        {
            MoreAccessories.Print("UI_ToggleGroupCtrl patch", LogLevel.Message);
            foreach (var instanceItem in __instance.Items)
            {
                instanceItem.TglItem.onValueChanged.AddListener((UnityAction<bool>)Function);
            }

            return;

            void Function(bool isOn)
            {
                MoreAccessories.Print($"UI_ToggleGroupCtrl : {__instance.name} {(__instance.transform.parent != null ? __instance.transform.parent.name : null)}");
            }
        }
    }

    [HarmonyPatch(typeof(AccessoryParentWindow), nameof(AccessoryParentWindow.Awake))]
    public static class AccessoryParentWindow_Patch
    {
        private static void Postfix(AccessoryParentWindow __instance)
        {
            MoreAccessories.Print("AccessoryParentWindow patch", LogLevel.Message);

            foreach (var instanceToggle in __instance._toggles)
            {
                instanceToggle.onValueChanged.AddListener((UnityAction<bool>)Function);
                return;

                void Function(bool isOn)
                {
                    MoreAccessories.Print($"AccessoryParentWindow : {__instance.name} {(__instance.transform.parent != null ? __instance.transform.parent.name : null)}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(CategorySelection), nameof(CategorySelection.Initialize))]
    public static class CategorySelection_Patch
    {
        private static void Postfix(CategorySelection __instance)
        {
            MoreAccessories.Print("CategorySelection patch", LogLevel.Message);


            __instance.Toggle.onValueChanged.AddListener((UnityAction<bool>)ToggleButton);

            foreach (var toggle in __instance._toggleGroup.onList)
            {
                toggle.onValueChanged.AddListener((UnityAction<bool>)GroupToggleButton);
            }
        }

        private static void ToggleButton(bool _)
        {
            MoreAccessories.Print("CategorySelection Button", LogLevel.Message);
        }

        private static void GroupToggleButton(bool _)
        {
            MoreAccessories.Print("CategorySelection GroupToggleButton", LogLevel.Message);
        }
    }

    [HarmonyPatch(typeof(HumanCustom), nameof(HumanCustom.Start))]
    public static class HumanCustom_Patch
    {
        private static void Postfix(HumanCustom __instance)
        {
            MoreAccessories.Print("HumanCustom patch", LogLevel.Message);
            foreach (var tmpInputField in __instance._inputFieldList)
            {
                MoreAccessories.Print($"{tmpInputField.placeholder}\t{tmpInputField.text}\t{tmpInputField.name} ", LogLevel.Message);
            }

            foreach (var instanceUIBundle in __instance._uiBundles)
            {
                MoreAccessories.Print($"UI Bundle {instanceUIBundle}", LogLevel.Message);
            }
        }
    }

    // [HarmonyPatch(typeof(HumanCustom), nameof(HumanCustom.Awake))]
    // public static class HumanCustomAwake_Patch
    // {
    //     private static void Postfix(HumanCustom __instance)
    //     {
    //         MoreAccessories.Print("HumanCustomAwake patch", LogLevel.Message);
    //     }
    // }
    // [HarmonyPatch(typeof(CharaCategorySelectParts), nameof(CharaCategorySelectParts.Start))]
    // public static class CharaCategorySelectParts_Patch
    // {
    //     private static void Prepare()
    //     {
    //         foreach (var methodInfo in typeof(CharaCategorySelectParts).GetMethods())
    //         {
    //             MoreAccessories.Print(methodInfo.FullDescription());
    //         }
    //     }
    //     
    //     private static void Postfix(CharaCategorySelectParts __instance)
    //     {
    //         MoreAccessories.Print("CharaCategorySelectParts patch", LogLevel.Message);
    //     }
    // }

    [HarmonyPatch(typeof(AcsCategorySelectEdit), nameof(AcsCategorySelectEdit.Initialize))]
    public static class AcsCategorySelectEdit_Patch
    {
        private static void Postfix(AcsCategorySelectEdit __instance)
        {
            MoreAccessories.Print("AcsCategorySelectEdit patch", LogLevel.Message);
        }
    }

    // [HarmonyPatch(typeof(ImageCustom_ToggleCheck), nameof(ImageCustom_ToggleCheck.Refresh))]
    // public static class ImageCustom_ToggleCheck_Patch
    // {
    //     private static void Postfix(ImageCustom_ToggleCheck __instance)
    //     {
    //         MoreAccessories.Print("ImageCustom_ToggleCheck patch", LogLevel.Message);
    //     }
    // }

    [HarmonyPatch(typeof(ImageCustom), nameof(ImageCustom.InitializeCheckMark))]
    public static class ImageCustomInitializeCheckMark_Patch
    {
        private static void Postfix(ImageCustom __instance)
        {
            MoreAccessories.Print("InitializeCheckMark patch", LogLevel.Message);
            var i = 1;
            foreach (var toggle in __instance._accessoryToggles)
            {
                var index = i++;
                toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
                continue;

                void Function(bool isOn)
                {
                    MoreAccessories.Print("InitializeCheckMark Toggle: " + index);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ImageCustom), nameof(ImageCustom.InitializeKindGroup))]
    public static class ImageCustomInitializeKindGroup_Patch
    {
        private static void Postfix(ImageCustom __instance)
        {
            MoreAccessories.Print("InitializeInitializeKindGroup patch", LogLevel.Message);
            var i = 1;
            foreach (var toggle in __instance._accessoryToggles)
            {
                var index = i++;
                toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
                continue;

                void Function(bool isOn)
                {
                    MoreAccessories.Print("InitializeInitializeKindGroup Toggle: " + index);
                }
            }
        }
    }


    [HarmonyPatch(typeof(ImageCustom), nameof(ImageCustom.InitializeKindImages))]
    public static class ImageCustomInitializeKindImages_Patch
    {
        private static void Postfix(ImageCustom __instance)
        {
            MoreAccessories.Print("InitializeInitializeInitializeKindImages  patch", LogLevel.Message);
            var i = 1;
            foreach (var toggle in __instance._accessoryToggles)
            {
                var index = i++;
                toggle.onValueChanged.AddListener((UnityAction<bool>)Function);
                continue;

                void Function(bool isOn)
                {
                    MoreAccessories.Print("ImageCustomInitializeKindImages_Patch Toggle: " + index);
                }
            }
        }
    }
}