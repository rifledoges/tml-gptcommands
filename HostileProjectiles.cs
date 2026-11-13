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
}
