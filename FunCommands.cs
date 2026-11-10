using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>freeze, clearinv, rocket, clone, lightning, meteor, spawnrate, pvp, event</summary>
    public static class FunCommands
    {
        public static void AddHelp(CommandCaller caller)
        {
            caller.Reply("-- Fun --");
            caller.Reply("admin freeze <player> [on|off]   admin falldamage <player> [on|off]   admin accessoryslots <player> <0-5>   admin clearinv <player>   admin rocket <player> [power 1-60]");
            caller.Reply("admin clone <player>   admin lightning <player> [damage]   admin meteor");
            caller.Reply("admin spawnrate <multiplier 0-10>   admin pvp <player> <on|off>");
            caller.Reply("admin event bossrush <player> [pre|hard|all]   admin event stop");
            caller.Reply("admin giveall <item-id> [amount]");
        }

        private static Player Target(CommandCaller caller, string[] args, string usage)
        {
            if (args.Length < 2)
            {
                caller.Reply("Usage: " + usage);
                return null;
            }
            Player target = AdminCommand.FindPlayer(args[1]);
            if (target == null)
                caller.Reply($"Could not find online player '{args[1]}'.");
            return target;
        }

        private static void SendPacket(Player target, byte type, Action<ModPacket> write)
        {
            ModPacket packet = ModContent.GetInstance<TMLAdmin>().GetPacket();
            packet.Write(type);
            write?.Invoke(packet);
            packet.Send(target.whoAmI);
        }

        public static bool TryHandle(string sub, CommandCaller caller, string[] args)
        {
            switch (sub)
            {
                case "freeze":
                {
                    Player target = Target(caller, args, "admin freeze <player> [on|off]");
                    if (target == null)
                        return true;
                    EffectsPlayer effects = target.GetModPlayer<EffectsPlayer>();
                    bool enable = !effects.Frozen;
                    if (args.Length == 3)
                    {
                        if (args[2].Equals("on", StringComparison.OrdinalIgnoreCase)) enable = true;
                        else if (args[2].Equals("off", StringComparison.OrdinalIgnoreCase)) enable = false;
                        else
                        {
                            caller.Reply("Usage: admin freeze <player> [on|off]");
                            return true;
                        }
                    }
                    effects.Frozen = enable;
                    SendPacket(target, TMLAdmin.PacketFreeze, p => p.Write(enable));
                    caller.Reply($"{target.name} is now {(enable ? "frozen" : "unfrozen")}.");
                    return true;
                }

                case "falldamage":
                {
                    Player target = Target(caller, args, "admin falldamage <player> [on|off]");
                    if (target == null)
                        return true;
                    EffectsPlayer effects = target.GetModPlayer<EffectsPlayer>();
                    bool enabled = effects.FallDamageDisabled; // no argument flips the current state
                    if (args.Length == 3)
                    {
                        if (args[2].Equals("on", StringComparison.OrdinalIgnoreCase)) enabled = true;
                        else if (args[2].Equals("off", StringComparison.OrdinalIgnoreCase)) enabled = false;
                        else
                        {
                            caller.Reply("Usage: admin falldamage <player> [on|off]");
                            return true;
                        }
                    }
                    effects.FallDamageDisabled = !enabled;
                    SendPacket(target, TMLAdmin.PacketSetFallDamage, p => p.Write(enabled));
                    caller.Reply($"Fall damage {(enabled ? "enabled" : "disabled")} for {target.name} until they leave or the server restarts.");
                    return true;
                }

                case "accessoryslots":
                {
                    if (args.Length != 3 || !int.TryParse(args[2], out int slots) || slots < 0 || slots > AccessorySlotPlayer.MaxBonus)
                    {
                        caller.Reply($"Usage: admin accessoryslots <player> <extra-slots 0-{AccessorySlotPlayer.MaxBonus}> (0 resets)");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[1]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return true;
                    }
                    AccessorySlotPlayer.Apply(target, slots);
                    ModerationSystem.SetSavedAccessorySlots(target.name, slots);
                    caller.Reply($"{target.name} now has {slots} extra accessory slot(s). This is saved and re-applied whenever they join. Tell them to empty extra slots before you lower the number.");
                    return true;
                }

                case "clearinv":
                {
                    Player target = Target(caller, args, "admin clearinv <player>");
                    if (target == null)
                        return true;
                    SendPacket(target, TMLAdmin.PacketClearInventory, null);
                    caller.Reply($"Cleared {target.name}'s inventory.");
                    return true;
                }

                case "rocket":
                {
                    Player target = Target(caller, args, "admin rocket <player> [power 1-60]");
                    if (target == null)
                        return true;
                    int power = 25;
                    if (args.Length == 3 && (!int.TryParse(args[2], out power) || power < 1 || power > 60))
                    {
                        caller.Reply("Power must be between 1 and 60.");
                        return true;
                    }
                    SendPacket(target, TMLAdmin.PacketRocket, p => p.Write((float)power));
                    caller.Reply($"Launched {target.name} (power {power}). Fall damage is disabled for 10 seconds.");
                    return true;
                }

                case "clone":
                {
                    Player target = Target(caller, args, "admin clone <player>");
                    if (target == null)
                        return true;
                    Item held = target.HeldItem;
                    if (held == null || held.IsAir)
                    {
                        caller.Reply($"{target.name} is not holding an item.");
                        return true;
                    }
                    int index = Item.NewItem(new EntitySource_Gift(target), (int)target.position.X, (int)target.position.Y, target.width, target.height, held.type, held.stack, false, held.prefix, true);
                    if (index < 0 || index >= Main.maxItems)
                    {
                        caller.Reply("Could not spawn the item (world item limit reached).");
                        return true;
                    }
                    Main.item[index].playerIndexTheItemIsReservedFor = target.whoAmI;
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1f);
                    caller.Reply($"Cloned {held.Name} x{held.stack} for {target.name}.");
                    return true;
                }

                case "lightning":
                {
                    Player target = Target(caller, args, "admin lightning <player> [damage]");
                    if (target == null)
                        return true;
                    int damage = 50;
                    if (args.Length == 3 && (!int.TryParse(args[2], out damage) || damage < 1 || damage > 1000))
                    {
                        caller.Reply("Damage must be between 1 and 1000.");
                        return true;
                    }
                    Projectile.NewProjectile(new EntitySource_Misc("TMLAdmin"), target.Center, new Vector2(0f, 4f), ProjectileID.DD2LightningBugZap, 0, 0f, Main.myPlayer);
                    PlayerDeathReason reason = PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral($"{target.name} was struck by lightning."));
                    target.Hurt(reason, damage, 0, pvp: false, quiet: false, cooldownCounter: -1, dodgeable: false);
                    caller.Reply($"Struck {target.name} with lightning for {damage} damage.");
                    return true;
                }

                case "meteor":
                {
                    WorldGen.dropMeteor();
                    Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("A meteorite has landed somewhere in the world!"), new Color(50, 255, 130));
                    caller.Reply("Dropped a meteorite.");
                    return true;
                }

                case "spawnrate":
                {
                    if (args.Length != 2 || !float.TryParse(args[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float multiplier)
                        || multiplier < 0f || multiplier > 10f)
                    {
                        caller.Reply($"Usage: admin spawnrate <multiplier 0-10> (current: {SpawnRateNpc.Multiplier}; 1 = normal, 0 = no spawns)");
                        return true;
                    }
                    SpawnRateNpc.Multiplier = multiplier;
                    caller.Reply($"Enemy spawn rate multiplier set to {multiplier} until the server restarts.");
                    return true;
                }

                case "pvp":
                {
                    if (args.Length != 3 || !(args[2].Equals("on", StringComparison.OrdinalIgnoreCase) || args[2].Equals("off", StringComparison.OrdinalIgnoreCase)))
                    {
                        caller.Reply("Usage: admin pvp <player> <on|off>");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[1]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[1]}'.");
                        return true;
                    }
                    bool on = args[2].Equals("on", StringComparison.OrdinalIgnoreCase);
                    SendPacket(target, TMLAdmin.PacketSetPvp, p => p.Write(on));
                    caller.Reply($"PvP {(on ? "enabled" : "disabled")} for {target.name}.");
                    return true;
                }

                case "event":
                {
                    if (args.Length >= 2 && args[1].Equals("stop", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!BossRushSystem.Active)
                            caller.Reply("No event is running.");
                        else
                        {
                            BossRushSystem.Stop("Boss rush stopped by a server administrator.");
                            caller.Reply("Boss rush stopped.");
                        }
                        return true;
                    }

                    if (args.Length < 3 || !args[1].Equals("bossrush", StringComparison.OrdinalIgnoreCase))
                    {
                        caller.Reply("Usage: admin event bossrush <player> [pre|hard|all]   or   admin event stop");
                        return true;
                    }
                    if (BossRushSystem.Active)
                    {
                        caller.Reply("A boss rush is already running. Use 'admin event stop' first.");
                        return true;
                    }
                    Player target = AdminCommand.FindPlayer(args[2]);
                    if (target == null)
                    {
                        caller.Reply($"Could not find online player '{args[2]}'.");
                        return true;
                    }
                    string[] stages = BossRushSystem.StagesFor(args.Length > 3 ? args[3] : "pre");
                    if (stages == null)
                    {
                        caller.Reply("Mode must be pre (pre-Hardmode bosses), hard (Hardmode bosses) or all.");
                        return true;
                    }
                    BossRushSystem.Start(target.whoAmI, stages);
                    caller.Reply($"Boss rush started on {target.name} with {stages.Length} bosses.");
                    return true;
                }
            }

            return false;
        }
    }
}
