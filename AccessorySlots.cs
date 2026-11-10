using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Up to five extra accessory slots, each enabled when the player's bonus slot count reaches its number.
    /// They use tModLoader's accessory slot system, so equipped items work like normal accessories.
    /// </summary>
    public abstract class BonusAccessorySlotBase : ModAccessorySlot
    {
        protected abstract int Number { get; }

        public override bool IsEnabled()
        {
            return Player.GetModPlayer<AccessorySlotPlayer>().BonusSlots >= Number;
        }

        public override bool IsVisibleWhenNotEnabled()
        {
            return false;
        }
    }

    public class BonusAccessorySlot1 : BonusAccessorySlotBase { protected override int Number => 1; }
    public class BonusAccessorySlot2 : BonusAccessorySlotBase { protected override int Number => 2; }
    public class BonusAccessorySlot3 : BonusAccessorySlotBase { protected override int Number => 3; }
    public class BonusAccessorySlot4 : BonusAccessorySlotBase { protected override int Number => 4; }
    public class BonusAccessorySlot5 : BonusAccessorySlotBase { protected override int Number => 5; }
}
