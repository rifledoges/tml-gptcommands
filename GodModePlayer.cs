using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Session-only godmode. Damage is applied on each player's own client, so the flag is synced
    /// from the server (see TMLAdmin.HandlePacket) and enforced locally.
    /// </summary>
    public class GodModePlayer : ModPlayer
    {
        public bool God;

        public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
        {
            return God;
        }

        public override void PostUpdate()
        {
            if (God)
            {
                Player.statLife = Player.statLifeMax2;
                Player.statMana = Player.statManaMax2;
            }
        }
    }
}
