using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ChaCustom;
using HarmonyLib;

namespace MoreAccessoriesKOI.Patches.Maker
{
    /// <summary>
    /// replace instances of fixed value "20" with either the minimum part array length or CvsAccessory array length
    /// </summary>
    [HarmonyPatch]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Harmony Patches - Used Externally")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Harmony Patches - Used Externally")]
    internal static class MakerReplace20_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var list = new List<MethodBase>
            {
                AccessTools.Method(typeof(CvsAccessory), nameof(CvsAccessory.UpdateCustomUI)),
                AccessTools.Method(typeof(CvsAccessory), nameof(CvsAccessory.FuncUpdateAcsParent)),
                AccessTools.Method(typeof(CvsAccessory), nameof(CvsAccessory.FuncUpdateAcsColor)),
                AccessTools.Method(typeof(CvsAccessory), nameof(CvsAccessory.FuncUpdateAccessory)),
                AccessTools.Method(typeof(CvsAccessoryChange), nameof(CvsAccessoryChange.CalculateUI)),
                AccessTools.Method(typeof(CustomAcsChangeSlot), nameof(CustomAcsChangeSlot.ChangeColorWindow), new[] { typeof(int) }),
                AccessTools.Method(typeof(CustomAcsChangeSlot), nameof(CustomAcsChangeSlot.UpdateSlotNames)),
                AccessTools.Method(typeof(CustomAcsChangeSlot), nameof(CustomAcsChangeSlot.LateUpdate)),
                AccessTools.Method(typeof(CustomControl), nameof(CustomControl.Update))
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
                    yield return new CodeInstruction(OpCodes.Call, typeof(MakerReplace20_Patch).GetMethod(nameof(AccessoryCount), AccessTools.all));
                    continue;
                }

                yield return inst;
            }
        }

        private static int AccessoryCount() //works fine for copy button since it is equal
        {
            if (CustomBase.instance.chaCtrl == null || MoreAccessories.MakerMode == null || MoreAccessories.MakerMode.AccessoriesWindow == null) return 20;
            return CustomBase.instance.chaCtrl.nowCoordinate.accessory.parts.Length;
        }
    }
}