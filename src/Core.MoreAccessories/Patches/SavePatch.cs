using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MoreAccessoriesKOI.Patches
{
#if KK || KKS
    /// <summary>
    /// Trim slot length to last slot that is not empty or 20 length
    /// </summary>
    [HarmonyPatch(typeof(ChaFile), nameof(ChaFile.GetCoordinateBytes), new Type[0])]
    internal class CharaSavePatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(ChaFile __instance)
        {
            try
            {
                var coord = __instance.coordinate;
                for (int outfitNum = 0, n = coord.Length; outfitNum < n; outfitNum++)
                {
                    if (coord[outfitNum].accessory.parts.Length == 20) continue; //array is natural size don't do extra work

                    var lastValidSlot = Math.Max(20, Array.FindLastIndex(coord[outfitNum].accessory.parts, x => x.type != 120) + 1);

                    coord[outfitNum].accessory.parts = coord[outfitNum].accessory.parts.Take(lastValidSlot).ToArray();
                }
            }
            catch (Exception ex)
            {
                MoreAccessories.Print($"Error occurred while saving chafile coordinates {ex}", LogLevel.Fatal);
            }
        }

        [HarmonyPriority(Priority.First)]
        private static void Postfix(ChaFile __instance)
        {
            ChaControl first = null;
            foreach (var x in Common_Patches.GetChaControls())
            {
                if (__instance == x.chaFile)
                {
                    first = x;
                    break;
                }
            }

            MoreAccessories.NowCoordinateTrimAndSync(first);
        }
    }
#else
    [HarmonyPatch(typeof(ChaFile), nameof(ChaFile.GetCoordinateBytes), new Type[0])]
    internal class SavePatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(ChaFile __instance)
        {
            try
            {
                var accessory = __instance.coordinate.accessory;
                if (accessory.parts.Length == 20) //Don't do extra work
                {
                    return;
                }

                var lastValidSlot =Array.FindLastIndex(accessory.parts, x => x.type != 120);
                if (lastValidSlot == accessory.parts.Length) return; //don't do below since nothing changed
                accessory.parts = accessory.parts.Take(lastValidSlot).ToArray();
            }
            catch (Exception ex)
            {
                MoreAccessories.Print($"Error occurred while saving chafile coordinates {ex}", LogLevel.Fatal);
            }
        }

        [HarmonyPriority(Priority.First)]
        private static void Postfix(ChaFile __instance)
        {
            ChaControl first = null;
            foreach (var x in Common_Patches.GetChaControls())
            {
                if (__instance == x.chaFile)
                {
                    first = x;
                    break;
                }
            }

            MoreAccessories.NowCoordinateTrimAndSync(first);
        }
    }

#endif
    [HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile))]
    internal class CoordSavePatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(ChaFileCoordinate __instance)
        {
            try
            {
                if (__instance.accessory.parts.Length == 20) //Don't do extra work
                {
                    return;
                }


                var lastValidSlot = Math.Max(20, Array.FindLastIndex(__instance.accessory.parts, x => x.type != 120) + 1);
                if (lastValidSlot == __instance.accessory.parts.Length) return; //don't do below since nothing changed
                __instance.accessory.parts = __instance.accessory.parts.Take(lastValidSlot).ToArray();
            }
            catch (Exception ex)
            {
                MoreAccessories.Print($"Error occurred while saving coordinate {ex}", LogLevel.Fatal);
            }
        }

        [HarmonyPriority(Priority.First)]
        private static void Postfix(ChaFileCoordinate __instance)
        {
            ChaControl first = null;
            foreach (var x in Common_Patches.GetChaControls())
            {
                if (__instance == x.nowCoordinate)
                {
                    first = x;
                    break;
                }
            }

            MoreAccessories.NowCoordinateTrimAndSync(first);
        }
    }

    //Don't save with manipulated showAccessory array. Will cause issues when plugin is not installed or when used with moreaccessories by joan
    [HarmonyPatch(typeof(ChaFile), nameof(ChaFile.GetStatusBytes), typeof(ChaFileStatus))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class ChaFileStatusPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(ChaFileStatus _status, out bool[] __state)
        {
            Common_Patches.Seal(false);
            try
            {
                __state = _status.showAccessory;
                _status.showAccessory = __state.Take(20).ToArray();
            }
            catch (Exception ex)
            {
                MoreAccessories.Print($"Error occurred while saving chafile coordinates {ex}", LogLevel.Fatal);
                __state = null;
            }
        }

        [HarmonyPriority(Priority.First)]
        private static void Postfix(ChaFile __instance, ChaFileStatus _status, bool[] __state)
        {
            if (__state != null)
            {
                _status.showAccessory = __state;
            }

            ChaControl first = null;
            foreach (var x in Common_Patches.GetChaControls())
            {
                if (__instance == x.chaFile)
                {
                    first = x;
                    break;
                }
            }

            MoreAccessories.NowCoordinateTrimAndSync(first);
            Common_Patches.Seal(true);
        }
    }
}