using System;
using System.Collections.Generic;
using System.Globalization;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>time, rain, sandstorm, slimerain, bloodmoon, eclipse, invade, boss, butcher, clearitems, clearprojectiles, hardmode, settle</summary>
    public static class WorldCommands
    {
        internal static readonly Dictionary<string, int[]> Bosses = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["kingslime"] = new int[] { NPCID.KingSlime },
            ["eyeofcthulhu"] = new int[] { NPCID.EyeofCthulhu },
            ["eaterofworlds"] = new int[] { NPCID.EaterofWorldsHead },
            ["brainofcthulhu"] = new int[] { NPCID.BrainofCthulhu },
            ["queenbee"] = new int[] { NPCID.QueenBee },
            ["skeletron"] = new int[] { NPCID.SkeletronHead },
            ["deerclops"] = new int[] { NPCID.Deerclops },
            ["twins"] = new int[] { NPCID.Retinazer, NPCID.Spazmatism },
            ["destroyer"] = new int[] { NPCID.TheDestroyer },
            ["skeletronprime"] = new int[] { NPCID.SkeletronPrime },
            ["queenslime"] = new int[] { NPCID.QueenSlimeBoss },
            ["plantera"] = new int[] { NPCID.Plantera },
            ["golem"] = new int[] { NPCID.Golem },
            ["dukefishron"] = new int[] { NPCID.DukeFishron },
            ["empressoflight"] = new int[] { NPCID.HallowBoss },
            ["moonlord"] = new int[] { NPCID.MoonLordCore },
        };

        public static void AddHelp(CommandCaller caller)
        {
            caller.Reply("-- World --");
            caller.Reply("admin time <day|noon|dusk|night|midnight|HH:MM>");
            caller.Reply("admin rain <on|off>   admin sandstorm <on|off>   admin slimerain <on|off>");
            caller.Reply("admin bloodmoon   admin eclipse   admin invade <goblins|frostlegion|pirates|martians|stop>");
            caller.Reply("admin boss <" + string.Join("|", Bosses.Keys) + "> <player>");
            caller.Reply("admin butcher   admin clearitems   admin clearprojectiles");
            caller.Reply("admin hardmode   admin settle   admin setspawn <player>");
        }

        private static void SyncWorld()
        {
            NetMessage.SendData(MessageID.WorldData);
        }

        private static bool TryParseTime(string text, out double hours)
        {
            hours = 0;
            switch (text.ToLowerInvariant())
            {
                case "day":
                case "dawn":
                    hours = 4.5;
                    return true;
                case "noon":
                    hours = 12;
                    return true;
                case "dusk":
                case "night":
                    hours = 19.5;
                    return true;
                case "midnight":
                    hours = 0;
                    return true;
            }

            string[] parts = text.Split(':');
            if (parts.Length != 2
                || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int h)
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int m)
                || h < 0 || h > 23 || m < 0 || m > 59)
                return false;

            hours = h + m / 60.0;
            return true;
        }

        private static void SetTime(double hours)
        {
            // Terraria day runs 4:30 -> 19:30 (54000 ticks), night 19:30 -> 4:30 (32400 ticks); 3600 ticks per in-game hour.
            if (hours >= 4.5 && hours < 19.5)
            {
                Main.dayTime = true;
                Main.time = (hours - 4.5) * 3600.0;
            }
            else
            {
                Main.dayTime = false;
                double nightHours = hours >= 19.5 ? hours - 19.5 : hours + 4.5;
                Main.time = nightHours * 3600.0;
            }
            SyncWorld();
        }

        private static bool ParseOnOff(string value, out bool on)
        {
            on = false;
            if (value.Equals("on", StringComparison.OrdinalIgnoreCase)) { on = true; return true; }
            if (value.Equals("off", StringComparison.OrdinalIgnoreCase)) { on = false; return true; }
            return false;
        }

        public static bool TryHandle(string sub, CommandCaller caller, string[] args)
        {
            switch (sub)
            {
                case "time":
                {
                    if (args.Length != 2 || !TryParseTime(args[1], out double hours))
                    {
                        caller.Reply("Usage: admin time <day|noon|dusk|night|midnight|HH:MM>");
                        return true;
                    }
                    SetTime(hours);
                    caller.Reply($"Time set to {args[1]}.");
                    return true;
                }

                case "rain":
                {
                    if (args.Length != 2 || !ParseOnOff(args[1], out bool on))
                    {
                        caller.Reply("Usage: admin rain <on|off>");
                        return true;
                    }
                    if (on) Main.StartRain(); else Main.StopRain();
                    SyncWorld();
                    caller.Reply(on ? "Rain started." : "Rain stopped.");
                    return true;
                }

                case "sandstorm":
                {
                    if (args.Length != 2 || !ParseOnOff(args[1], out bool on))
                    {
                        caller.Reply("Usage: admin sandstorm <on|off>");
                        return true;
                    }
                    if (on) Sandstorm.StartSandstorm(); else Sandstorm.StopSandstorm();
                    SyncWorld();
                    caller.Reply(on ? "Sandstorm started." : "Sandstorm stopped.");
                    return true;
                }

                case "slimerain":
                {
                    if (args.Length != 2 || !ParseOnOff(args[1], out bool on))
                    {
                        caller.Reply("Usage: admin slimerain <on|off>");
                        return true;
                    }
                    if (on) Main.StartSlimeRain(true); else Main.StopSlimeRain(true);
                    SyncWorld();
                    caller.Reply(on ? "Slime rain started." : "Slime rain stopped.");
                    return true;
                }

                case "bloodmoon":
                {
                    if (Main.dayTime)
                    {
                        caller.Reply("A blood moon can only start at night. Use 'admin time night' first.");
                        return true;
                    }
                    Main.bloodMoon = true;
                    SyncWorld();
                    caller.Reply("Blood moon started.");
                    return true;
                }

                case "eclipse":
                {
                    if (!Main.dayTime)
                    {
                        caller.Reply("A solar eclipse can only start during the day. Use 'admin time noon' first.");
                        return true;
                    }
                    Main.eclipse = true;
                    SyncWorld();
                    caller.Reply("Solar eclipse started.");
                    return true;
                }

                case "invade":
                {
                    if (args.Length != 2)
                    {
                        caller.Reply("Usage: admin invade <goblins|frostlegion|pirates|martians|stop>");
                        return true;
                    }
                    int invasion;
                    switch (args[1].ToLowerInvariant())
                    {
                        case "goblins": invasion = InvasionID.GoblinArmy; break;
                        case "frostlegion": invasion = InvasionID.SnowLegion; break;
                        case "pirates": invasion = InvasionID.PirateInvasion; break;
                        case "martians": invasion = InvasionID.MartianMadness; break;
                        case "stop":
                            Main.invasionType = 0;
                            Main.invasionSize = 0;
                            SyncWorld();
                            caller.Reply("Invasion stopped.");
                            return true;
                        default:
                            caller.Reply("Usage: admin invade <goblins|frostlegion|pirates|martians|stop>");
                            return true;
                    }
                    Main.StartInvasion(invasion);
                    SyncWorld();
                    caller.Reply($"Started invasion: {args[1].ToLowerInvariant()}.");
                    return true;
                }

                case "boss":
                {
                    if (args.Length != 3 || !Bosses.TryGetValue(args[1], out int[] bossTypes))
                    {
                        caller.Reply("Usage: admin boss <name> <player>. Names: " + string.Join(", ", Bosses.Keys));
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[2]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[2]}'.");
                        return true;
                    }
                    foreach (int type in bossTypes)
                        NPC.SpawnOnPlayer(target.whoAmI, type);
                    caller.Reply($"Summoned {args[1].ToLowerInvariant()} on {target.name}.");
                    return true;
                }

                case "butcher":
                {
                    int killed = 0;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (!npc.active || npc.friendly || npc.townNPC || npc.lifeMax <= 5)
                            continue;

                        npc.life = 0;
                        npc.HitEffect();
                        npc.active = false;
                        NetMessage.SendData(MessageID.SyncNPC, number: i);
                        killed++;
                    }
                    caller.Reply($"Killed {killed} hostile NPC(s).");
                    return true;
                }

                case "clearitems":
                {
                    int cleared = 0;
                    for (int i = 0; i < Main.maxItems; i++)
                    {
                        if (!Main.item[i].active)
                            continue;
                        Main.item[i].active = false;
                        NetMessage.SendData(MessageID.SyncItem, number: i);
                        cleared++;
                    }
                    caller.Reply($"Cleared {cleared} item(s).");
                    return true;
                }

                case "clearprojectiles":
                {
                    int cleared = 0;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (!Main.projectile[i].active)
                            continue;
                        Main.projectile[i].Kill();
                        cleared++;
                    }
                    caller.Reply($"Cleared {cleared} projectile(s).");
                    return true;
                }

                case "hardmode":
                {
                    if (Main.hardMode)
                    {
                        caller.Reply("The world is already in Hardmode.");
                        return true;
                    }
                    WorldGen.StartHardmode();
                    SyncWorld();
                    caller.Reply("Hardmode started.");
                    return true;
                }

                case "settle":
                {
                    Liquid.StartPanic();
                    caller.Reply("Settling liquids.");
                    return true;
                }

                case "setspawn":
                {
                    if (args.Length != 2)
                    {
                        caller.Reply("Usage: admin setspawn <player>");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[1]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return true;
                    }
                    Main.spawnTileX = (int)(target.Center.X / 16f);
                    Main.spawnTileY = (int)(target.Bottom.Y / 16f) - 1;
                    SyncWorld();
                    caller.Reply($"World spawn set to tile {Main.spawnTileX}, {Main.spawnTileY}.");
                    return true;
                }
            }

            return false;
        }
    }
}
