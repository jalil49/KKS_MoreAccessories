#if PATCH_BUGGED
using System.Diagnostics.CodeAnalysis;
using Character;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MessagePack.Formatters.Character;

namespace HC;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public static class Formatter_Patches
{
    private static void LogMessage(string message)
    {
        MoreAccessoriesLoader.ManualLogSource.LogMessage(message);
    }

    [HarmonyPatch(typeof(HumanDataAccessoryFormatter), nameof(HumanDataAccessoryFormatter.Serialize))]
    private static class HumanDataAccessoryFormatter_Patch
    {
        private static void Prefix(HumanDataAccessory value, out Il2CppReferenceArray<HumanDataAccessory.PartsInfo>? __state)
        {
             LogMessage("Pre Fix Serializing accessory parts info");
            LogMessage("Serializing accessory parts info");
            __state = value.parts;
            var max = Math.Max(Array.FindLastIndex(value.parts.ToArray(), obj => obj.type == 120), 20);
            value.parts = value.parts.Take(max).ToArray();
        }

        
        private static void Postfix()
        {
             LogMessage("Post Fix Serializing accessory parts info");
        }
    }   
    
    [HarmonyPatch(typeof(HumanDataStatusFormatter), nameof(HumanDataStatusFormatter.Serialize))]
    private static class HumanDataStatusFormatter_Patch
    {
        private static void Prefix()
        {
            LogMessage("Serializing Status");
        }
    
        private static void Postfix()
        {
            LogMessage("Serializing Status");
        }
    }
}
#endif