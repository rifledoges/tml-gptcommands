using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>ban, unban, banlist, mute, unmute, mutelist, whois, slap</summary>
    public static class ModerationCommands
    {
        public static void AddHelp(CommandCaller caller)
        {
            caller.Reply("-- Moderation --");
            caller.Reply("admin ban <player> [reason]   admin banname <name> [reason]");
            caller.Reply("admin unban <name|ip>   admin banlist");
            caller.Reply("admin mute <player>   admin unmute <name>   admin mutelist");
            caller.Reply("admin whois <player>   admin slap <player> [damage]");
        }

        public static bool TryHandle(string sub, CommandCaller caller, string[] args)
        {
            switch (sub)
            {
                case "ban":
                {
                    if (args.Length < 2)
                    {
                        caller.Reply("Usage: admin ban <player> [reason]");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[1]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'. Use 'admin banname <name> [reason]' for offline players.");
                        return true;
                    }
                    string reason = args.Length > 2 ? string.Join(" ", args, 2, args.Length - 2) : "";
                    string ip = ModerationSystem.GetIp(target.whoAmI);
                    ModerationSystem.AddBan(target.name, ip, reason);
                    string kickText = reason.Length > 0 ? "Banned: " + reason : "Banned.";
                    NetMessage.SendData(Terraria.ID.MessageID.Kick, target.whoAmI, -1, NetworkText.FromLiteral(kickText));
                    caller.Reply($"Banned {target.name}" + (ip.Length > 0 ? $" (IP {ip})" : "") + ".");
                    return true;
                }

                case "banname":
                {
                    if (args.Length < 2)
                    {
                        caller.Reply("Usage: admin banname <name> [reason]");
                        return true;
                    }
                    string reason = args.Length > 2 ? string.Join(" ", args, 2, args.Length - 2) : "";
                    ModerationSystem.AddBan(args[1], "", reason);
                    caller.Reply($"Banned name '{args[1]}'. They will be removed whenever they join.");
                    return true;
                }

                case "unban":
                {
                    if (args.Length != 2)
                    {
                        caller.Reply("Usage: admin unban <name|ip>");
                        return true;
                    }
                    int removed = ModerationSystem.RemoveBan(args[1]);
                    caller.Reply(removed > 0 ? $"Removed {removed} ban(s) matching '{args[1]}'." : $"No ban matches '{args[1]}'.");
                    return true;
                }

                case "banlist":
                {
                    var bans = ModerationSystem.Bans;
                    foreach (var ban in bans)
                    {
                        string line = ban.Name.Length > 0 ? ban.Name : "(no name)";
                        if (ban.Ip.Length > 0)
                            line += $" [{ban.Ip}]";
                        if (ban.Reason.Length > 0)
                            line += $" - {ban.Reason}";
                        caller.Reply(line);
                    }
                    caller.Reply($"Bans: {bans.Count}");
                    return true;
                }

                case "mute":
                {
                    if (args.Length != 2)
                    {
                        caller.Reply("Usage: admin mute <player>");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[1]);
                    string name = target != null ? target.name : args[1];
                    caller.Reply(ModerationSystem.Mute(name) ? $"Muted {name}." : $"{name} is already muted.");
                    return true;
                }

                case "unmute":
                {
                    if (args.Length != 2)
                    {
                        caller.Reply("Usage: admin unmute <name>");
                        return true;
                    }
                    caller.Reply(ModerationSystem.Unmute(args[1]) ? $"Unmuted {args[1]}." : $"{args[1]} is not muted.");
                    return true;
                }

                case "mutelist":
                {
                    foreach (string name in ModerationSystem.Mutes)
                        caller.Reply(name);
                    caller.Reply($"Muted players: {ModerationSystem.Mutes.Count}");
                    return true;
                }

                case "whois":
                {
                    if (args.Length != 2)
                    {
                        caller.Reply("Usage: admin whois <player>");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[1]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return true;
                    }
                    caller.Reply($"{target.name} (slot {target.whoAmI})");
                    caller.Reply($"IP: {ModerationSystem.GetIp(target.whoAmI)}");
                    caller.Reply($"Life: {target.statLife}/{target.statLifeMax2}  Mana: {target.statMana}/{target.statManaMax2}  Summon slots: {target.maxMinions}");
                    caller.Reply($"Position (tiles): {(int)(target.Center.X / 16f)}, {(int)(target.Center.Y / 16f)}");
                    caller.Reply($"Muted: {(ModerationSystem.IsMuted(target.name) ? "yes" : "no")}  Banned: {(ModerationSystem.FindBan(target.name, ModerationSystem.GetIp(target.whoAmI)) != null ? "yes" : "no")}");
                    return true;
                }

                case "slap":
                {
                    if (args.Length < 2 || args.Length > 3)
                    {
                        caller.Reply("Usage: admin slap <player> [damage]");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[1]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return true;
                    }
                    int damage = 5;
                    if (args.Length == 3 && (!int.TryParse(args[2], out damage) || damage < 1 || damage > 1000))
                    {
                        caller.Reply("Damage must be between 1 and 1000.");
                        return true;
                    }
                    DamageHelper.Send(target, damage, $"{target.name} was slapped by a server administrator.", false);
                    caller.Reply($"Slapped {target.name} for {damage} damage.");
                    return true;
                }
            }

            return false;
        }
    }
}
