using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Hostile copies of friendly vanilla projectiles (same sprite, size and flight behavior) that damage players on contact.
    /// The "hostile" flag is not synced over the network, so each copy is its own projectile type known to both server and clients.
    /// They fly through blocks so a player underground can still be hit.
    /// </summary>
    public abstract class HostileProjectileCopy : ModProjectile
    {
        protected abstract int VanillaType { get; }

        public override string Texture => "Terraria/Images/Projectile_" + VanillaType;

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(VanillaType);
            AIType = VanillaType;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;
        }
    }

    public class HostileFireball : HostileProjectileCopy { protected override int VanillaType => ProjectileID.Fireball; }
    public class HostileSwordBeam : HostileProjectileCopy { protected override int VanillaType => ProjectileID.EnchantedBeam; }
    public class HostileArrow : HostileProjectileCopy { protected override int VanillaType => ProjectileID.WoodenArrowFriendly; }
    public class HostileBullet : HostileProjectileCopy { protected override int VanillaType => ProjectileID.Bullet; }
    public class HostileShuriken : HostileProjectileCopy { protected override int VanillaType => ProjectileID.Shuriken; }
    public class HostileKnife : HostileProjectileCopy { protected override int VanillaType => ProjectileID.ThrowingKnife; }
    public class HostileLaser : HostileProjectileCopy { protected override int VanillaType => ProjectileID.PurpleLaser; }
    public class HostileMagicMissile : HostileProjectileCopy { protected override int VanillaType => ProjectileID.MagicMissile; }
    public class HostileFrostBolt : HostileProjectileCopy { protected override int VanillaType => ProjectileID.FrostBoltSword; }
    public class HostileAmethystBolt : HostileProjectileCopy { protected override int VanillaType => ProjectileID.AmethystBolt; }
    public class HostileTopazBolt : HostileProjectileCopy { protected override int VanillaType => ProjectileID.TopazBolt; }
    public class HostileSapphireBolt : HostileProjectileCopy { protected override int VanillaType => ProjectileID.SapphireBolt; }
    public class HostileEmeraldBolt : HostileProjectileCopy { protected override int VanillaType => ProjectileID.EmeraldBolt; }
    public class HostileRubyBolt : HostileProjectileCopy { protected override int VanillaType => ProjectileID.RubyBolt; }
    public class HostileDiamondBolt : HostileProjectileCopy { protected override int VanillaType => ProjectileID.DiamondBolt; }
    public class HostileAmberBolt : HostileProjectileCopy { protected override int VanillaType => ProjectileID.AmberBolt; }
    public class HostileWaterBolt : HostileProjectileCopy { protected override int VanillaType => ProjectileID.WaterBolt; }
    public class HostileBallOfFire : HostileProjectileCopy { protected override int VanillaType => ProjectileID.BallofFire; }
    public class HostileDemonScythe : HostileProjectileCopy { protected override int VanillaType => ProjectileID.DemonScythe; }
    public class HostileCrystalStorm : HostileProjectileCopy { protected override int VanillaType => ProjectileID.CrystalStorm; }
    public class HostileShadowBeam : HostileProjectileCopy { protected override int VanillaType => ProjectileID.ShadowBeamFriendly; }
    public class HostileNightBeam : HostileProjectileCopy { protected override int VanillaType => ProjectileID.NightBeam; }
    public class HostileTerraBeam : HostileProjectileCopy { protected override int VanillaType => ProjectileID.TerraBeam; }
    public class HostileLightBeam : HostileProjectileCopy { protected override int VanillaType => ProjectileID.LightBeam; }
    public class HostileFireArrow : HostileProjectileCopy { protected override int VanillaType => ProjectileID.FireArrow; }
    public class HostileFrostburnArrow : HostileProjectileCopy { protected override int VanillaType => ProjectileID.FrostburnArrow; }
    public class HostileHellfireArrow : HostileProjectileCopy { protected override int VanillaType => ProjectileID.HellfireArrow; }
    public class HostileHolyArrow : HostileProjectileCopy { protected override int VanillaType => ProjectileID.HolyArrow; }
    public class HostileUnholyArrow : HostileProjectileCopy { protected override int VanillaType => ProjectileID.UnholyArrow; }
    public class HostileCursedArrow : HostileProjectileCopy { protected override int VanillaType => ProjectileID.CursedArrow; }
    public class HostileIchorArrow : HostileProjectileCopy { protected override int VanillaType => ProjectileID.IchorArrow; }
    public class HostileCursedBullet : HostileProjectileCopy { protected override int VanillaType => ProjectileID.CursedBullet; }
    public class HostileIchorBullet : HostileProjectileCopy { protected override int VanillaType => ProjectileID.IchorBullet; }
    public class HostileCrystalBullet : HostileProjectileCopy { protected override int VanillaType => ProjectileID.CrystalBullet; }
    public class HostileChlorophyteBullet : HostileProjectileCopy { protected override int VanillaType => ProjectileID.ChlorophyteBullet; }
    public class HostileHighVelocityBullet : HostileProjectileCopy { protected override int VanillaType => ProjectileID.BulletHighVelocity; }
    public class HostileMeteorShot : HostileProjectileCopy { protected override int VanillaType => ProjectileID.MeteorShot; }
    public class HostileNanoBullet : HostileProjectileCopy { protected override int VanillaType => ProjectileID.NanoBullet; }
    public class HostileBee : HostileProjectileCopy { protected override int VanillaType => ProjectileID.Bee; }
    public class HostileVampireKnife : HostileProjectileCopy { protected override int VanillaType => ProjectileID.VampireKnife; }
    public class HostilePoisonDart : HostileProjectileCopy { protected override int VanillaType => ProjectileID.PoisonDartBlowgun; }
    public class HostileStarWrath : HostileProjectileCopy { protected override int VanillaType => ProjectileID.StarWrath; }
    public class HostileRazorbladeTyphoon : HostileProjectileCopy { protected override int VanillaType => ProjectileID.Typhoon; }
    public class HostileBubble : HostileProjectileCopy { protected override int VanillaType => ProjectileID.Bubble; }
    public class HostileNebulaBolt : HostileProjectileCopy { protected override int VanillaType => ProjectileID.NebulaBolt; }
    public class HostileSkyFracture : HostileProjectileCopy { protected override int VanillaType => ProjectileID.SkyFracture; }
}
