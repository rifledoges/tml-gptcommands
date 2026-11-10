using System;
using Terraria;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>Server-wide enemy spawn rate multiplier. Session-only. 1 = normal, 0 = no spawns.</summary>
    public class SpawnRateNpc : GlobalNPC
    {
        public static float Multiplier = 1f;

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (Multiplier == 1f)
                return;

            if (Multiplier <= 0f)
            {
                maxSpawns = 0;
                return;
            }

            spawnRate = Math.Max(1, (int)(spawnRate / Multiplier));
            maxSpawns = (int)(maxSpawns * Multiplier);
        }
    }
}
