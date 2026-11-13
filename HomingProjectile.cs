using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TMLAdmin
{
    /// <summary>
    /// Makes a projectile curve toward a player. The homing settings are sent with the projectile to every client,
    /// and the steering runs identically on the server and on each client, like a vanilla homing projectile.
    /// Set by the shoot command; ordinary projectiles are untouched.
    /// </summary>
    public class HomingProjectile : GlobalProjectile
    {
        private const int MaxHomingTicks = 240;
        private const float TurnRate = 0.08f;

        public override bool InstancePerEntity => true;

        public bool Homing;
        public int TargetSlot;
        public float Speed;
        private int age;

        public static void Enable(Projectile projectile, int targetSlot, float speed)
        {
            HomingProjectile homing = projectile.GetGlobalProjectile<HomingProjectile>();
            homing.Homing = true;
            homing.TargetSlot = targetSlot;
            homing.Speed = speed;
            projectile.netUpdate = true; // resend so every client learns it is homing
        }

        public override void PostAI(Projectile projectile)
        {
            if (!Homing || ++age > MaxHomingTicks)
                return;

            if (TargetSlot < 0 || TargetSlot >= Main.maxPlayers)
                return;

            Player target = Main.player[TargetSlot];
            if (target == null || !target.active || target.dead)
                return;

            Vector2 toTarget = target.Center - projectile.Center;
            if (toTarget == Vector2.Zero)
                return;
            toTarget.Normalize();

            Vector2 velocity = Vector2.Lerp(projectile.velocity, toTarget * Speed, TurnRate);
            if (velocity == Vector2.Zero)
                return;

            velocity.Normalize();
            projectile.velocity = velocity * Speed;
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(Homing);
            if (Homing)
            {
                binaryWriter.Write((byte)TargetSlot);
                binaryWriter.Write(Speed);
            }
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            Homing = bitReader.ReadBit();
            if (Homing)
            {
                TargetSlot = binaryReader.ReadByte();
                Speed = binaryReader.ReadSingle();
            }
        }
    }
}
