using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using CharacterCreation.UI;
using HC.Maker;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace HC;

public class MoreAccessories : MonoBehaviour
{
    [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
    public static MakerMode? MakerMode;

    [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
    public static StudioMode? StudioMode;

    public MoreAccessories(IntPtr ptr) : base(ptr)
    {
        Plugin = this;
        SceneManager.sceneLoaded+= (UnityAction<UnityEngine.SceneManagement.Scene, LoadSceneMode>)LevelLoaded;
    }

    private static void LevelLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadMode)
    {
        Print($"Scene index {scene.buildIndex} Load {loadMode}");
        if (scene.buildIndex == 3)
        {
            switch (loadMode)
            {
                case LoadSceneMode.Additive:
                    StudioMode = new StudioMode();
                    return;
                case LoadSceneMode.Single:
                    MakerMode = new MakerMode();
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loadMode), loadMode, null);
            }
        }

        MakerMode = null;
        StudioMode = null;
    }

    
    public static MoreAccessories Plugin { get; private set; } = null!;


#if DEBUG
    internal static void Print(string text, LogLevel logLevel = LogLevel.Warning)
#else
        internal static void Print(string text, LogLevel logLevel)
#endif
    {
        MoreAccessoriesLoader.ManualLogSource.Log(logLevel, text);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            MoreAccessories.Print("Manual Image Custom");
            Experimental_Patches.ImageCustom_Patch.Manual();
        }
    }
}

public class StudioMode { }

public class MakerMode
{
    public AccessoryWindow AccessoryWindow = null!;
    public CopyWindow CopyWindow = null!;
    public TransferWindow TransferWindow = null!;
    public List<CharaMakerSlotData> AdditionalCharaMakerSlots { get; } = new();
}

public class CopyWindow
{
    internal CopyWindow(CoordinateCopyAccessory window)
    {
        Window = window;
    }

    public CoordinateCopyAccessory Window { get; }
}

public class TransferWindow
{
    internal TransferWindow(Accessory_01 window)
    {
        Window = window;
    }

    public Accessory_01 Window { get; }
}

public class CharaMakerSlotData
{
    internal CharaMakerSlotData() { }
    public GameObject AccessorySlot = null!;
    public GameObject CopySlot = null!;
    public GameObject TransferSlot = null!;
}