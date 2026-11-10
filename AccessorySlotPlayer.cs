using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Number of bonus accessory slots granted by an admin (0 to MaxBonus). Session-only.
    /// The slots themselves are the BonusAccessorySlot classes in AccessorySlots.cs; they show up
    /// in the player's own accessory UI on their client, so the value is synced to the client by packet.
    /// </summary>
    public class AccessorySlotPlayer : ModPlayer
    {
        public const int MaxBonus = 5;

        public int BonusSlots;
    }
}
