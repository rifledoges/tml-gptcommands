using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
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
            public string Prefix = "";
            public bool HasPrefixColor;
            public Color PrefixColor = Color.White;
            public string Suffix = "";
            public bool HasSuffixColor;
            public Color SuffixColor = Color.White;
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
            var saved = new TagCompound();
            var playerNames = new List<string>();
            foreach (var entry in Styles)
            {
                var style = new TagCompound
                {
                    ["prefix"] = entry.Value.Prefix,
                    ["prefixHasColor"] = entry.Value.HasPrefixColor,
                    ["prefixR"] = (int)entry.Value.PrefixColor.R,
                    ["prefixG"] = (int)entry.Value.PrefixColor.G,
                    ["prefixB"] = (int)entry.Value.PrefixColor.B,
                    ["suffix"] = entry.Value.Suffix,
                    ["suffixHasColor"] = entry.Value.HasSuffixColor,
                    ["suffixR"] = (int)entry.Value.SuffixColor.R,
                    ["suffixG"] = (int)entry.Value.SuffixColor.G,
                    ["suffixB"] = (int)entry.Value.SuffixColor.B,
                    ["hasColor"] = entry.Value.HasColor,
                    ["r"] = (int)entry.Value.Color.R,
                    ["g"] = (int)entry.Value.Color.G,
                    ["b"] = (int)entry.Value.Color.B
                };
                playerNames.Add(entry.Key);
                saved["style_" + entry.Key] = style;
            }

            saved["playerNames"] = playerNames;
            tag["tmlAdminChatStyles"] = saved;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            Styles.Clear();
            if (!tag.ContainsKey("tmlAdminChatStyles"))
                return;

            TagCompound saved = tag.GetCompound("tmlAdminChatStyles");
            IList<string> playerNames = saved.GetList<string>("playerNames");
            foreach (string playerName in playerNames)
            {
                TagCompound style = saved.GetCompound("style_" + playerName);
                Styles[playerName] = new ChatStyle
                {
                    Prefix = style.ContainsKey("prefix") ? style.GetString("prefix") : style.GetString("tag"),
                    HasPrefixColor = style.GetBool("prefixHasColor"),
                    PrefixColor = new Color((byte)style.GetInt("prefixR"), (byte)style.GetInt("prefixG"), (byte)style.GetInt("prefixB")),
                    Suffix = style.GetString("suffix"),
                    HasSuffixColor = style.GetBool("suffixHasColor"),
                    SuffixColor = new Color((byte)style.GetInt("suffixR"), (byte)style.GetInt("suffixG"), (byte)style.GetInt("suffixB")),
                    HasColor = style.GetBool("hasColor"),
                    Color = new Color((byte)style.GetInt("r"), (byte)style.GetInt("g"), (byte)style.GetInt("b"))
                };
            }
        }

        private delegate void OrigBroadcastChatMessageAs(byte messageAuthor, NetworkText text, Color color, int excludedPlayer);
        private delegate void HookBroadcastChatMessageAs(OrigBroadcastChatMessageAs orig, byte messageAuthor, NetworkText text, Color color, int excludedPlayer);

        public override void Load()
        {
            MethodInfo method = typeof(ChatHelper).GetMethod(
                nameof(ChatHelper.BroadcastChatMessageAs),
                BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Could not find ChatHelper.BroadcastChatMessageAs.");

            MonoModHooks.Add(method, (HookBroadcastChatMessageAs)OnBroadcastChatMessageAs);
        }

        public override void Unload()
        {
            Styles.Clear();
        }

        private static void OnBroadcastChatMessageAs(
            OrigBroadcastChatMessageAs orig,
            byte messageAuthor,
            NetworkText text,
            Color color,
            int excludedPlayer)
        {
            if (messageAuthor < Main.maxPlayers)
            {
                Player player = Main.player[messageAuthor];
                if (player != null && player.active && ModerationSystem.IsMuted(player.name))
                {
                    ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral("You are muted."), Color.OrangeRed, messageAuthor);
                    return;
                }

                if (player != null && player.active && Styles.TryGetValue(player.name, out ChatStyle style))
                {
                    string message = text.ToString();

                    // Suffix goes right after the "<name>" part of the message when it is there, otherwise at the end.
                    if (!string.IsNullOrWhiteSpace(style.Suffix))
                    {
                        string suffix = FormatTag(style.Suffix, style.HasSuffixColor, style.SuffixColor);
                        string nameToken = "<" + player.name + ">";
                        if (message.StartsWith(nameToken, StringComparison.Ordinal))
                            message = nameToken + " " + suffix + message.Substring(nameToken.Length);
                        else
                            message = message + " " + suffix;
                    }

                    if (!string.IsNullOrWhiteSpace(style.Prefix))
                        message = FormatTag(style.Prefix, style.HasPrefixColor, style.PrefixColor) + " " + message;

                    if (style.HasColor)
                        color = style.Color;

                    text = NetworkText.FromLiteral(message);
                }
            }

            orig(messageAuthor, text, color, excludedPlayer);
        }

        public static void SetPrefix(string playerName, string text, bool hasColor, Color color)
        {
            ChatStyle style = GetOrCreate(playerName);
            style.Prefix = text;
            style.HasPrefixColor = hasColor;
            style.PrefixColor = color;
        }

        public static void ClearPrefix(string playerName)
        {
            if (Styles.TryGetValue(playerName, out ChatStyle style))
            {
                style.Prefix = "";
                style.HasPrefixColor = false;
            }
        }

        public static void SetSuffix(string playerName, string text, bool hasColor, Color color)
        {
            ChatStyle style = GetOrCreate(playerName);
            style.Suffix = text;
            style.HasSuffixColor = hasColor;
            style.SuffixColor = color;
        }

        public static void ClearSuffix(string playerName)
        {
            if (Styles.TryGetValue(playerName, out ChatStyle style))
            {
                style.Suffix = "";
                style.HasSuffixColor = false;
            }
        }

        // Renders "[text]" with the text itself colored using Terraria's [c/RRGGBB:text] chat tag.
        private static string FormatTag(string text, bool hasColor, Color color)
        {
            if (!hasColor)
                return "[" + text + "]";

            return "[[c/" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2") + ":" + text + "]]";
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
                && tag.IndexOfAny(new[] { '\n', '\r' }) < 0;
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
