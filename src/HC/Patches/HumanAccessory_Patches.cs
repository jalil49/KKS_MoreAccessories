using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Character;
using HarmonyLib;

namespace HC;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
public static class HumanAccessory_Patches
{
    private static void LogMessage(string message)
    {
        MoreAccessoriesLoader.ManualLogSource.LogMessage(message);
    }

    [HarmonyPatch(typeof(HumanAccessory), nameof(HumanAccessory.ChangeAccessory), typeof(bool))]
    private static class ChangeAccessory_Patch
    {
        private static void Prefix(HumanAccessory __instance)
        {
            LogMessage("ChangeAccessory_Patch Syncing nowAccessories.parts to accessory objects");
            var partsLength = __instance.nowCoordinate.Accessory.parts.Length;
            var accessoryLength = __instance._accessories_k__BackingField.Length;
            if (partsLength < accessoryLength)
            {
                __instance._accessories_k__BackingField = __instance._accessories_k__BackingField.Take(partsLength).ToArray();
                return;
            }

            if (partsLength > accessoryLength)
            {
                var accessories = new HumanAccessory.Accessory[partsLength - accessoryLength];
                __instance._accessories_k__BackingField = __instance._accessories_k__BackingField.Concat(accessories).ToArray();
            }
        }

        private static void Postfix(HumanAccessory __instance, bool forceChange)
        {
            LogMessage("Changing Accessories after 20");
            var parts = __instance.nowCoordinate.Accessory.parts;
            var accessoriesLength = __instance.accessories.Length;
            for (var i = 20; i < accessoriesLength; i++)
            {
                var partsInfo = parts[i];
                __instance.ChangeAccessory(slotNo: i, type: partsInfo.type, id: partsInfo.id, parentKey: (ChaAccessoryDefine.AccessoryParentKey)partsInfo.parentKeyType, forceChange: forceChange);
            }
        }
    }

    [HarmonyPatch(typeof(HumanAccessory), nameof(HumanAccessory.SetAccessoryStateAll))]
    private static class SetAccessoryStateAll_Patch
    {
        private static void Postfix(HumanAccessory __instance, bool show)
        {
            LogMessage("SetAccessoryState after 20");
            var accessoriesLength = __instance.accessories.Length;
            for (var i = 20; i < accessoriesLength; i++)
            {
                __instance.SetAccessoryState(slotNo: i, show);
            }
        }
    }

    [HarmonyPatch(typeof(HumanAccessory), nameof(HumanAccessory.SetAccessoryStateCategory))]
    private static class SetAccessoryStateCategory_Patch
    {
        private static void Postfix(HumanAccessory __instance, bool show)
        {
            LogMessage("SetAccessoryStateCategory after 20");
            var accessoriesLength = __instance.accessories.Length;
            for (var i = 20; i < accessoriesLength; i++)
            {
                __instance.SetAccessoryState(slotNo: i, show);
            }
        }
    }

    [HarmonyPatch(typeof(HumanAccessory), nameof(HumanAccessory.UpdateAccessoryMoveAllFromInfo))]
    private static class UpdateAccessoryMoveAllFromInfo_Patch
    {
        private static void Postfix(HumanAccessory __instance)
        {
            LogMessage("HumanAccessory.UpdateAccessoryMoveAllFromInfo_Patch: Unknown Use");
        }
    }

    [HarmonyPatch(typeof(HumanAccessory), nameof(HumanAccessory.CheckHideHair))]
    private static class CheckHideHair_Patch
    {
        private static void Postfix(HumanAccessory __instance, ref bool __result)
        {
            LogMessage("CheckHideHair_Patch");
            __result = __instance.accessories.Any(x => x.hideHairAcs);
        }
    }

    [HarmonyPatch(typeof(HumanAccessory), nameof(HumanAccessory.SetupAccessoryFK), new Type[0])]
    private static class SetupAccessoryFk_Patch
    {
        private static void Postfix(HumanAccessory __instance)
        {
            LogMessage("SetupAccessoryFk_Patch after 20");
            var accessoriesLength = __instance.Accessories.Length;
            for (var slotNo = 20; slotNo < accessoriesLength; slotNo++)
            {
                __instance.SetupAccessoryFK(slotNo);
            }
        }
    }

    [HarmonyPatch(typeof(HumanAccessory), nameof(HumanAccessory.UpdateAccessoryFK), new Type[0])]
    private static class UpdateAccessoryFkNoArgs_Patch
    {
        private static void Postfix(HumanAccessory __instance)
        {
            LogMessage("UpdateAccessoryFkNoArgs_Patch after 20");
            var accessoriesLength = __instance.Accessories.Length;
            for (var slotNo = 20; slotNo < accessoriesLength; slotNo++)
            {
                __instance.UpdateAccessoryFK(slotNo);
            }
        }
    }

    [HarmonyPatch]
    private static class UpdateAccessoryFkIntVector3_Patch
    {
        private static MethodBase TargetMethod()
        {
            return typeof(HumanAccessory).GetMethods(AccessTools.all).First(x => x.Name.StartsWith("UpdateAccessoryFK") && x.GetParameters().Length == 2);
        }

        private static void Postfix(HumanAccessory __instance)
        {
            LogMessage("UpdateAccessoryFkIntVector3_Patch: Unknown Use");
        }
    }
}