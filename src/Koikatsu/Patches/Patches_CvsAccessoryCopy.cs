#if KK || KKS
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ChaCustom;
using HarmonyLib;
using TMPro;

namespace MoreAccessoriesKOI.Patches.Maker
{
    [HarmonyPatch(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.Start))]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Harmony Patches - Used Externally")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Harmony Patches - Used Externally")]
    internal static class CvsAccessoryCopyStart_Patches
    {
        private static void Postfix(CvsAccessoryCopy __instance)
        {
            MoreAccessories.MakerMode.CopyWindow ??= new Copy_Window(__instance);
        }
    }
    
    [HarmonyPatch]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Harmony Patches - Used Externally")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Harmony Patches - Used Externally")]
    internal static class CvsAccessoryCopyDd_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var list = new List<MethodBase>
            {
                AccessTools.Method(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.ChangeDstDD)),
                AccessTools.Method(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.ChangeSrcDD))
            };
            return list;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = instructions.ToList();

            foreach (var inst in instructionsList)
            {
                if (inst.opcode == OpCodes.Ldc_I4_S && inst.operand.ToString() == "20")
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0); //accessory load
                    yield return new CodeInstruction(OpCodes.Call, typeof(CvsAccessoryCopyDd_Patch).GetMethod(nameof(AccessoryCount), AccessTools.all));
                    continue;
                }

                yield return inst;
            }
        }

        private static int AccessoryCount(ChaFileAccessory chaFileAccessory)
        {
            if (CustomBase.instance.chaCtrl == null || MoreAccessories.MakerMode == null || MoreAccessories.MakerMode.AccessoriesWindow == null) return 20;
            return chaFileAccessory.parts.Length;
        }
    }

    [HarmonyPatch]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Harmony Patches - Used Externally")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Harmony Patches - Used Externally")]
    internal static class CvsAccessoryCopyAcs_Patch
    {
        private static void Postfix(CvsAccessoryCopy __instance, TMP_Dropdown[] ___ddCoordeType)
        {
            MoreAccessories.MakerMode.UpdateMakerUI();
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            var list = new List<MethodBase>
            {
                AccessTools.Method(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.CopyAcs))
            };

            return list;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = instructions.ToList();

            foreach (var inst in instructionsList)
            {
                if (inst.opcode == OpCodes.Ldc_I4_S && inst.operand.ToString() == "20")
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1); //accessory2 load
                    yield return new CodeInstruction(OpCodes.Ldloc_0); //accessory1 load
                    yield return new CodeInstruction(OpCodes.Call, typeof(CvsAccessoryCopyAcs_Patch).GetMethod(nameof(AccessoryCount), AccessTools.all));
                    continue;
                }

                yield return inst;
            }
        }


        private static int AccessoryCount(ChaFileAccessory source, ChaFileAccessory destination)
        {
            if (CustomBase.instance.chaCtrl == null || MoreAccessories.MakerMode == null || MoreAccessories.MakerMode.AccessoriesWindow == null) return 20;
            return Math.Max(source.parts.Length, destination.parts.Length);
        }
    }

    [HarmonyPatch(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.Start))]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Harmony Patches - Used Externally")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Harmony Patches - Used Externally")]
    internal static class CvsAccessoryCopyStart_Patch
    {
        private static void Postfix(CvsAccessoryCopy __instance)
        {
            foreach (var tmpDropdown in __instance.ddCoordeType)
            {
                tmpDropdown.onValueChanged.AddListener(x => MoreAccessories.ArraySync(CustomBase.instance.chaCtrl));
            }
        }
    }
}
#endif