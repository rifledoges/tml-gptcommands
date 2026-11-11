using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ID;
using Terraria.ModLoader;

namespace TMLAdmin
{
    public class TMLAdmin : Mod
    {
        public const byte PacketSetSummonSlots = 0;
        public const byte PacketSetGodmode = 1;
        public const byte PacketFreeze = 2;
        public const byte PacketClearInventory = 3;
        public const byte PacketRocket = 4;
        public const byte PacketSetPvp = 5;
        public const byte PacketSetFallDamage = 6;
        public const byte PacketSetAccessorySlots = 7;
        public const byte PacketHurt = 8;
        public const byte PacketSetSentrySlots = 9;

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            // Server -> client: set the local player's bonus summon slots.
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            byte packetType = reader.ReadByte();
            if (packetType == PacketSetSummonSlots)
            {
                int bonus = reader.ReadInt32();
                Main.LocalPlayer.GetModPlayer<SummonSlotPlayer>().BonusMinions = bonus;
            }
            else if (packetType == PacketSetGodmode)
            {
                Main.LocalPlayer.GetModPlayer<GodModePlayer>().God = reader.ReadBoolean();
            }
            else if (packetType == PacketFreeze)
            {
                EffectsPlayer effects = Main.LocalPlayer.GetModPlayer<EffectsPlayer>();
                effects.Frozen = reader.ReadBoolean();
                effects.FrozenPosition = Main.LocalPlayer.position;
            }
            else if (packetType == PacketClearInventory)
            {
                Player me = Main.LocalPlayer;
                for (int i = 0; i < me.inventory.Length; i++)
                {
                    me.inventory[i].TurnToAir();
                    NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Main.myPlayer, PlayerItemSlotID.Inventory0 + i);
                }
            }
            else if (packetType == PacketRocket)
            {
                float speed = reader.ReadSingle();
                Player me = Main.LocalPlayer;
                me.velocity.Y = -speed;
                me.GetModPlayer<EffectsPlayer>().NoFallTicks = 600;
            }
            else if (packetType == PacketSetFallDamage)
            {
                // The packet carries whether fall damage is enabled.
                Main.LocalPlayer.GetModPlayer<EffectsPlayer>().FallDamageDisabled = !reader.ReadBoolean();
            }
            else if (packetType == PacketSetAccessorySlots)
            {
                Main.LocalPlayer.GetModPlayer<AccessorySlotPlayer>().BonusSlots = reader.ReadInt32();
            }
            else if (packetType == PacketHurt)
            {
                int damage = reader.ReadInt32();
                bool kill = reader.ReadBoolean();
                string deathText = reader.ReadString();
                Player me = Main.LocalPlayer;
                PlayerDeathReason reason = PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(deathText));
                if (kill)
                {
                    me.KillMe(reason, 1000.0, 0, false);
                }
                else
                {
                    // Clear invincibility frames so admin damage always lands (godmode still blocks it).
                    me.immune = false;
                    me.immuneTime = 0;
                    me.Hurt(reason, damage, 0, pvp: false, quiet: false, cooldownCounter: -1, dodgeable: false);
                }
            }
            else if (packetType == PacketSetSentrySlots)
            {
                Main.LocalPlayer.GetModPlayer<SentrySlotPlayer>().BonusSentries = reader.ReadInt32();
            }
            else if (packetType == PacketSetPvp)
            {
                Main.LocalPlayer.hostile = reader.ReadBoolean();
                NetMessage.SendData(MessageID.TogglePVP, -1, -1, null, Main.myPlayer);
            }
        }
    }
}
