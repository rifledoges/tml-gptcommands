using Terraria;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Number of bonus accessory slots granted by an admin (0 to MaxBonus). Saved by character name on the server and re-applied when the player joins.
    /// The slots themselves are the BonusAccessorySlot classes in AccessorySlots.cs; they show up
    /// in the player's own accessory UI on their client, so the value is synced to the client by packet.
    /// </summary>
    public class AccessorySlotPlayer : ModPlayer
    {
        public const int MaxBonus = 5;

        public int BonusSlots;

        /// <summary>Sets the bonus on the server's copy of the player and sends it to that player's client.</summary>
        public static void Apply(Player target, int slots)
        {
            target.GetModPlayer<AccessorySlotPlayer>().BonusSlots = slots;
            ModPacket packet = ModContent.GetInstance<TMLAdmin>().GetPacket();
            packet.Write(TMLAdmin.PacketSetAccessorySlots);
            packet.Write(slots);
            packet.Send(target.whoAmI);
        }
    }
}
