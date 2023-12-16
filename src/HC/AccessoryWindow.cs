using CharacterCreation.UI;

namespace HC;

public class AccessoryWindow
{
    internal AccessoryWindow(Accessory_00 accessoryTab)
    {
        AccessoryTab = accessoryTab;
    }

    public Accessory_00 AccessoryTab { get; set; }
}