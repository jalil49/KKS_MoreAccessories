using CharacterCreation.UI;
using UnityEngine;

namespace HC;

public class MoreAccessories : MonoBehaviour
{
    public static MakerMode MakerMode = null;

    public MoreAccessories(IntPtr ptr) : base(ptr)
    {
        Plugin = this;
    }

    public static MoreAccessories Plugin { get; private set; } = null!;
}

public class MakerMode
{
    public AccessoryWindow AccessoryWindow = null!;
    public CopyWindow CopyWindow = null!;
    public TransferWindow TransferWindow = null!;
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