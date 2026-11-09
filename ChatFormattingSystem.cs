using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TMLAdmin
{
    public class ChatFormattingSystem : ModSystem
    {
        private sealed class ChatStyle
        {
            public string Tag = "";
            public bool HasColor;
            public Color Color = Color.White;
        }

        private static readonly Dictionary<string, ChatStyle> Styles =
            new Dictionary<string, ChatStyle>(StringComparer.OrdinalIgnoreCase);

        public override void OnWorldLoad()
        {
            Styles.Clear();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            var saved = new Dictionary<string, TagCompound>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in Styles)
            {
                var style = new TagCompound
                {
                    ["tag"] = entry.Value.Tag,
                    ["hasColor"] = entry.Value.HasColor,
                    ["r"] = (int)entry.Value.Color.R,
                    ["g"] = (int)entry.Value.Color.G,
                    ["b"] = (int)entry.Value.Color.B
                };
                saved[entry.Key] = style;
            }

            tag["tmlAdminChatStyles"] = saved;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            Styles.Clear();
            if (!tag.ContainsKey("tmlAdminChatStyles"))
                return;

            var saved = tag.Get<Dictionary<string, TagCompound>>("tmlAdminChatStyles");
            foreach (var entry in saved)
            {
                TagCompound style = entry.Value;
                Styles[entry.Key] = new ChatStyle
                {
                    Tag = style.GetString("tag"),
                    HasColor = style.GetBool("hasColor"),
                    Color = new Color((byte)style.GetInt("r"), (byte)style.GetInt("g"), (byte)style.GetInt("b"))
                };
            }
        }

        public override void Load()
        {
            On.Terraria.Chat.ChatHelper.BroadcastChatMessageAs += OnBroadcastChatMessageAs;
        }

        public override void Unload()
        {
            On.Terraria.Chat.ChatHelper.BroadcastChatMessageAs -= OnBroadcastChatMessageAs;
            Styles.Clear();
        }

        private static void OnBroadcastChatMessageAs(
            On.Terraria.Chat.ChatHelper.orig_BroadcastChatMessageAs orig,
            byte messageAuthor,
            NetworkText text,
            Color color,
            int excludedPlayer)
        {
            if (messageAuthor < Main.maxPlayers)
            {
                Player player = Main.player[messageAuthor];
                if (player != null && player.active && Styles.TryGetValue(player.name, out ChatStyle style))
                {
                    string message = text.ToString();
                    if (!string.IsNullOrWhiteSpace(style.Tag))
                        message = "[" + style.Tag + "] " + message;

                    if (style.HasColor)
                        color = style.Color;

                    text = NetworkText.FromLiteral(message);
                }
            }

            orig(messageAuthor, text, color, excludedPlayer);
        }

        public static void SetTag(string playerName, string tag)
        {
            ChatStyle style = GetOrCreate(playerName);
            style.Tag = tag;
        }

        public static void ClearTag(string playerName)
        {
            if (Styles.TryGetValue(playerName, out ChatStyle style))
                style.Tag = "";
        }

        public static void SetColor(string playerName, Color color)
        {
            ChatStyle style = GetOrCreate(playerName);
            style.Color = color;
            style.HasColor = true;
        }

        public static bool IsValidTag(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag)
                && tag.Length <= 20
                && !tag.Contains("[")
                && !tag.Contains("]")
                && !tag.Contains("\\n")
                && !tag.Contains("\\r");
        }

        public static bool TryParseHexColor(string value, out Color color)
        {
            color = Color.White;
            if (string.IsNullOrEmpty(value))
                return false;

            string hex = value.StartsWith("#", StringComparison.Ordinal) ? value.Substring(1) : value;
            if (hex.Length != 6)
                return false;

            if (!byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r)
                || !byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g)
                || !byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                return false;

            color = new Color(r, g, b);
            return true;
        }

        private static ChatStyle GetOrCreate(string playerName)
        {
            if (!Styles.TryGetValue(playerName, out ChatStyle style))
            {
                style = new ChatStyle();
                Styles[playerName] = style;
            }

            return style;
        }
    }
}
