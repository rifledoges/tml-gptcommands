using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Fallen star strikes on a player. Each star drops from above as a falling star projectile and
    /// hurts the player when it lands (the damage is applied on the player's own client).
    /// </summary>
    public class StarfallSystem : ModSystem
    {
        private const int FlightTicks = 30;
        private const int SpacingTicks = 25;

        private class Strike
        {
            public int Slot;
            public int TicksLeft;
            public int Damage;
            public bool Spawned;
        }

        private static readonly List<Strike> strikes = new List<Strike>();

        public static void Start(Player target, int stars, int damage)
        {
            for (int i = 0; i < stars; i++)
            {
                strikes.Add(new Strike
                {
                    Slot = target.whoAmI,
                    TicksLeft = FlightTicks + 1 + i * SpacingTicks,
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

                if (!strike.Spawned && strike.TicksLeft <= FlightTicks)
                {
                    strike.Spawned = true;
                    Vector2 start = player.Center + new Vector2(Main.rand.Next(-150, 151), -500f);
                    Vector2 velocity = (player.Center - start) / FlightTicks;
                    Projectile.NewProjectile(new EntitySource_Misc("TMLAdmin"), start, velocity, ProjectileID.FallingStar, 0, 0f, Main.myPlayer);
                }

                if (strike.TicksLeft <= 0)
                {
                    DamageHelper.Send(player, strike.Damage, $"{player.name} was struck by a fallen star.", false);
                    strikes.RemoveAt(i);
                }
            }
        }

        public override void Unload()
        {
            strikes.Clear();
        }
    }
}
