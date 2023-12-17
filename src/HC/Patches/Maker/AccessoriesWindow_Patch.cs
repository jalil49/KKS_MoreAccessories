using System.Diagnostics.CodeAnalysis;
using CharacterCreation;
using CharacterCreation.UI;
using HarmonyLib;
using ILLGames.Unity;
using UniRx;

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
                MoreAccessoriesLoader.ManualLogSource.LogMessage($"Makermode null {MoreAccessories.MakerMode == null}");
                MoreAccessories.MakerMode!.AccessoryWindow = new AccessoryWindow(__instance);

                foreach (var toggle in __instance._acsGroupEdit._tglAcsGroup)
                {
                    toggle.OnValueChangedAsObservable().Subscribe((Il2CppSystem.Action<bool>)Functions);
                    continue;

                    void Functions(bool isOn)
                    {
                        MoreAccessories.Print("Accessory_00 toggle");
                    }
                }

                for (int i = 0; i < 20; i++)
                {
                    __instance._kindToggles[i].Toggle.OnValueChangedAsObservable().Subscribe((Il2CppSystem.Action<bool>)Functions);
                    continue;

                    void Functions(bool isOn)
                    {
                        MoreAccessories.Print("Accessory_00 _Kind toggle");
                    }
                }

                foreach (var instanceTitleLabel in __instance._titleLabels)
                {
                    MoreAccessories.Print(instanceTitleLabel);
                }
                
                foreach (var instanceEditLabel in __instance._editLabels)
                {
                    MoreAccessories.Print(instanceEditLabel);
                }
            }
        }

        #endregion
    }
}