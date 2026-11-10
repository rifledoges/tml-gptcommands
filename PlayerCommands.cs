using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>godmode, buff, clearbuffs, tpspawn</summary>
    public static class PlayerCommands
    {
        public static void AddHelp(CommandCaller caller)
        {
            caller.Reply("-- Player tools --");
            caller.Reply("admin godmode <player> [on|off]");
            caller.Reply("admin buff <player> <buff-id> [seconds]   admin clearbuffs <player>");
            caller.Reply("admin tpspawn <player>");
        }

        public static bool TryHandle(string sub, CommandCaller caller, string[] args)
        {
            switch (sub)
            {
                case "godmode":
                {
                    if (args.Length < 2 || args.Length > 3)
                    {
                        caller.Reply("Usage: admin godmode <player> [on|off]");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[1]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return true;
                    }
                    GodModePlayer god = target.GetModPlayer<GodModePlayer>();
                    bool enable = !god.God;
                    if (args.Length == 3)
                    {
                        if (args[2].Equals("on", StringComparison.OrdinalIgnoreCase)) enable = true;
                        else if (args[2].Equals("off", StringComparison.OrdinalIgnoreCase)) enable = false;
                        else
                        {
                            caller.Reply("Usage: admin godmode <player> [on|off]");
                            return true;
                        }
                    }
                    god.God = enable;
                    ModPacket packet = ModContent.GetInstance<TMLAdmin>().GetPacket();
                    packet.Write(TMLAdmin.PacketSetGodmode);
                    packet.Write(enable);
                    packet.Send(target.whoAmI);
                    caller.Reply($"Godmode {(enable ? "enabled" : "disabled")} for {target.name} until they leave or the server restarts.");
                    return true;
                }

                case "buff":
                {
                    if (args.Length < 3 || args.Length > 4 || !int.TryParse(args[2], out int buffType))
                    {
                        caller.Reply("Usage: admin buff <player> <buff-id> [seconds]");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[1]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return true;
                    }
                    if (buffType <= 0 || buffType >= BuffLoader.BuffCount)
                    {
                        caller.Reply($"Buff ID must be between 1 and {BuffLoader.BuffCount - 1}.");
                        return true;
                    }
                    int seconds = 60;
                    if (args.Length == 4 && (!int.TryParse(args[3], out seconds) || seconds < 1 || seconds > 3600))
                    {
                        caller.Reply("Seconds must be between 1 and 3600.");
                        return true;
                    }
                    int ticks = seconds * 60;
                    target.AddBuff(buffType, ticks);
                    NetMessage.SendData(MessageID.AddPlayerBuff, target.whoAmI, -1, null, target.whoAmI, buffType, ticks);
                    caller.Reply($"Gave {target.name} buff {buffType} for {seconds} second(s).");
                    return true;
                }

                case "clearbuffs":
                {
                    if (args.Length != 2)
                    {
                        caller.Reply("Usage: admin clearbuffs <player>");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[1]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return true;
                    }
                    for (int i = 0; i < Player.MaxBuffs; i++)
                    {
                        target.buffType[i] = 0;
                        target.buffTime[i] = 0;
                    }
                    NetMessage.SendData(MessageID.PlayerBuffs, -1, -1, null, target.whoAmI);
                    caller.Reply($"Cleared buffs for {target.name}.");
                    return true;
                }

                case "tpspawn":
                {
                    if (args.Length != 2)
                    {
                        caller.Reply("Usage: admin tpspawn <player>");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[1]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return true;
                    }
                    Vector2 spawn = new Vector2(Main.spawnTileX * 16f, (Main.spawnTileY - 3) * 16f);
                    target.Teleport(spawn, 0);
                    NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0,
                        target.whoAmI, target.position.X, target.position.Y, 0);
                    caller.Reply($"Teleported {target.name} to world spawn.");
                    return true;
                }
            }

            return false;
        }
    }
}
