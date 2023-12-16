using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BepInEx.Logging;
using H;
using HarmonyLib;
using ILLGames.Unity;

namespace HC;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public static class Common_Patches
{
    //Character.LoadAccessory
    //Character.Update
    //Character.Update

    private static ManualLogSource ManualLogSource => MoreAccessoriesLoader.ManualLogSource;

    [HarmonyPatch]
    private static class RangeEqualOn_Patch
    {
        #region Potential Targets

        /*
ChangeAccessory(int slotNo, int type, int id, ChaAccessoryDefine.AccessoryParentKey parentKey, bool forceChange = false)
IsAccessory(int slotNo)
SetAccessoryState(int slotNo, bool show)
GetAccessoryDefaultParentStr(int slotNo)
GetAccessoryDefaultParentType(int slotNo)
ChangeAccessoryParent(int slotNo, ChaAccessoryDefine.AccessoryParentKey parentKey)
SetAccessoryPos(int slotNo, int correctNo, float value, bool add, int flag = 7)
SetAccessoryRot(int slotNo, int correctNo, float value, bool add, int flag = 7)
SetAccessoryScl(int slotNo, int correctNo, float value, bool add, int flag = 7)
ResetAccessoryMove(int slotNo, int correctNo, int type = 7)
UpdateAccessoryMoveFromInfo(int slotNo)
ChangeAccessoryPattern(int slotNo, int index = -1)
ChangeAccessoryPatternTexture(int slotNo, int index = -1)
ChangeAccessoryPatternColor(int slotNo, int index = -1)
ChangeAccessoryPatternParameter(int slotNo, int index = -1)
ChangeAccessoryColor(int slotNo)
GetAccessoryDefaultColorData(int slotNo)
GetAccessoryDefaultColor(ref Color color, int slotNo, int no)
SetAccessoryDefaultColor(int slotNo)
ChangeShakeAccessory(int slotNo)
SetHideHairAccessory(int slotNo)
SetupAccessoryFK(int slotNo)
UpdateAccessoryFK(int slotNo)
UpdateAccessoryFK(int slotNo, Il2CppStructArray<Vector3> values)
GetAccessoryDefaultFK(int slotNo)
SetAccessoryFK(int slotNo, int correctNo, float value, bool add, int flag = 7)
ResetAccessoryFK(int slotNo, int correctNo)
Accessory(Human human, ChaListDefine.CategoryNo type, int id, int slotNo, Human.UseCopyWeightType copyWeightType, Transform parent)
GetSlotName(int slotNo)
GetTLSlotTitle(int slotNo)
IsOpenCheck(int slotNo, int editNo)
Open(IReadOnlyList<GuideObject> guidList, int slotNo, int editNo)
UpdateAcsRotAdd(int slotNo, int editNo, int xyz, bool add, float val)
UpdateAcsMovePaste(int slotNo, int editNo, Vector3 value)
UpdateAcsAllReset(int slotNo, int editNo)
SetControllerTransform(int slotNo, int editNo)
CoordinateSync(Nullable<int> type, int slotNo, int editNo)
IsOpenCheck(int slotNo, int editNo)
Open(IReadOnlyList<GuideObject> guidList, int slotNo, int editNo)
UpdateSelection(int slotNo, int editNo)
UpdateAcsPosAdd(int slotNo, int editNo, int xyz, bool add, float val)
UpdateAcsRotAdd(int slotNo, int editNo, int xyz, bool add, float val)
UpdateAcsSclAdd(int slotNo, int editNo, int xyz, bool add, float val)
UpdateAcsMovePaste(int slotNo, int editNo, Il2CppStructArray<Vector3> value, Il2CppStructArray<bool> actives)
UpdateAcsAllReset(int slotNo, int editNo)
SetControllerTransform(int slotNo, int editNo)
CoordinateSync(Nullable<int> type, int slotNo, int editNo)
SelectParent(int slotNo)
Get(int slotNo)
UpdateSlotName(int slotNo)
UpdateAccessory(int slotNo, bool setDefaultColor)
SetDefaultAcsColor(int slotNo)
SetDefAcsColor(int slotNo, [In] ref HumanAccessory.DefaultColorData defColorData)
GetSlotName(int slotNo)
GetSlotNameForNone(int slotNo)
UpdateSlotName(int slotNo)
Data(int slotNo, int value, ChaListDefine.CategoryNo category)
RemoveAccessorySlot(Human human, int slotNo)
Method_Internal_Static_Void_Human_Int32_Int32_CategoryNo_0(Human human, int slotNo, int id, ChaListDefine.CategoryNo category)
SetSlotNo(int slotNo)
SettingSlot(int slotNo, int sel)
Setting(int slotNo, int sel)
IsOptionVisible(int slotNo)
SetColorData(int slotNo, IReadOnlyList<ThumbnailColor> thumbnailColors, Il2CppReferenceArray<Nullable<Color>> colorData)
SetColorWindow(int slotNo, int index, ThumbnailColor acsColors, Func<bool> updateUI)
ChangeData(int slotNo, bool isPrev)
SetSlotNo(int slotNo)
SetSlotNo(int slotNo)
Open(int slotNo, int correctIndex)
SetControllerTransform(int slotNo, int editNo)
GetAccessoryObject(int slotNo, int editNo)
SetAccessoryAngle(int slotNo, int editNo, [In] ref Vector3 angle)
SetAccessoryAngleLocal(int slotNo, int editNo, [In] ref Vector3 angle)
SetAccessoryAngleUpdate(int slotNo, int editNo)
SetCmpGuide(int slotNo)
SetSlotNo(int slotNo)
Open(int slotNo, int correctIndex)
SetControllerTransform(int slotNo, int editNo)
GetAccessoryObject(int slotNo, int editNo)
SetAccessoryPosition(int slotNo, int editNo, [In] ref Vector3 position)
SetAccessoryPositionLocal(int slotNo, int editNo, [In] ref Vector3 position)
SetAccessoryPositionUpdate(int slotNo, int editNo)
SetAccessoryAngle(int slotNo, int editNo, [In] ref Vector3 angle)
SetAccessoryAngleLocal(int slotNo, int editNo, [In] ref Vector3 angle)
SetAccessoryAngleUpdate(int slotNo, int editNo)
SetAccessoryScale(int slotNo, int editNo, [In] ref Vector3 scale)
SetAccessoryScaleUpdate(int slotNo, int editNo)
SetCmpGuide(int slotNo)
SetSlotNo(int slotNo)
Open(int slotNo)
UpdateSelectAccessoryParent(int slotNo, int parentIndex)
ChangeAccessoryParent(int slotNo)
SetSlotNo(int slotNo)
Setting(int slotNo, int index, int sel)
SetPatternData(int slotNo, IReadOnlyList<ThumbnailColor> thumbnailColors, Il2CppReferenceArray<ChaAccessoryComponent.Pattern> patterns)
SetColorPtnWindow(int slotNo, int index, ThumbnailColor ptnColor, Func<bool> updateUI)
OnClickAccessory(int _accessory)
*/

