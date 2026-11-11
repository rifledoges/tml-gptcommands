using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Persistent bans and mutes, stored in &lt;save path&gt;/TMLAdmin/moderation.json.
    /// Bans are enforced by a periodic check on the server (a banned player is kicked within about half a second of joining).
    /// </summary>
    public class ModerationSystem : ModSystem
    {
        public class BanEntry
        {
            public string Name { get; set; } = "";
            public string Ip { get; set; } = "";
            public string Reason { get; set; } = "";
        }

        public class BroadcastEntry
        {
            public string Text { get; set; } = "";
            public string Color { get; set; } = "";
        }

        public class SaveData
        {
            public List<BanEntry> Bans { get; set; } = new List<BanEntry>();
            public List<string> Mutes { get; set; } = new List<string>();
            public List<string> Whitelist { get; set; } = new List<string>();
            public bool WhitelistEnabled { get; set; }
            public string Motd { get; set; } = "";
            public string MotdColor { get; set; } = "";
            public List<BroadcastEntry> Broadcasts { get; set; } = new List<BroadcastEntry>();
            public int BroadcastIntervalSeconds { get; set; } = 300;
            public bool BroadcastEnabled { get; set; }
            public List<string> ChatFilter { get; set; } = new List<string>();
            public Dictionary<string, int> AccessorySlots { get; set; } = new Dictionary<string, int>();
            public Dictionary<string, int> SentrySlots { get; set; } = new Dictionary<string, int>();
        }

        private static SaveData data = new SaveData();
        private static int tick;

        // Session-only state.
        public static bool Lockdown;
        public static int SlowmodeSeconds;
        private static readonly bool[] present = new bool[256];
        private static readonly bool[] motdSent = new bool[256];
        private static readonly int[] sinceJoin = new int[256];
        private static readonly DateTime[] lastChat = new DateTime[256];

        public static SaveData Data => data;

        private static string FilePath => Path.Combine(Main.SavePath, "TMLAdmin", "moderation.json");

        public static IReadOnlyList<BanEntry> Bans => data.Bans;
        public static IReadOnlyList<string> Mutes => data.Mutes;

        public override void Load()
        {
            data = new SaveData();
            try
            {
                if (File.Exists(FilePath))
                    data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(FilePath)) ?? new SaveData();
            }
            catch (Exception e)
            {
                Mod.Logger.Warn("Could not read moderation.json: " + e.Message);
                data = new SaveData();
            }
        }

        public override void Unload()
        {
            data = new SaveData();
        }

        public static void Persist() => Save();

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception e)
            {
                ModContent.GetInstance<TMLAdmin>().Logger.Warn("Could not save moderation.json: " + e.Message);
            }
        }

        public static string GetIp(int whoAmI)
        {
            try
            {
                var socket = Netplay.Clients[whoAmI].Socket;
                if (socket == null)
                    return "";
                var address = socket.GetRemoteAddress();
                return address == null ? "" : (address.GetIdentifier() ?? "");
            }
            catch
            {
                return "";
            }
        }

        // ---- Mutes ----

        public static bool IsMuted(string name)
        {
            return data.Mutes.Any(m => m.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static bool Mute(string name)
        {
            if (IsMuted(name))
                return false;
            data.Mutes.Add(name);
            Save();
            return true;
        }

        public static bool Unmute(string name)
        {
            int removed = data.Mutes.RemoveAll(m => m.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
                Save();
            return removed > 0;
        }

        // ---- Bans ----

        public static BanEntry FindBan(string name, string ip)
        {
            return data.Bans.FirstOrDefault(b =>
                (!string.IsNullOrEmpty(b.Name) && b.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrEmpty(b.Ip) && !string.IsNullOrEmpty(ip) && b.Ip == ip));
        }

        public static void AddBan(string name, string ip, string reason)
        {
            data.Bans.RemoveAll(b => !string.IsNullOrEmpty(name) && b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            data.Bans.Add(new BanEntry { Name = name ?? "", Ip = ip ?? "", Reason = reason ?? "" });
            Save();
        }

        /// <summary>Removes bans whose name or IP matches the given key. Returns how many were removed.</summary>
        public static int RemoveBan(string key)
        {
            int removed = data.Bans.RemoveAll(b =>
                b.Name.Equals(key, StringComparison.OrdinalIgnoreCase) || (b.Ip.Length > 0 && b.Ip == key));
            if (removed > 0)
                Save();
            return removed;
        }

        // ---- Saved per-player settings ----

        public static int GetSavedAccessorySlots(string name)
        {
            foreach (var entry in data.AccessorySlots)
            {
                if (entry.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return entry.Value;
            }
            return 0;
        }

        public static void SetSavedAccessorySlots(string name, int slots)
        {
            foreach (string key in data.AccessorySlots.Keys.Where(k => k.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList())
                data.AccessorySlots.Remove(key);
            if (slots > 0)
                data.AccessorySlots[name] = slots;
            Save();
        }

        public static int GetSavedSentrySlots(string name)
        {
            foreach (var entry in data.SentrySlots)
            {
                if (entry.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return entry.Value;
            }
            return 0;
        }

        public static void SetSavedSentrySlots(string name, int slots)
        {
            foreach (string key in data.SentrySlots.Keys.Where(k => k.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList())
                data.SentrySlots.Remove(key);
            if (slots > 0)
                data.SentrySlots[name] = slots;
            Save();
        }

        // ---- Whitelist / chat filter / slow mode / styled text ----

        public static bool IsWhitelisted(string name)
        {
            return data.Whitelist.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static string Censor(string message, string playerName)
        {
            if (data.ChatFilter.Count == 0)
                return message;

            string head = "";
            string body = message;
            string nameToken = "<" + playerName + ">";
            if (message.StartsWith(nameToken, StringComparison.Ordinal))
            {
                head = nameToken;
                body = message.Substring(nameToken.Length);
            }

            foreach (string word in data.ChatFilter)
                body = Regex.Replace(body, Regex.Escape(word), m => new string('*', m.Length), RegexOptions.IgnoreCase);

            return head + body;
        }

        /// <summary>Returns true (and the seconds left to wait) if this player must wait before chatting again.</summary>
        public static bool CheckSlowmode(int slot, out int secondsLeft)
        {
            secondsLeft = 0;
            if (SlowmodeSeconds <= 0 || slot < 0 || slot >= lastChat.Length)
                return false;

            DateTime now = DateTime.UtcNow;
            double elapsed = (now - lastChat[slot]).TotalSeconds;
            if (elapsed < SlowmodeSeconds)
            {
                secondsLeft = (int)Math.Ceiling(SlowmodeSeconds - elapsed);
                return true;
            }

            lastChat[slot] = now;
            return false;
        }

        /// <summary>Applies an optional color spec to text. A gradient colors the letters; a solid color is returned as the message color.</summary>
        public static string Render(string text, string colorSpec, out Color color)
        {
            color = Color.White;
            if (string.IsNullOrEmpty(colorSpec) || !ChatFormattingSystem.TryParseColorSpec(colorSpec, out Color[] colors))
                return text;

            if (colors.Length == 1)
            {
                color = colors[0];
                return text;
            }

            return ChatFormattingSystem.Colorize(text, colors);
        }

        public static void SendToPlayer(int slot, string text, string colorSpec)
        {
            string rendered = Render(text, colorSpec, out Color color);
            Terraria.Chat.ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(rendered), color, slot);
        }

        public override void PostUpdateEverything()
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            if (++tick % 30 != 0)
                return;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player == null || !player.active)
                {
                    present[i] = false;
                    motdSent[i] = false;
                    sinceJoin[i] = 0;
                    continue;
                }

                bool isNew = !present[i];
                present[i] = true;

                BanEntry ban = FindBan(player.name, GetIp(i));
                if (ban != null)
                {
                    string reason = string.IsNullOrWhiteSpace(ban.Reason) ? "Banned." : "Banned: " + ban.Reason;
                    NetMessage.SendData(MessageID.Kick, i, -1, NetworkText.FromLiteral(reason));
                    continue;
                }

                if (data.WhitelistEnabled && !IsWhitelisted(player.name))
                {
                    NetMessage.SendData(MessageID.Kick, i, -1, NetworkText.FromLiteral("You are not on this server's whitelist."));
                    continue;
                }

                if (isNew && Lockdown && !IsWhitelisted(player.name))
                {
                    NetMessage.SendData(MessageID.Kick, i, -1, NetworkText.FromLiteral("The server is locked down. Please try again later."));
                    continue;
                }

                // Send the MOTD a few seconds after joining so the client is in the world to see it.
                if (!motdSent[i])
                {
                    if (++sinceJoin[i] >= 6)
                    {
                        motdSent[i] = true;
                        if (!string.IsNullOrWhiteSpace(data.Motd))
                            SendToPlayer(i, data.Motd, data.MotdColor);

                        // Re-apply saved per-player settings now that the client is in the world.
                        int savedSlots = GetSavedAccessorySlots(player.name);
                        if (savedSlots > 0)
                            AccessorySlotPlayer.Apply(player, savedSlots);

                        int savedSentries = GetSavedSentrySlots(player.name);
                        if (savedSentries > 0)
                            SentrySlotPlayer.Apply(player, savedSentries);
                    }
                }
            }
        }
    }
}
