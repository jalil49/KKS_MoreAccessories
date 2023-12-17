using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace MoreAccessoriesKOI.Patches.MainGame
{
    public class ChaControl_Patches
    {
        /// <summary>
        /// Check array sizes at multiple points when coordinate/character changes
        /// </summary>

        #region ArraySyncChecks

#if KK || KKS
        [HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
        internal class ChangeCoordinateType_Patch
        {
            private static void Postfix(ChaControl __instance) => ArraySyncCheck(__instance, true);
        }

        [HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetNowCoordinate), new[] { typeof(ChaFileCoordinate) })]
        internal class SetNowCoordinatePostFix
        {
            private static void Postfix(ChaControl __instance) => ArraySyncCheck(__instance);
        }
#elif EC
        [HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeNowCoordinate), typeof(ChaFileCoordinate), typeof(bool), typeof(bool))]
        internal class ChangeCoordinateTypePostFix
        {
            private static void Postfix(ChaControl __instance) => ArraySyncCheck(__instance);
        }
#endif


        [HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Load))]
        internal class ChaControlLoadPatch
        {
            private static void Postfix(ChaControl __instance) => ArraySyncCheck(__instance);
        }

#if KKS
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadNoAsync))]
        internal class ChaControlLoadAsyncPatch
        {
            [HarmonyPriority(Priority.Last)]
            private static void Prefix() => Common_Patches.Seal(false);

            [HarmonyPriority(Priority.First)]
            private static void Postfix(ChaControl __instance) => ArraySyncCheck(__instance);
        }
#endif
#if KK || KKS
        [HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.AssignCoordinate), typeof(ChaFileDefine.CoordinateType))]
#elif EC
        [HarmonyPriority(Priority.First), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.AssignCoordinate), new Type[0])]
#endif
        internal class AssignCoordinate_Patch
        {
            private static void Postfix(ChaControl __instance) => ArraySyncCheck(__instance, true);
        }

#if KKS
        [HarmonyPriority(Priority.Last), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), new[] { typeof(bool), typeof(bool) })]
#elif KK || EC
        [HarmonyPriority(Priority.Last), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), typeof(bool))]
