using System.ComponentModel;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Assembly = System.Reflection.Assembly;
using Object = UnityEngine.Object;

namespace HC;

[BepInProcess("DigitalCraft")]
[BepInProcess("HoneyCome")]
[BepInProcess("HoneyComeTrial")]
[Browsable(false)]
[BepInPlugin(GUID: Guid, Name: "MoreAccessories", Version: VersionNum)]
#pragma warning disable BepInEx002
public class MoreAccessoriesLoader : BasePlugin
#pragma warning restore BepInEx002
{
    internal static ManualLogSource ManualLogSource = null!;
    internal const string VersionNum = "2.0.22";
    private const string Guid = "com.joan6694.illusionplugins.moreaccessories";

    private const int SaveVersion = 2;
    private const string ExtSaveKey = "moreAccessories";

    internal static MoreAccessoriesLoader Instance = null!;

    public MoreAccessoriesLoader()
    {
        Instance = this;
        ManualLogSource = Log;
    }

    public override void Load()
    {
        ClassInjector.RegisterTypeInIl2Cpp<MoreAccessories>();
        var gameObject = new GameObject(nameof(MoreAccessories));
        gameObject.hideFlags |= HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<MoreAccessories>();
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }
}