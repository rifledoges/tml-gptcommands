using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// "admin shoot": fires projectiles at a player from random spots around them, a few ticks apart.
    /// Types listed here are either hostile vanilla projectiles used as they are, or hostile copies from HostileProjectiles.cs.
    /// </summary>
    public class ShootSystem : ModSystem
    {
        public static readonly Dictionary<string, Func<int>> Types = new Dictionary<string, Func<int>>(StringComparer.OrdinalIgnoreCase)
        {
            // Hostile copies of friendly projectiles
            ["star"] = () => ModContent.ProjectileType<FallenStarStrike>(),
            ["fireball"] = () => ModContent.ProjectileType<HostileFireball>(),
            ["swordbeam"] = () => ModContent.ProjectileType<HostileSwordBeam>(),
            ["arrow"] = () => ModContent.ProjectileType<HostileArrow>(),
            ["bullet"] = () => ModContent.ProjectileType<HostileBullet>(),
            ["shuriken"] = () => ModContent.ProjectileType<HostileShuriken>(),
            ["knife"] = () => ModContent.ProjectileType<HostileKnife>(),
            ["laser"] = () => ModContent.ProjectileType<HostileLaser>(),

            // Vanilla hostile projectiles
            ["eyelaser"] = () => ProjectileID.EyeLaser,
            ["deathlaser"] = () => ProjectileID.DeathLaser,
            ["pinklaser"] = () => ProjectileID.PinkLaser,
            ["eyefire"] = () => ProjectileID.EyeFire,
            ["cursedflame"] = () => ProjectileID.CursedFlameHostile,
            ["skull"] = () => ProjectileID.Skull,
            ["bone"] = () => ProjectileID.SkeletonBone,
            ["sickle"] = () => ProjectileID.DeathSickle,
            ["seed"] = () => ProjectileID.SeedPlantera,
            ["poisonseed"] = () => ProjectileID.PoisonSeedPlantera,
            ["thornball"] = () => ProjectileID.ThornBall,
            ["stinger"] = () => ProjectileID.Stinger,
        };

        private const int TicksBetweenShots = 8;

        private class Shot
        {
            public int Slot;
            public int Type;
            public int Damage;
            public float Speed;
            public int Delay;
            public bool Homing;
        }

        // A fired projectile that is being steered toward its target.
        private class Homer
        {
            public int Index;
            public int Identity;
            public int Slot;
            public float Speed;
            public int TicksLeft;
        }

        private const int HomingTicks = 240;
        private const float HomingTurnRate = 0.08f;

        private static readonly List<Shot> shots = new List<Shot>();
        private static readonly List<Homer> homers = new List<Homer>();

        public static void Queue(Player target, int type, int count, int damage, float speed, bool homing)
        {
            for (int i = 0; i < count; i++)
                shots.Add(new Shot { Slot = target.whoAmI, Type = type, Damage = damage, Speed = speed, Delay = i * TicksBetweenShots, Homing = homing });
        }

        // The server steers homing projectiles by changing their velocity and syncing it to the players.
        private static void SteerHomingProjectiles()
        {
            for (int i = homers.Count - 1; i >= 0; i--)
            {
                Homer homer = homers[i];
                Projectile projectile = Main.projectile[homer.Index];
                Player player = Main.player[homer.Slot];
                if (--homer.TicksLeft <= 0 || !projectile.active || projectile.identity != homer.Identity || player == null || !player.active)
                {
                    homers.RemoveAt(i);
                    continue;
                }

                Vector2 toTarget = player.Center - projectile.Center;
                if (toTarget == Vector2.Zero)
                    continue;
                toTarget.Normalize();

                Vector2 velocity = Vector2.Lerp(projectile.velocity, toTarget * homer.Speed, HomingTurnRate);
                if (velocity != Vector2.Zero)
                {
                    velocity.Normalize();
                    projectile.velocity = velocity * homer.Speed;
                }

                if (homer.TicksLeft % 2 == 0)
                    projectile.netUpdate = true;
            }
        }

        public override void PostUpdateEverything()
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            SteerHomingProjectiles();

            if (shots.Count == 0)
                return;

            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Shot shot = shots[i];
                Player player = Main.player[shot.Slot];
                if (player == null || !player.active)
                {
                    shots.RemoveAt(i);
                    continue;
                }

                if (--shot.Delay > 0)
                    continue;

                // Random spot on a ring around the player, aimed slightly ahead of where they are heading.
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float distance = Main.rand.Next(350, 701);
                Vector2 start = player.Center + angle.ToRotationVector2() * distance;
                Vector2 aim = player.Center + player.velocity * (distance / shot.Speed) * 0.5f;
                Vector2 direction = aim - start;
                if (direction == Vector2.Zero)
                    direction = Vector2.UnitY;
                direction.Normalize();

                int index = Projectile.NewProjectile(new EntitySource_Misc("TMLAdmin"), start, direction * shot.Speed, shot.Type, shot.Damage, 4f, Main.myPlayer);
                if (shot.Homing && index >= 0 && index < Main.maxProjectiles)
                {
                    homers.Add(new Homer
                    {
                        Index = index,
                        Identity = Main.projectile[index].identity,
                        Slot = shot.Slot,
                        Speed = shot.Speed,
                        TicksLeft = HomingTicks
                    });
                }
                shots.RemoveAt(i);
            }
        }

        public override void Unload()
        {
            shots.Clear();
            homers.Clear();
        }
    }
}