#endif
        internal class ChacontrolChangeAccessory_Patch
        {
            private static void Prefix(ChaControl __instance) => ArraySyncCheck(__instance);
        }

        [HarmonyPriority(Priority.Last), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryAsync), typeof(bool))]
        internal class ChacontrolChangeAccessoryAsync_Patch
        {
            private static void Prefix(ChaControl __instance) => ArraySyncCheck(__instance);
        }

        internal static void ArraySyncCheck(ChaControl chara, bool setslot = false)
        {
            try
            {
                if (setslot && MoreAccessories.CharaMaker)
                {
                    Accessories.ShowSlot = Math.Max(Array.FindLastIndex(chara.nowCoordinate.accessory.parts, x => x.type != 120) + 1, 20);
                    MoreAccessories.NowCoordinateTrimAndSync(chara);
                }

                MoreAccessories.ArraySync(chara);

                MoreAccessories._self.UpdateUI();
            }
            catch (Exception ex)
            {
                MoreAccessories.Print($"{chara.fileParam.fullname} {ex}", LogLevel.Error);
            }
        }

        #endregion

        #region Transpiler Patches

        /// <summary>
        /// Async/coroutines need to be patched like this KKAPI can patch movenext, but cant use kkapi due to loop
        /// </summary>
        [HarmonyPatch]
        internal class ChaControl_ChangeAccessoryAsync_Patches
        {
            private static MethodBase TargetMethod()
            {
                MethodBase methodbase;
#if KKS
                methodbase = AccessTools.Method(AccessTools.TypeByName("ChaControl+<ChangeAccessoryAsync>d__483, Assembly-CSharp"), "MoveNext");
#elif KK || EC
                methodbase = AccessTools.Method(AccessTools.TypeByName("ChaControl+<ChangeAccessoryAsync>c__Iterator12, Assembly-CSharp"), "MoveNext");
#endif
                return methodbase;
            }

            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionsList = instructions.ToList();
                var end = instructionsList.FindIndex(4, x => x.opcode == OpCodes.Brtrue || x.opcode == OpCodes.Brtrue_S); //work backwards from end


                var start = end - 4; //code is at least 4 lines

                for (; start > 0; start--)
                {
                    if (instructionsList[start].opcode == OpCodes.Ldc_I4_0)
                    {
                        break;
                    }
                }

                for (var i = 0; i < instructionsList.Count; i++)
                {
                    var inst = instructionsList[i];

                    if (i == start) //instead pushing 0,slot,19 and popping RangeEqual => push Chacontrol, slotno and pop with call
                    {
                        i++; //skip pushing 0 to stack
#if KKS
                        yield return new CodeInstruction(OpCodes.Ldloc_1); //ldarg_0  contains chacontrol don't use this.chacontrol parts array is null for a mysterious reason
#elif KK || EC
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.TypeByName("ChaControl+<ChangeAccessoryAsync>c__Iterator12, Assembly-CSharp").GetField("$this", AccessTools.all));
#endif
                        yield return instructionsList[i++]; //ldarg_0  this
                        yield return instructionsList[i++]; //ldfld  this.Chacontrol
                        yield return instructionsList[i++]; //ldfld  this.Chacontrol.slotno

                        yield return new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(AccessorySlotBoolCheck), AccessTools.all));
                        i += 2; //skip 19 insert and call range
                    }

                    yield return instructionsList[i];
                }
            }
        }

        [HarmonyPatch]
        internal class ChaControl_ChangeAccessoryAsync_Replace20_Patches
        {
            private static MethodBase TargetMethod()
            {
                MethodBase methodbase;
#if KKS
                methodbase = AccessTools.Method(AccessTools.TypeByName("ChaControl+<ChangeAccessoryAsync>d__482, Assembly-CSharp"), "MoveNext");
#elif KK || EC
                methodbase = AccessTools.Method(AccessTools.TypeByName("ChaControl+<ChangeAccessoryAsync>c__Iterator11, Assembly-CSharp"), "MoveNext");
#endif
                return methodbase;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionsList = instructions.ToList();

                for (var i = 0; i < instructionsList.Count; i++)
                {
                    var inst = instructionsList[i];
                    if (inst.opcode == OpCodes.Ldc_I4_S && inst.operand.ToString() == "20")
                    {
#if KKS
                        yield return new CodeInstruction(OpCodes.Ldloc_1); //feed chacontrol to method
#elif KK || EC
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.TypeByName("ChaControl+<ChangeAccessoryAsync>c__Iterator11, Assembly-CSharp").GetField("$this", AccessTools.all));
#endif
                        yield return new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(AccessoryCount), AccessTools.all));
                        continue;
                    }

                    yield return inst;
                }
            }
        }

        /// <summary>
        /// Patches where slot number is the first parameter
        /// </summary>
        [HarmonyPatch]
        internal class ChaControl_CheckAdjuster_param_slot_0_Patches
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                var ChaCon = typeof(ChaControl);
                var list = new List<MethodBase>
                {
                    AccessTools.Method(ChaCon, nameof(ChaControl.ChangeAccessoryParent)), //0
                    AccessTools.Method(ChaCon, nameof(ChaControl.SetAccessoryPos)), //1
                    AccessTools.Method(ChaCon, nameof(ChaControl.SetAccessoryRot)), //2
                    AccessTools.Method(ChaCon, nameof(ChaControl.SetAccessoryScl)), //3
                    AccessTools.Method(ChaCon, nameof(ChaControl.UpdateAccessoryMoveFromInfo)), //4
                    AccessTools.Method(ChaCon, nameof(ChaControl.ChangeAccessoryColor)), //5
                    AccessTools.Method(ChaCon, nameof(ChaControl.SetAccessoryDefaultColor)), //6
                    AccessTools.Method(ChaCon, nameof(ChaControl.IsAccessory)) //6
                };

#if KKS
                list.Add(AccessTools.Method(ChaCon, nameof(ChaControl.ChangeAccessoryNoAsync)));
