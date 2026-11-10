using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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

        private class SaveData
        {
            public List<BanEntry> Bans { get; set; } = new List<BanEntry>();
            public List<string> Mutes { get; set; } = new List<string>();
        }

        private static SaveData data = new SaveData();
        private static int tick;

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

        public override void PostUpdateEverything()
        {
            if (Main.netMode != NetmodeID.Server || data.Bans.Count == 0)
                return;

            if (++tick % 30 != 0)
                return;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player == null || !player.active)
                    continue;

                BanEntry ban = FindBan(player.name, GetIp(i));
                if (ban == null)
                    continue;

                string reason = string.IsNullOrWhiteSpace(ban.Reason) ? "Banned." : "Banned: " + ban.Reason;
                NetMessage.SendData(MessageID.Kick, i, -1, NetworkText.FromLiteral(reason));
            }
        }
    }
}
