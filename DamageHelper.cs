using Terraria;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// A player's health is controlled by their own client, so hurting the server's copy of a player does nothing.
    /// Instead the server tells the player's client to hurt (or kill) itself, and the client reports the result.
    /// </summary>
    public static class DamageHelper
    {
        public static void Send(Player target, int damage, string deathText, bool kill)
        {
            ModPacket packet = ModContent.GetInstance<TMLAdmin>().GetPacket();
            packet.Write(TMLAdmin.PacketHurt);
            packet.Write(damage);
            packet.Write(kill);
            packet.Write(deathText);
            packet.Send(target.whoAmI);
        }
    }
}