#endif

                return list;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionsList = instructions.ToList();
                var end = instructionsList.FindIndex(4, x => x.opcode == OpCodes.Brtrue || x.opcode == OpCodes.Brtrue_S); //work backwards from end


                var start = end - 4; //code is at least 4 lines

                for (; start > 0; start--)
                {
                    if (instructionsList[start].opcode == OpCodes.Ldc_I4_0)
                    {
                        break;
                    }
                }

                for (; start > 0; start--)
                {
                    if (instructionsList[start].opcode == OpCodes.Ldc_I4_0)
                    {
                        break;
                    }
                }

                for (var i = 0; i < instructionsList.Count; i++)
                {
                    var inst = instructionsList[i];

                    if (i == start)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);

                        yield return new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(AccessorySlotBoolCheck), AccessTools.all));
                        i = end;
                        inst = instructionsList[i];
                    }

                    yield return inst;
                }
            }
        }

        /// <summary>
        /// Patches where slot number is the second parameter
        /// </summary>
        [HarmonyPatch]
        internal class ChaControl_CheckAdjuster_param_slot_1_Patches
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                var ChaCon = typeof(ChaControl);
                var list = new List<MethodBase>
                {
                    AccessTools.Method(ChaCon, nameof(ChaControl.GetAccessoryDefaultColor))
                };
                return list;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionsList = instructions.ToList();
                var end = instructionsList.FindIndex(4, x => x.opcode == OpCodes.Brtrue); //work backwards from end


                var start = end - 4; //code is at least 4 lines

                for (; start > 0; start--)
                {
                    if (instructionsList[start].opcode == OpCodes.Ldc_I4_0)
                    {
                        break;
                    }
                }

                for (; start > 0; start--)
                {
                    if (instructionsList[start].opcode == OpCodes.Ldc_I4_0)
                    {
                        break;
                    }
                }

                for (var i = 0; i < instructionsList.Count; i++)
                {
                    var inst = instructionsList[i];

                    if (i == start)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldarg_2);
                        yield return new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(AccessorySlotBoolCheck), AccessTools.all));
                        i = end;
                        inst = instructionsList[i];
                    }

                    yield return inst;
                }
            }
        }

        /// <summary>
        /// Replace 20 values with current accessory parts length
        /// </summary>
        [HarmonyPatch]
        private static class ChaControl_Replace_20_Patch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                var list = new List<MethodBase>
                {
                    AccessTools.Method(typeof(ChaControl), nameof(ChaControl.UpdateAccessoryMoveAllFromInfo)),
#if KKS
                    AccessTools.Method(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), new[] { typeof(bool), typeof(bool) }),
#elif KK || EC
                    AccessTools.Method(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), new[] { typeof(bool) }),
#endif
                };
                return list;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionsList = instructions.ToList();
                for (var i = 0; i < instructionsList.Count; i++)
                {
                    var inst = instructionsList[i];
                    if (inst.opcode == OpCodes.Ldc_I4_S && inst.operand.ToString() == "20")
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0); //feed chacontrol to method
                        yield return new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(AccessoryCount), AccessTools.all));
                        continue;
                    }

                    yield return inst;
                }
            }
        }

        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateVisible))]
        internal class UpdateVisible_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionsList = instructions.ToList();
                //var end = instructionsList.FindLastIndex(x => x.opcode == OpCodes.Conv_I4);
                var end = instructionsList.Count - 1;
#if KK || KKS
                const int endcount = 0;
#elif EC
               const int endcount = 1;
