using Terraria;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Bonus sentry slots (max turrets). Saved by character name on the server and re-applied when the player joins.
    /// The limit is enforced on the player's own client, so the value is synced to the client by packet.
    /// </summary>
    public class SentrySlotPlayer : ModPlayer
    {
        public const int MaxBonus = 50;

        public int BonusSentries;

        public override void PostUpdateEquips()
        {
            if (BonusSentries != 0)
                Player.maxTurrets += BonusSentries;
        }

        /// <summary>Sets the bonus on the server's copy of the player and sends it to that player's client.</summary>
        public static void Apply(Player target, int slots)
        {
            target.GetModPlayer<SentrySlotPlayer>().BonusSentries = slots;
            ModPacket packet = ModContent.GetInstance<TMLAdmin>().GetPacket();
            packet.Write(TMLAdmin.PacketSetSentrySlots);
            packet.Write(slots);
            packet.Send(target.whoAmI);
        }
    }
}
