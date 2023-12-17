using ChaCustom;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UniRx;

namespace MoreAccessoriesKOI.Patches.Maker
{
    /// <summary>
    /// replace instances of fixed value "20" with either the minimum part array length or cvsaccessory array length
    /// </summary>
    [HarmonyPatch]
    internal static class Maker_Replace_20_Patch
    {
#if DEBUG
        private static int count = 0;
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                MoreAccessories.Print(__exception.ToString(), BepInEx.Logging.LogLevel.Error);
                __exception = null;
            }
            return __exception;
        }
#endif
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var list = new List<MethodBase>
            {
                AccessTools.Method(typeof(CvsAccessory), nameof(CvsAccessory.UpdateCustomUI)),
                AccessTools.Method(typeof(CvsAccessory), nameof(CvsAccessory.FuncUpdateAcsParent)),
                AccessTools.Method(typeof(CvsAccessory), nameof(CvsAccessory.FuncUpdateAcsColor)),
                AccessTools.Method(typeof(CvsAccessory), nameof(CvsAccessory.FuncUpdateAccessory)),
                AccessTools.Method(typeof(CustomAcsChangeSlot), nameof(CustomAcsChangeSlot.ChangeColorWindow), new[] { typeof(int)}),
                AccessTools.Method(typeof(CustomAcsChangeSlot), nameof(CustomAcsChangeSlot.UpdateSlotNames)),
                AccessTools.Method(typeof(CvsAccessoryChange), nameof(CvsAccessoryChange.CalculateUI)),
                AccessTools.Method(typeof(CustomAcsChangeSlot), nameof(CustomAcsChangeSlot.LateUpdate)),
                AccessTools.Method(typeof(CustomControl), nameof(CustomControl.Update)),
            };

#if KK || KKS
            list.Add(AccessTools.Method(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.ChangeDstDD)));
            list.Add(AccessTools.Method(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.ChangeSrcDD)));
            list.Add(AccessTools.Method(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.CopyAcs)));
#endif
            return list;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = instructions.ToList();
#if DEBUG
            var worked = false;
            MoreAccessories.Print($"transpiler {count} started");
#endif
            for (var i = 0; i < instructionsList.Count; i++)
            {
                var inst = instructionsList[i];
                yield return inst;
                if (inst.opcode == OpCodes.Ldc_I4_S && inst.operand.ToString() == "20")
                {
#if DEBUG
                    worked = true;
#endif
                    yield return new CodeInstruction(OpCodes.Pop);//avoid label error
                    yield return new CodeInstruction(OpCodes.Call, typeof(Maker_Replace_20_Patch).GetMethod(nameof(AccessoryCount), AccessTools.all));
                    continue;
                }
            }
#if DEBUG
            MoreAccessories.Print($"Maker_Replace_20_Patch transpiler {count++} finished", worked ? BepInEx.Logging.LogLevel.Warning : BepInEx.Logging.LogLevel.Error);
#endif
        }

        private static int AccessoryCount()//works fine for copybutton since it is equal
        {
            if (CustomBase.instance.chaCtrl == null || MoreAccessories.MakerMode == null || MoreAccessories.MakerMode.AccessoriesWindow == null) return 20;
            return Math.Min(CustomBase.instance.chaCtrl.nowCoordinate.accessory.parts.Length, MoreAccessories.MakerMode.AccessoriesWindow.CvsAccessoryArray.Length);
        }
    }
}
