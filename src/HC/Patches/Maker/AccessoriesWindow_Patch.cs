using System.Diagnostics.CodeAnalysis;
using CharacterCreation;
using CharacterCreation.UI;
using HarmonyLib;

namespace HC.Maker;

public class AccessoriesWindow_Patch
{
    public static class CustomAcs_Patches
    {
        #region CustomAcsChangeSlot

        [HarmonyPatch(typeof(Accessory_00), nameof(Accessory_00.Awake))]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Harmony Patches - Used Externally")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Harmony Patches - Used Externally")]
        private static class CustomAcsChangeSlotStart_Patches
        {
            private static void Postfix(Accessory_00 __instance)
            {
                MoreAccessories.MakerMode.AccessoriesWindow = new Accessories(__instance);
            }
        }
        #endregion
    }
}