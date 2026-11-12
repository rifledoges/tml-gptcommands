using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// A hostile copy of the falling star projectile: same sprite, size and flight behavior,
    /// but it damages players on contact with its own hitbox. Used by the starfall command.
    /// </summary>
    public class FallenStarStrike : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FallingStar;

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.FallingStar);
            AIType = ProjectileID.FallingStar;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false; // fly through blocks so it still reaches a player underground
            Projectile.timeLeft = 90;
        }
    }
}
