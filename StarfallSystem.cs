using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Fallen star strikes on a player. Each star is a hostile falling star projectile (FallenStarStrike) that drops
    /// from above and damages the player with its own hitbox, so a moving player can dodge it.
    /// </summary>
    public class StarfallSystem : ModSystem
    {
        private const int FlightTicks = 30;
        private const int SpacingTicks = 45; // longer than the player's invincibility frames so every star can land

        private class Strike
        {
            public int Slot;
            public int TicksLeft;
            public int Damage;
        }

        private static readonly List<Strike> strikes = new List<Strike>();

        public static void Start(Player target, int stars, int damage)
        {
            for (int i = 0; i < stars; i++)
            {
                strikes.Add(new Strike
                {
                    Slot = target.whoAmI,
                    TicksLeft = 1 + i * SpacingTicks,
                    Damage = damage
                });
            }
        }

        public override void PostUpdateEverything()
        {
            if (Main.netMode != NetmodeID.Server || strikes.Count == 0)
                return;

            for (int i = strikes.Count - 1; i >= 0; i--)
            {
                Strike strike = strikes[i];
                Player player = Main.player[strike.Slot];
                if (player == null || !player.active)
                {
                    strikes.RemoveAt(i);
                    continue;
                }

                strike.TicksLeft--;
                if (strike.TicksLeft > 0)
                    continue;

                // Aim slightly ahead of where the player is heading.
                Vector2 aim = player.Center + player.velocity * FlightTicks;
                Vector2 start = aim + new Vector2(Main.rand.Next(-150, 151), -500f);
                Vector2 velocity = (aim - start) / FlightTicks;
                Projectile.NewProjectile(new EntitySource_Misc("TMLAdmin"), start, velocity,
                    ModContent.ProjectileType<FallenStarStrike>(), strike.Damage, 4f, Main.myPlayer);
                strikes.RemoveAt(i);
            }
        }

        public override void Unload()
        {
            strikes.Clear();
        }
    }
}
