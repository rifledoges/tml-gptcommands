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
            ["magicmissile"] = () => ModContent.ProjectileType<HostileMagicMissile>(),
            ["frostbolt"] = () => ModContent.ProjectileType<HostileFrostBolt>(),
            ["amethyst"] = () => ModContent.ProjectileType<HostileAmethystBolt>(),
            ["topaz"] = () => ModContent.ProjectileType<HostileTopazBolt>(),
            ["sapphire"] = () => ModContent.ProjectileType<HostileSapphireBolt>(),
            ["emerald"] = () => ModContent.ProjectileType<HostileEmeraldBolt>(),
            ["ruby"] = () => ModContent.ProjectileType<HostileRubyBolt>(),
            ["diamond"] = () => ModContent.ProjectileType<HostileDiamondBolt>(),
            ["amber"] = () => ModContent.ProjectileType<HostileAmberBolt>(),
            ["waterbolt"] = () => ModContent.ProjectileType<HostileWaterBolt>(),
            ["ballofflame"] = () => ModContent.ProjectileType<HostileBallOfFire>(),
            ["demonscythe"] = () => ModContent.ProjectileType<HostileDemonScythe>(),
            ["crystalstorm"] = () => ModContent.ProjectileType<HostileCrystalStorm>(),
            ["shadowbeam"] = () => ModContent.ProjectileType<HostileShadowBeam>(),
            ["nightbeam"] = () => ModContent.ProjectileType<HostileNightBeam>(),
            ["terrabeam"] = () => ModContent.ProjectileType<HostileTerraBeam>(),
            ["lightbeam"] = () => ModContent.ProjectileType<HostileLightBeam>(),
            ["firearrow"] = () => ModContent.ProjectileType<HostileFireArrow>(),
            ["frostarrow"] = () => ModContent.ProjectileType<HostileFrostburnArrow>(),
            ["hellfirearrow"] = () => ModContent.ProjectileType<HostileHellfireArrow>(),
            ["holyarrow"] = () => ModContent.ProjectileType<HostileHolyArrow>(),
            ["unholyarrow"] = () => ModContent.ProjectileType<HostileUnholyArrow>(),
            ["cursedarrow"] = () => ModContent.ProjectileType<HostileCursedArrow>(),
            ["ichorarrow"] = () => ModContent.ProjectileType<HostileIchorArrow>(),
            ["cursedbullet"] = () => ModContent.ProjectileType<HostileCursedBullet>(),
            ["ichorbullet"] = () => ModContent.ProjectileType<HostileIchorBullet>(),
            ["crystalbullet"] = () => ModContent.ProjectileType<HostileCrystalBullet>(),
            ["chlorophytebullet"] = () => ModContent.ProjectileType<HostileChlorophyteBullet>(),
            ["highvelocity"] = () => ModContent.ProjectileType<HostileHighVelocityBullet>(),
            ["meteorshot"] = () => ModContent.ProjectileType<HostileMeteorShot>(),
            ["nanobullet"] = () => ModContent.ProjectileType<HostileNanoBullet>(),
            ["bee"] = () => ModContent.ProjectileType<HostileBee>(),
            ["vampireknife"] = () => ModContent.ProjectileType<HostileVampireKnife>(),
            ["poisondart"] = () => ModContent.ProjectileType<HostilePoisonDart>(),
            ["starwrath"] = () => ModContent.ProjectileType<HostileStarWrath>(),
            ["razortyphoon"] = () => ModContent.ProjectileType<HostileRazorbladeTyphoon>(),
            ["bubble"] = () => ModContent.ProjectileType<HostileBubble>(),
            ["nebulabolt"] = () => ModContent.ProjectileType<HostileNebulaBolt>(),
            ["skyfracture"] = () => ModContent.ProjectileType<HostileSkyFracture>(),

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
            ["frostblast"] = () => ProjectileID.FrostBlastHostile,
            ["snowball"] = () => ProjectileID.SnowBallHostile,
            ["javelin"] = () => ProjectileID.JavelinHostile,
            ["spikedslimespike"] = () => ProjectileID.SpikedSlimeSpike,
            ["deadeye"] = () => ProjectileID.BulletDeadeye,
            ["flamingscythe"] = () => ProjectileID.FlamingScythe,
            ["saucerlaser"] = () => ProjectileID.SaucerLaser,
            ["martianbolt"] = () => ProjectileID.MartianTurretBolt,
            ["scramblerbolt"] = () => ProjectileID.BrainScramblerBolt,
            ["betsyfireball"] = () => ProjectileID.DD2BetsyFireball,
            ["betsyflame"] = () => ProjectileID.DD2BetsyFlameBreath,
            ["drakinshot"] = () => ProjectileID.DD2DrakinShot,
            ["ogrespit"] = () => ProjectileID.DD2OgreSpit,
            ["lightningzap"] = () => ProjectileID.DD2LightningBugZap,
            ["shadowbeamhostile"] = () => ProjectileID.ShadowBeamHostile,
            ["infernobolt"] = () => ProjectileID.InfernoHostileBolt,
            ["phantasmalbolt"] = () => ProjectileID.PhantasmalBolt,
            ["phantasmaleye"] = () => ProjectileID.PhantasmalEye,
            ["phantasmalsphere"] = () => ProjectileID.PhantasmalSphere,
            ["lightningorb"] = () => ProjectileID.CultistBossLightningOrb,
            ["cultistfireball"] = () => ProjectileID.CultistBossFireBall,
            ["icemist"] = () => ProjectileID.CultistBossIceMist,
            ["ancientdoom"] = () => ProjectileID.AncientDoomProjectile,
            ["rainbowstreak"] = () => ProjectileID.HallowBossRainbowStreak,
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

        private static readonly List<Shot> shots = new List<Shot>();

        public static void Queue(Player target, int type, int count, int damage, float speed, bool homing)
        {
            for (int i = 0; i < count; i++)
                shots.Add(new Shot { Slot = target.whoAmI, Type = type, Damage = damage, Speed = speed, Delay = i * TicksBetweenShots, Homing = homing });
        }

        public override void PostUpdateEverything()
        {
            if (Main.netMode != NetmodeID.Server)
                return;

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
                    HomingProjectile.Enable(Main.projectile[index], shot.Slot, shot.Speed);
                shots.RemoveAt(i);
            }
        }

        public override void Unload()
        {
            shots.Clear();
        }
    }
}
