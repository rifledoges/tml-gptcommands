using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Boss rush event: spawns bosses on one player one after another, each as soon as the last is defeated.
    /// Session-only; it stops if the player leaves.
    /// </summary>
    public class BossRushSystem : ModSystem
    {
        private static readonly string[] PreHardmode =
            { "kingslime", "eyeofcthulhu", "eaterofworlds", "brainofcthulhu", "queenbee", "skeletron", "deerclops" };

        private static readonly string[] Hardmode =
            { "queenslime", "twins", "destroyer", "skeletronprime", "plantera", "golem", "dukefishron", "empressoflight", "moonlord" };

        private static bool active;
        private static int playerSlot;
        private static string[] stages = new string[0];
        private static int stageIndex;
        private static int delay;
        private static int tick;

        public static bool Active => active;

        public static string[] StagesFor(string mode)
        {
            switch (mode.ToLowerInvariant())
            {
                case "pre": return PreHardmode;
                case "hard": return Hardmode;
                case "all": return PreHardmode.Concat(Hardmode).ToArray();
                default: return null;
            }
        }

        public static void Start(int slot, string[] bossStages)
        {
            active = true;
            playerSlot = slot;
            stages = bossStages;
            stageIndex = -1;
            delay = 4;
            Announce($"Boss rush starting on {Main.player[slot].name}! {stages.Length} bosses. Get ready...", Color.OrangeRed);
        }

        public static void Stop(string reason)
        {
            if (!active)
                return;
            active = false;
            Announce(reason, Color.OrangeRed);
        }

        private static void Announce(string text, Color color)
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
        }

        // Types that must all be gone before a boss counts as defeated.
        private static List<int> TypesToWatch(string stage)
        {
            var types = new List<int>(WorldCommands.Bosses[stage]);
            if (stage == "eaterofworlds")
            {
                types.Add(NPCID.EaterofWorldsBody);
                types.Add(NPCID.EaterofWorldsTail);
            }
            else if (stage == "destroyer")
            {
                types.Add(NPCID.TheDestroyerBody);
                types.Add(NPCID.TheDestroyerTail);
            }
            return types;
        }

        public override void PostUpdateEverything()
        {
            if (!active || Main.netMode != NetmodeID.Server)
                return;

            if (++tick % 30 != 0)
                return;

            Player player = Main.player[playerSlot];
            if (player == null || !player.active)
            {
                Stop("Boss rush ended because the player left.");
                return;
            }

            if (delay > 0)
            {
                delay--;
                return;
            }

            if (stageIndex >= 0 && TypesToWatch(stages[stageIndex]).Any(type => NPC.AnyNPCs(type)))
                return;

            if (stageIndex >= 0)
                Announce($"{stages[stageIndex]} defeated!", Color.LightGreen);

            stageIndex++;
            if (stageIndex >= stages.Length)
            {
                Stop($"Boss rush complete! Well done, {player.name}.");
                return;
            }

            foreach (int type in WorldCommands.Bosses[stages[stageIndex]])
                NPC.SpawnOnPlayer(playerSlot, type);

            Announce($"Boss {stageIndex + 1}/{stages.Length}: {stages[stageIndex]}!", Color.OrangeRed);
            delay = 4;
        }

        public override void Unload()
        {
            active = false;
        }
    }
}
