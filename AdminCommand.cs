using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.Localization;

namespace TMLAdmin
{
    public class AdminCommand : ModCommand
    {
        public override string Command => "admin";
        public override CommandType Type => CommandType.Console;
        public override string Description => "Console-only server administration commands.";
        public override string Usage => "admin <help|players|announce|heal|kick|save> [arguments]";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (caller.CommandType != CommandType.Console || Main.netMode != NetmodeID.Server)
            {
                caller.Reply("This command is available only from the dedicated server console.");
                return;
            }

            if (args.Length == 0 || args[0] == "help")
            {
                caller.Reply("TML Admin commands:");
                caller.Reply("admin players");
                caller.Reply("admin announce <message>");
                caller.Reply("admin heal <player>");
                caller.Reply("admin kick <player> [reason]");
                caller.Reply("admin save");
                return;
            }

            switch (args[0])
            {
                case "players":
                    int count = 0;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p.active)
                        {
                            caller.Reply($"{p.name} (slot {p.whoAmI})");
                            count++;
                        }
                    }
                    caller.Reply($"Online players: {count}");
                    break;

                case "announce":
                    if (args.Length < 2)
                    {
                        caller.Reply("Usage: admin announce <message>");
                        return;
                    }
                    string announcement = string.Join(" ", args, 1, args.Length - 1);
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(announcement), Color.White);
                    caller.Reply("Announcement sent.");
                    break;

                case "heal":
                    if (args.Length != 2)
                    {
                        caller.Reply("Usage: admin heal <player>");
                        return;
                    }
                    Player healTarget = FindPlayer(args[1]);
                    if (healTarget == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return;
                    }
                    healTarget.statLife = healTarget.statLifeMax2;
                    healTarget.statMana = healTarget.statManaMax2;
                    NetMessage.SendData(MessageID.PlayerLifeMana, number: healTarget.whoAmI);
                    caller.Reply($"Restored life and mana for {healTarget.name}.");
                    break;

                case "kick":
                    if (args.Length < 2)
                    {
                        caller.Reply("Usage: admin kick <player> [reason]");
                        return;
                    }
                    Player kickTarget = FindPlayer(args[1]);
                    if (kickTarget == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return;
                    }
                    string reason = args.Length > 2 ? string.Join(" ", args, 2, args.Length - 2) : "Removed by server administrator.";
                    NetMessage.SendData(MessageID.Kick, kickTarget.whoAmI, -1, NetworkText.FromLiteral(reason));
                    caller.Reply($"Kicked {kickTarget.name}.");
                    break;

                case "save":
                    WorldFile.SaveWorld();
                    caller.Reply("World save requested.");
                    break;

                default:
                    caller.Reply($"Unknown subcommand '{args[0]}'. Use 'admin help'.");
                    break;
            }
        }

        private static Player FindPlayer(string name)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && player.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return player;
            }
            return null;
        }
    }
}