        #endregion

        private static MethodBase TargetMethod()
        {
            return typeof(MathfEx).GetMethod(nameof(MathfEx.RangeEqualOn))!.MakeGenericMethod(typeof(int));
        }

        /// <summary>
        /// Illusion puts range checks on methods that consume slot numbers.
        /// Checks have been constant 0 and 19 in previous games.
        /// This is dangerous in the event of something else matches criteria as this is blind to caller.
        /// Unlikely to be triggered by illusion themselves tho.
        /// </summary>
        /// <returns></returns>
        private static bool Prefix(int min, int max, ref bool __result)
        {
            if (min == 0 && max == 19)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch]
    private static class RangeOn_Patch
    {
        private static MethodBase TargetMethod()
        {
            return typeof(GlobalMethod).GetMethod(nameof(GlobalMethod.RangeOn))!.MakeGenericMethod(typeof(int));
        }

        /// <summary>
        /// UNKNOWN PURPOSE
        /// Illusion puts range checks on methods that consume slot numbers.
        /// Checks have been constant 0 and 19 in previous games.
        /// This is dangerous in the event of something else matches criteria as this is blind to caller.
        /// Unlikely to be triggered by illusion themselves tho.
        /// </summary>
        /// <returns></returns>
        private static void Prefix(int valNow, int valMin, int valMax)
        {
            MoreAccessoriesLoader.ManualLogSource.LogMessage($"RangeOn_Patch {valNow}:  {valMin} {valMax} Unknown use");
        }
    }
}