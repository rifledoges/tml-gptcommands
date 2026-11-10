using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TMLAdmin
{
    public class TMLAdmin : Mod
    {
        public const byte PacketSetSummonSlots = 0;

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
        }
    }
}