#endif

                var findcount = 0;
                for (; end > 0; end--)
                {
                    if (instructionsList[end].opcode == OpCodes.Conv_I4)
                    {
                        if (findcount++ == endcount)
                        {
                            break;
                        }
                    }
                }

                var start = end - 1;
                while (instructionsList[start].opcode != OpCodes.Call)
                {
                    start--;
                }

                for (var i = 0; i < instructionsList.Count; i++)
                {
                    var inst = instructionsList[i];
                    if (i == start)
                    {
                        //yield return instructionsList[start - 2];
                        yield return new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(AccessoryCount), AccessTools.all));
                        i = end;
                        continue;
                    }

                    yield return inst;
                }
            }
        }

        private static bool AccessorySlotBoolCheck(ChaControl chara, int slot)
        {
            try
            {
                return MathfEx.RangeEqualOn(0, slot, chara.nowCoordinate.accessory.parts.Length - 1);
            }
            catch (Exception ex)
            {
                MoreAccessories.Print($"{chara.fileParam.fullname} {ex}", LogLevel.Error);
                return MathfEx.RangeEqualOn(0, slot, 19);
            }
        }

        private static int AccessoryCount(ChaControl chara)
        {
            try
            {
                return chara.nowCoordinate.accessory.parts.Length;
            }
            catch (Exception ex)
            {
                MoreAccessories.Print($"{chara.fileParam.fullname} {ex}", LogLevel.Error);
                return 20;
            }
        }

        #endregion

        /// <summary>
        /// Probably got lazy and just replicated the original since its small function that'll work 
        /// </summary>

        #region Prefix override

        [HarmonyPriority(Priority.Last), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetAccessoryState))]
        internal class SetAccessory_Patch
        {
            private static bool Prefix(ChaControl __instance, int slotNo, bool show)
            {
                try
                {
                    if (__instance.nowCoordinate.accessory.parts.Length <= slotNo)
                    {
                        return false;
                    }

                    __instance.fileStatus.showAccessory[slotNo] = show;
                    return false;
                }
                catch (Exception ex)
                {
                    MoreAccessories.Print($"{__instance.fileParam.fullname} {ex}", LogLevel.Error);
                    return true;
                }
            }
        }

        [HarmonyPriority(Priority.Last), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetAccessoryStateAll))]
        internal class SetAccessoryStateAll_Patch
        {
            private static bool Prefix(ChaControl __instance, bool show)
            {
                try
                {
                    var length = __instance.nowCoordinate.accessory.parts.Length;
                    for (var i = 0; i < length; i++)
                    {
                        __instance.fileStatus.showAccessory[i] = show;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    MoreAccessories.Print($"{__instance.fileParam.fullname} {ex}", LogLevel.Error);
                    return true;
                }
            }
        }

#if KK || KKS
        [HarmonyPriority(Priority.Last), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetAccessoryStateCategory))]
        internal class SetAccessoryStateCategoryPatch
        {
            private static bool Prefix(ChaControl __instance, int cateNo, bool show)
            {
                try
                {
                    if (cateNo != 0 && 1 != cateNo)
                    {
                        return false;
                    }

                    var length = __instance.nowCoordinate.accessory.parts.Length;
                    for (var i = 0; i < length; i++)
                    {
                        if (__instance.nowCoordinate.accessory.parts[i].hideCategory == cateNo)
                        {
                            __instance.fileStatus.showAccessory[i] = show;
                        }
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    MoreAccessories.Print($"{__instance.fileParam.fullname} {ex}", BepInEx.Logging.LogLevel.Error);
                    return true;
                }
            }
        }

        [HarmonyPriority(Priority.Last), HarmonyPatch(typeof(ChaControl), nameof(ChaControl.GetAccessoryCategoryCount))]
        internal class GetAccessoryCategoryCountPatch
        {
            private static bool Prefix(ChaControl __instance, int cateNo, ref int __result)
            {
                try
                {
                    if (cateNo != 0 && 1 != cateNo)
                    {
                        __result = -1;
                        return false;
                    }

                    __result = 0;
                    var length = __instance.nowCoordinate.accessory.parts.Length;

                    for (var i = 0; i < length; i++)
                    {
                        if (__instance.nowCoordinate.accessory.parts[i].hideCategory == cateNo)
                        {
                            __result++;
                        }
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    MoreAccessories.Print($"{__instance.fileParam.fullname} {ex}", BepInEx.Logging.LogLevel.Error);
                    return true;
                }
            }
        }
#endif

        #endregion
    }
}