using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.IO;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.Localization;

namespace TMLAdmin
{
    public class AdminCommand : ModCommand
    {
        public override string Command => "admin";
        public override CommandType Type => CommandType.Console;
        public override string Description => "Console-only server administration commands.";
        public override bool IsCaseSensitive => true;
        public override string Usage => "admin <help|players|announce|heal|kick|spawn|kill|tp|color|tag|tagclear|give|summonslots|save> [arguments]";

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
                caller.Reply("admin spawn <npc-id> [amount]");
                caller.Reply("admin kill <player>");
                caller.Reply("admin tp <player> <destination-player>");
                caller.Reply("admin color <player> <#RRGGBB>");
                caller.Reply("admin tag <player> <tag>");
                caller.Reply("admin tagclear <player>");
                caller.Reply("admin give <player> <item-id> [amount]");
                caller.Reply("admin summonslots <player> <extra-slots>");
                caller.Reply("admin save");
                ModerationCommands.AddHelp(caller);
                WorldCommands.AddHelp(caller);
                PlayerCommands.AddHelp(caller);
                return;
            }

            switch (args[0].ToLowerInvariant())
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


                case "spawn":
                    if (args.Length < 2 || args.Length > 3 || !int.TryParse(args[1], out int npcType))
                    {
                        caller.Reply("Usage: admin spawn <npc-id> [amount]");
                        return;
                    }
                    int amount = 1;
                    if (args.Length == 3 && (!int.TryParse(args[2], out amount) || amount < 1 || amount > 20))
                    {
                        caller.Reply("Amount must be between 1 and 20.");
                        return;
                    }
                    if (npcType <= 0 || npcType >= NPCLoader.NPCCount)
                    {
                        caller.Reply($"NPC ID must be between 1 and {NPCLoader.NPCCount - 1}.");
                        return;
                    }
                    int spawned = 0;
                    int spawnX = Main.spawnTileX * 16;
                    int spawnY = Main.spawnTileY * 16;
                    for (int i = 0; i < amount; i++)
                    {
                        int index = NPC.NewNPC(new EntitySource_Misc("TMLAdmin"), spawnX, spawnY, npcType);
                        if (index >= 0 && index < Main.maxNPCs)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, number: index);
                            spawned++;
                        }
                    }
                    caller.Reply($"Spawned {spawned} NPC(s) with ID {npcType} near world spawn.");
                    break;

                case "kill":
                    if (args.Length != 2)
                    {
                        caller.Reply("Usage: admin kill <player>");
                        return;
                    }
                    Player killTarget = FindPlayer(args[1]);
                    if (killTarget == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return;
                    }
                    PlayerDeathReason deathReason = PlayerDeathReason.ByCustomReason($"{killTarget.name} was removed by a server administrator.");
                    killTarget.Hurt(deathReason, 1000000, 0, pvp: false, quiet: false, cooldownCounter: -1, dodgeable: false);
                    if (killTarget.dead)
                        caller.Reply($"{killTarget.name} was killed.");
                    else
                        caller.Reply($"Could not kill {killTarget.name}; another mod or an immunity effect may have prevented the damage.");
                    break;

                case "tp":
                case "teleport":
                    if (args.Length != 3)
                    {
                        caller.Reply("Usage: admin tp <player> <destination-player>");
                        return;
                    }
                    Player teleportTarget = FindPlayer(args[1]);
                    if (teleportTarget == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return;
                    }
                    Player destinationTarget = FindPlayer(args[2]);
                    if (destinationTarget == null)
                    {
                        caller.Reply($"Could not find online player '{args[2]}'.");
                        return;
                    }
                    if (teleportTarget.whoAmI == destinationTarget.whoAmI)
                    {
                        caller.Reply("A player cannot be teleported to themselves.");
                        return;
                    }
                    teleportTarget.Teleport(destinationTarget.position, 0);
                    NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0,
                        teleportTarget.whoAmI, teleportTarget.position.X, teleportTarget.position.Y, 0);
                    caller.Reply($"Teleported {teleportTarget.name} to {destinationTarget.name}.");
                    break;

                case "color":
                    if (args.Length != 3 || !ChatFormattingSystem.TryParseHexColor(args[2], out Color chatColor))
                    {
                        caller.Reply("Usage: admin color <player> <#RRGGBB> (example: #FF8800)");
                        return;
                    }
                    Player colorTarget = FindPlayer(args[1]);
                    if (colorTarget == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return;
                    }
                    ChatFormattingSystem.SetColor(colorTarget.name, chatColor);
                    caller.Reply($"Set {colorTarget.name}'s chat color to {args[2]}.");
                    break;

                case "tag":
                    if (args.Length < 3)
                    {
                        caller.Reply("Usage: admin tag <player> <tag>");
                        return;
                    }
                    Player tagTarget = FindPlayer(args[1]);
                    if (tagTarget == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return;
                    }
                    string tagText = string.Join(" ", args, 2, args.Length - 2).Trim();
                    if (!ChatFormattingSystem.IsValidTag(tagText))
                    {
                        caller.Reply("Tags must be 1-20 characters and cannot contain brackets or line breaks.");
                        return;
                    }
                    ChatFormattingSystem.SetTag(tagTarget.name, tagText);
                    caller.Reply($"Set {tagTarget.name}'s chat tag to [{tagText}].");
                    break;

                case "tagclear":
                    if (args.Length != 2)
                    {
                        caller.Reply("Usage: admin tagclear <player>");
                        return;
                    }
                    Player clearTagTarget = FindPlayer(args[1]);
                    if (clearTagTarget == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return;
                    }
                    ChatFormattingSystem.ClearTag(clearTagTarget.name);
                    caller.Reply($"Cleared {clearTagTarget.name}'s chat tag.");
                    break;

                case "give":
                    if (args.Length < 3 || args.Length > 4 || !int.TryParse(args[2], out int giveItemType))
                    {
                        caller.Reply("Usage: admin give <player> <item-id> [amount]");
                        return;
                    }
                    Player giveTarget = FindPlayer(args[1]);
                    if (giveTarget == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return;
                    }
                    if (giveItemType <= 0 || giveItemType >= ItemLoader.ItemCount)
                    {
                        caller.Reply($"Item ID must be between 1 and {ItemLoader.ItemCount - 1}.");
                        return;
                    }
                    Item giveSample = ContentSamples.ItemsByType[giveItemType];
                    int giveMaxStack = Math.Max(1, giveSample.maxStack);
                    int giveAmount = 1;
                    if (args.Length == 4 && (!int.TryParse(args[3], out giveAmount) || giveAmount < 1 || giveAmount > giveMaxStack))
                    {
                        caller.Reply($"Amount must be between 1 and {giveMaxStack} for {giveSample.Name}.");
                        return;
                    }
                    int giveIndex = Item.NewItem(new EntitySource_Gift(giveTarget), (int)giveTarget.position.X, (int)giveTarget.position.Y, giveTarget.width, giveTarget.height, giveItemType, giveAmount, false, 0, true);
                    if (giveIndex < 0 || giveIndex >= Main.maxItems)
                    {
                        caller.Reply("Could not spawn the item (world item limit reached).");
                        return;
                    }
                    Main.item[giveIndex].playerIndexTheItemIsReservedFor = giveTarget.whoAmI;
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, giveIndex, 1f);
                    caller.Reply($"Gave {giveAmount}x {giveSample.Name} (ID {giveItemType}) to {giveTarget.name}.");
                    break;

                case "summonslots":
                    if (args.Length != 3 || !int.TryParse(args[2], out int extraSlots) || extraSlots < 0 || extraSlots > SummonSlotPlayer.MaxBonus)
                    {
                        caller.Reply($"Usage: admin summonslots <player> <extra-slots 0-{SummonSlotPlayer.MaxBonus}> (0 resets)");
                        return;
                    }
                    Player slotsTarget = FindPlayer(args[1]);
                    if (slotsTarget == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return;
                    }
                    slotsTarget.GetModPlayer<SummonSlotPlayer>().BonusMinions = extraSlots;
                    ModPacket slotsPacket = Mod.GetPacket();
                    slotsPacket.Write(TMLAdmin.PacketSetSummonSlots);
                    slotsPacket.Write(extraSlots);
                    slotsPacket.Send(slotsTarget.whoAmI);
                    caller.Reply($"{slotsTarget.name} now has {extraSlots} extra summon slot(s) until they leave or the server restarts.");
                    break;

                case "save":
                    WorldFile.SaveWorld();
                    caller.Reply("World save requested.");
                    break;

                default:
                    string sub = args[0].ToLowerInvariant();
                    if (ModerationCommands.TryHandle(sub, caller, args)
                        || WorldCommands.TryHandle(sub, caller, args)
                        || PlayerCommands.TryHandle(sub, caller, args))
                        break;
                    caller.Reply($"Unknown subcommand '{args[0]}'. Use 'admin help'.");
                    break;
            }
        }

        internal static Player FindPlayer(string name)
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
