using System.Diagnostics.CodeAnalysis;
using ChaCustom;
using HarmonyLib;

namespace MoreAccessoriesKOI.Patches.Maker
{
    public static class CustomAcs_Patches
    {
        #region CustomAcsChangeSlot

        [HarmonyPatch(typeof(CustomAcsChangeSlot), nameof(CustomAcsChangeSlot.Start))]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Harmony Patches - Used Externally")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Harmony Patches - Used Externally")]
        private static class CustomAcsChangeSlotStart_Patch
        {
            private static void Postfix(CustomAcsChangeSlot __instance)
            {
                MoreAccessories.MakerMode.AccessoriesWindow = new Accessories(__instance);
            }
        }
        #endregion
    }
}
