using System.Diagnostics.CodeAnalysis;
using Character;
using HarmonyLib;

namespace HC;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public class Human_Patches
{
    [HarmonyPatch(typeof(Human), nameof(Human.LoadAccessory))]
    public static class LoadAccessory_Patch
    {
        private static void LogMessage(string message)
        {
            MoreAccessoriesLoader.ManualLogSource.LogMessage(message);
        }
        private static void Prefix(Human __instance)
        {
            LogMessage("LoadAccessory_Patch Syncing nowAccessories.parts to accessory objects");
            var acs = __instance.acs;
            var partsLength = acs.nowCoordinate.Accessory.parts.Length;
            var accessoryLength = acs._accessories_k__BackingField.Length;
            if (partsLength < accessoryLength)
            {
                acs._accessories_k__BackingField = acs._accessories_k__BackingField.Take(partsLength).ToArray();
                return;
            }

            if (partsLength > accessoryLength)
            {
                var accessories = new HumanAccessory.Accessory[partsLength - accessoryLength];
                acs._accessories_k__BackingField = acs._accessories_k__BackingField.Concat(accessories).ToArray();
            }
        }
    }
}