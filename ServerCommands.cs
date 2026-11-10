using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>motd, autobroadcast, chatfilter, slowmode, lockdown, whitelist, shutdown, restart, backup</summary>
    public static class ServerCommands
    {
        public static void AddHelp(CommandCaller caller)
        {
            caller.Reply("-- Server --");
            caller.Reply("admin motd [<text> [color] | off]   admin autobroadcast <add|remove|list|interval|on|off>");
            caller.Reply("admin chatfilter <add|remove|list|clear> [word]   admin slowmode <seconds>");
            caller.Reply("admin lockdown [on|off]   admin whitelist <on|off|add|remove|list> [name]");
            caller.Reply("admin shutdown [minutes|cancel]   admin restart [minutes]   admin backup");
        }

        // Splits "text words... [color]" into the text and an optional trailing color spec.
        private static string ParseTextAndColor(string[] args, int start, out string colorSpec)
        {
            colorSpec = "";
            int end = args.Length;
            if (end - start > 1 && ChatFormattingSystem.TryParseColorSpec(args[end - 1], out Color[] _))
            {
                colorSpec = args[end - 1];
                end--;
            }
            return string.Join(" ", args, start, end - start).Trim();
        }

        private static bool? ParseOnOff(string value)
        {
            if (value.Equals("on", StringComparison.OrdinalIgnoreCase)) return true;
            if (value.Equals("off", StringComparison.OrdinalIgnoreCase)) return false;
            return null;
        }

        public static bool TryHandle(string sub, CommandCaller caller, string[] args)
        {
            var data = ModerationSystem.Data;

            switch (sub)
            {
                case "motd":
                {
                    if (args.Length == 1)
                    {
                        caller.Reply(string.IsNullOrEmpty(data.Motd) ? "No MOTD is set." : "MOTD: " + data.Motd + (data.MotdColor.Length > 0 ? $" (color {data.MotdColor})" : ""));
                        return true;
                    }
                    if (args.Length == 2 && args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                    {
                        data.Motd = "";
                        data.MotdColor = "";
                        ModerationSystem.Persist();
                        caller.Reply("MOTD removed.");
                        return true;
                    }
                    string text = ParseTextAndColor(args, 1, out string spec);
                    if (text.Length == 0 || text.Length > 200)
                    {
                        caller.Reply("The MOTD must be 1-200 characters. Usage: admin motd <text> [color]");
                        return true;
                    }
                    data.Motd = text;
                    data.MotdColor = spec;
                    ModerationSystem.Persist();
                    caller.Reply("MOTD set. Players see it a few seconds after joining.");
                    return true;
                }

                case "autobroadcast":
                {
                    string action = args.Length > 1 ? args[1].ToLowerInvariant() : "list";
                    switch (action)
                    {
                        case "add":
                        {
                            if (args.Length < 3)
                            {
                                caller.Reply("Usage: admin autobroadcast add <text> [color]");
                                return true;
                            }
                            string text = ParseTextAndColor(args, 2, out string spec);
                            if (text.Length == 0 || text.Length > 200)
                            {
                                caller.Reply("Messages must be 1-200 characters.");
                                return true;
                            }
                            data.Broadcasts.Add(new ModerationSystem.BroadcastEntry { Text = text, Color = spec });
                            ModerationSystem.Persist();
                            caller.Reply($"Added broadcast #{data.Broadcasts.Count}." + (data.BroadcastEnabled ? "" : " Use 'admin autobroadcast on' to start."));
                            return true;
                        }
                        case "remove":
                        {
                            if (args.Length != 3 || !int.TryParse(args[2], out int n) || n < 1 || n > data.Broadcasts.Count)
                            {
                                caller.Reply("Usage: admin autobroadcast remove <number> (see 'admin autobroadcast list')");
                                return true;
                            }
                            data.Broadcasts.RemoveAt(n - 1);
                            ModerationSystem.Persist();
                            caller.Reply($"Removed broadcast #{n}.");
                            return true;
                        }
                        case "list":
                        {
                            for (int i = 0; i < data.Broadcasts.Count; i++)
                                caller.Reply($"{i + 1}. {data.Broadcasts[i].Text}" + (data.Broadcasts[i].Color.Length > 0 ? $" ({data.Broadcasts[i].Color})" : ""));
                            caller.Reply($"Auto-broadcast is {(data.BroadcastEnabled ? "ON" : "OFF")}, every {data.BroadcastIntervalSeconds} seconds, {data.Broadcasts.Count} message(s).");
                            return true;
                        }
                        case "interval":
                        {
                            if (args.Length != 3 || !int.TryParse(args[2], out int seconds) || seconds < 30 || seconds > 86400)
                            {
                                caller.Reply("Usage: admin autobroadcast interval <seconds 30-86400>");
                                return true;
                            }
                            data.BroadcastIntervalSeconds = seconds;
                            ModerationSystem.Persist();
                            caller.Reply($"Auto-broadcast interval set to {seconds} seconds.");
                            return true;
                        }
                        case "on":
                        case "off":
                        {
                            data.BroadcastEnabled = action == "on";
                            ModerationSystem.Persist();
                            caller.Reply($"Auto-broadcast turned {action}.");
                            return true;
                        }
                        default:
                            caller.Reply("Usage: admin autobroadcast <add|remove|list|interval|on|off>");
                            return true;
                    }
                }

                case "chatfilter":
                {
                    string action = args.Length > 1 ? args[1].ToLowerInvariant() : "list";
                    switch (action)
                    {
                        case "add":
                        {
                            if (args.Length != 3 || args[2].Length < 2)
                            {
                                caller.Reply("Usage: admin chatfilter add <word> (at least 2 characters)");
                                return true;
                            }
                            if (data.ChatFilter.Any(w => w.Equals(args[2], StringComparison.OrdinalIgnoreCase)))
                            {
                                caller.Reply("That word is already filtered.");
                                return true;
                            }
                            data.ChatFilter.Add(args[2]);
                            ModerationSystem.Persist();
                            caller.Reply($"'{args[2]}' will now be replaced with asterisks in chat.");
                            return true;
                        }
                        case "remove":
                        {
                            if (args.Length != 3)
                            {
                                caller.Reply("Usage: admin chatfilter remove <word>");
                                return true;
                            }
                            int removed = data.ChatFilter.RemoveAll(w => w.Equals(args[2], StringComparison.OrdinalIgnoreCase));
                            if (removed > 0)
                                ModerationSystem.Persist();
                            caller.Reply(removed > 0 ? $"Removed '{args[2]}' from the filter." : "That word is not in the filter.");
                            return true;
                        }
                        case "clear":
                        {
                            data.ChatFilter.Clear();
                            ModerationSystem.Persist();
                            caller.Reply("Chat filter cleared.");
                            return true;
                        }
                        case "list":
                        {
                            caller.Reply(data.ChatFilter.Count == 0 ? "The chat filter is empty." : "Filtered words: " + string.Join(", ", data.ChatFilter));
                            return true;
                        }
                        default:
                            caller.Reply("Usage: admin chatfilter <add|remove|list|clear> [word]");
                            return true;
                    }
                }

                case "slowmode":
                {
                    if (args.Length != 2 || !int.TryParse(args[1], out int seconds) || seconds < 0 || seconds > 600)
                    {
                        caller.Reply($"Usage: admin slowmode <seconds 0-600> (0 = off; currently {ModerationSystem.SlowmodeSeconds})");
                        return true;
                    }
                    ModerationSystem.SlowmodeSeconds = seconds;
                    caller.Reply(seconds == 0 ? "Slow mode turned off." : $"Slow mode on: one message every {seconds} second(s) per player, until the server restarts.");
                    return true;
                }

                case "lockdown":
                {
                    bool enable = !ModerationSystem.Lockdown;
                    if (args.Length == 2)
                    {
                        bool? parsed = ParseOnOff(args[1]);
                        if (parsed == null)
                        {
                            caller.Reply("Usage: admin lockdown [on|off]");
                            return true;
                        }
                        enable = parsed.Value;
                    }
                    ModerationSystem.Lockdown = enable;
                    caller.Reply(enable
                        ? "Lockdown ON: new players are removed on joining (whitelisted names can still join). Players already online stay."
                        : "Lockdown OFF: players can join again.");
                    return true;
                }

                case "whitelist":
                {
                    string action = args.Length > 1 ? args[1].ToLowerInvariant() : "list";
                    switch (action)
                    {
                        case "on":
                        {
                            int added = 0;
                            for (int i = 0; i < Main.maxPlayers; i++)
                            {
                                Player p = Main.player[i];
                                if (p.active && !ModerationSystem.IsWhitelisted(p.name))
                                {
                                    data.Whitelist.Add(p.name);
                                    added++;
                                }
                            }
                            data.WhitelistEnabled = true;
                            ModerationSystem.Persist();
                            caller.Reply($"Whitelist ON. Added {added} player(s) who are online now so they are not removed. Names are not verified, so this only filters by character name.");
                            return true;
                        }
                        case "off":
                        {
                            data.WhitelistEnabled = false;
                            ModerationSystem.Persist();
                            caller.Reply("Whitelist OFF.");
                            return true;
                        }
                        case "add":
                        {
                            if (args.Length < 3)
                            {
                                caller.Reply("Usage: admin whitelist add <name>");
                                return true;
                            }
                            string name = string.Join(" ", args, 2, args.Length - 2);
                            if (ModerationSystem.IsWhitelisted(name))
                            {
                                caller.Reply($"{name} is already whitelisted.");
                                return true;
                            }
                            data.Whitelist.Add(name);
                            ModerationSystem.Persist();
                            caller.Reply($"Whitelisted {name}.");
                            return true;
                        }
                        case "remove":
                        {
                            if (args.Length < 3)
                            {
                                caller.Reply("Usage: admin whitelist remove <name>");
                                return true;
                            }
                            string name = string.Join(" ", args, 2, args.Length - 2);
                            int removed = data.Whitelist.RemoveAll(n => n.Equals(name, StringComparison.OrdinalIgnoreCase));
                            if (removed > 0)
                                ModerationSystem.Persist();
                            caller.Reply(removed > 0 ? $"Removed {name} from the whitelist." : $"{name} is not whitelisted.");
                            return true;
                        }
                        case "list":
                        {
                            foreach (string n in data.Whitelist)
                                caller.Reply(n);
                            caller.Reply($"Whitelist is {(data.WhitelistEnabled ? "ON" : "OFF")}, {data.Whitelist.Count} name(s).");
                            return true;
                        }
                        default:
                            caller.Reply("Usage: admin whitelist <on|off|add|remove|list> [name]");
                            return true;
                    }
                }

                case "shutdown":
                case "restart":
                {
                    bool restart = sub == "restart";
                    if (args.Length == 2 && args[1].Equals("cancel", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!ServerSystem.ShutdownPending)
                            caller.Reply("Nothing is scheduled.");
                        else
                        {
                            ServerSystem.CancelShutdown();
                            caller.Reply("Cancelled.");
                        }
                        return true;
                    }
                    int minutes = 1;
                    if (args.Length > 2 || (args.Length == 2 && (!int.TryParse(args[1], out minutes) || minutes < 0 || minutes > 1440)))
                    {
                        caller.Reply($"Usage: admin {sub} [minutes 0-1440 | cancel] (default 1 minute, 0 = immediately)");
                        return true;
                    }
                    ServerSystem.StartShutdown(minutes, restart);
                    caller.Reply($"{(restart ? "Restart" : "Shutdown")} scheduled in {minutes} minute(s). The world is saved first. Use 'admin {sub} cancel' to stop it."
                        + (restart ? " The server only comes back up if your host (e.g. BisectHosting) is set to auto-restart it." : ""));
                    return true;
                }

                case "backup":
                {
                    try
                    {
                        WorldFile.SaveWorld();
                        string world = Main.worldPathName;
                        string dir = Path.Combine(Path.GetDirectoryName(world), "TMLAdmin-Backups");
                        Directory.CreateDirectory(dir);
                        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                        string baseName = Path.GetFileNameWithoutExtension(world);
                        string target = Path.Combine(dir, $"{baseName}_{stamp}.wld");
                        File.Copy(world, target, true);
                        string twld = Path.ChangeExtension(world, ".twld");
                        if (File.Exists(twld))
                            File.Copy(twld, Path.Combine(dir, $"{baseName}_{stamp}.twld"), true);
                        caller.Reply("Backup saved to " + target);
                    }
                    catch (Exception e)
                    {
                        caller.Reply("Backup failed: " + e.Message);
                    }
                    return true;
                }
            }

            return false;
        }
    }
}
