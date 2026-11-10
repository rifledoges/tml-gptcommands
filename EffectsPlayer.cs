using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Client-side effects triggered by the server through packets: freeze and rocket.
    /// Movement is controlled by each player's own client, so these are enforced locally.
    /// Session-only: nothing here is saved.
    /// </summary>
    public class EffectsPlayer : ModPlayer
    {
        public bool Frozen;
        public Vector2 FrozenPosition;
        public int NoFallTicks;

        private bool IsLocalClient => Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer;

        public override void SetControls()
        {
            if (Frozen && IsLocalClient)
            {
                Player.controlLeft = false;
                Player.controlRight = false;
                Player.controlUp = false;
                Player.controlDown = false;
                Player.controlJump = false;
            }
        }

        public override void PostUpdate()
        {
            if (Frozen && IsLocalClient)
            {
                Player.position = FrozenPosition;
                Player.velocity = Vector2.Zero;
            }

            if (NoFallTicks > 0)
                NoFallTicks--;
        }

        public override void PostUpdateEquips()
        {
            if (NoFallTicks > 0)
                Player.noFallDmg = true;
        }
    }
}
