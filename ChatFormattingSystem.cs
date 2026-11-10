using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TMLAdmin
{
    /// <summary>
    /// Per-player chat styling: a prefix (before the name), a suffix (after the name) and a message color.
    /// Each can be a solid color or a gradient (several colors, e.g. "#FF0000-#0000FF").
    /// Gradients are drawn with Terraria's [c/RRGGBB:text] chat tags, one tag per letter (or per small chunk for long messages).
    /// </summary>
    public class ChatFormattingSystem : ModSystem
    {
        private const int MaxGradientStops = 6;
        private const int MaxTagsPerGradient = 40;

        private static readonly Dictionary<string, string> NamedColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["red"] = "FF0000",
            ["orange"] = "FF8800",
            ["yellow"] = "FFFF00",
            ["gold"] = "FFD700",
            ["lime"] = "00FF00",
            ["green"] = "00B000",
            ["teal"] = "008080",
            ["turquoise"] = "40E0D0",
            ["cyan"] = "00FFFF",
            ["blue"] = "3366FF",
            ["purple"] = "A020F0",
            ["magenta"] = "FF00FF",
            ["pink"] = "FF69B4",
            ["hotpink"] = "FF1493",
            ["brown"] = "8B4513",
            ["maroon"] = "800000",
            ["white"] = "FFFFFF",
            ["silver"] = "C0C0C0",
            ["gray"] = "808080",
            ["black"] = "000000",
        };

        // Gradient presets (up to 6 colors each).
        private static readonly Dictionary<string, string> GradientPresets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["rainbow"] = "FF0000-FFFF00-00FF00-00FFFF-0000FF-FF00FF",
            ["fire"] = "FF0000-FF8800-FFFF00",
            ["ocean"] = "00FFFF-0066FF-0000AA",
            ["sunset"] = "FF4500-FF1493-8A2BE2",
            ["forest"] = "006400-32CD32-ADFF2F",
            ["ice"] = "FFFFFF-00FFFF-1E90FF",
            ["neon"] = "FF00FF-00FFFF",
            ["pastel"] = "FFB3BA-FFDFBA-FFFFBA-BAFFC9-BAE1FF",
            ["galaxy"] = "4B0082-8A2BE2-FF69B4",
            ["goldrush"] = "B8860B-FFD700-FFFACD",
            ["toxic"] = "00FF00-FFFF00",
            ["candy"] = "FF69B4-87CEFA",
        };

        public static string ListNamedColors() => string.Join(", ", NamedColors.Keys);

        public static string ListGradientPresets() => string.Join(", ", GradientPresets.Keys);

        private sealed class ChatStyle
        {
            public string Prefix = "";
            public Color[] PrefixColors;
            public string Suffix = "";
            public Color[] SuffixColors;
            public Color[] MessageColors;
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
                    ["prefixColors"] = ColorsToString(entry.Value.PrefixColors),
                    ["suffix"] = entry.Value.Suffix,
                    ["suffixColors"] = ColorsToString(entry.Value.SuffixColors),
                    ["messageColors"] = ColorsToString(entry.Value.MessageColors)
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
                var loaded = new ChatStyle
                {
                    Prefix = style.ContainsKey("prefix") ? style.GetString("prefix") : style.GetString("tag"),
                    Suffix = style.GetString("suffix")
                };

                if (style.ContainsKey("prefixColors")) loaded.PrefixColors = ColorsFromString(style.GetString("prefixColors"));
                else if (style.GetBool("prefixHasColor")) loaded.PrefixColors = new[] { new Color((byte)style.GetInt("prefixR"), (byte)style.GetInt("prefixG"), (byte)style.GetInt("prefixB")) };

                if (style.ContainsKey("suffixColors")) loaded.SuffixColors = ColorsFromString(style.GetString("suffixColors"));
                else if (style.GetBool("suffixHasColor")) loaded.SuffixColors = new[] { new Color((byte)style.GetInt("suffixR"), (byte)style.GetInt("suffixG"), (byte)style.GetInt("suffixB")) };

                if (style.ContainsKey("messageColors")) loaded.MessageColors = ColorsFromString(style.GetString("messageColors"));
                else if (style.GetBool("hasColor")) loaded.MessageColors = new[] { new Color((byte)style.GetInt("r"), (byte)style.GetInt("g"), (byte)style.GetInt("b")) };

                Styles[playerName] = loaded;
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
                    string nameToken = "<" + player.name + ">";
                    bool hasName = message.StartsWith(nameToken, StringComparison.Ordinal);

                    // Message color: a single color recolors the whole message; a gradient colors the text after the name.
                    if (style.MessageColors != null && style.MessageColors.Length == 1)
                    {
                        color = style.MessageColors[0];
                    }
                    else if (style.MessageColors != null && style.MessageColors.Length > 1)
                    {
                        if (hasName)
                            message = nameToken + Colorize(message.Substring(nameToken.Length), style.MessageColors);
                        else
                            message = Colorize(message, style.MessageColors);
                    }

                    // Suffix goes right after the "<name>" part of the message when it is there, otherwise at the end.
                    if (!string.IsNullOrWhiteSpace(style.Suffix))
                    {
                        string suffix = FormatTag(style.Suffix, style.SuffixColors);
                        if (hasName)
                            message = nameToken + " " + suffix + message.Substring(nameToken.Length);
                        else
                            message = message + " " + suffix;
                    }

                    if (!string.IsNullOrWhiteSpace(style.Prefix))
                        message = FormatTag(style.Prefix, style.PrefixColors) + " " + message;

                    text = NetworkText.FromLiteral(message);
                }
            }

            orig(messageAuthor, text, color, excludedPlayer);
        }

        // ---- Setters used by the admin command ----

        public static void SetPrefix(string playerName, string text, Color[] colors)
        {
            ChatStyle style = GetOrCreate(playerName);
            style.Prefix = text;
            style.PrefixColors = colors;
        }

        public static void ClearPrefix(string playerName)
        {
            if (Styles.TryGetValue(playerName, out ChatStyle style))
            {
                style.Prefix = "";
                style.PrefixColors = null;
            }
        }

        public static void SetSuffix(string playerName, string text, Color[] colors)
        {
            ChatStyle style = GetOrCreate(playerName);
            style.Suffix = text;
            style.SuffixColors = colors;
        }

        public static void ClearSuffix(string playerName)
        {
            if (Styles.TryGetValue(playerName, out ChatStyle style))
            {
                style.Suffix = "";
                style.SuffixColors = null;
            }
        }

        public static void SetColor(string playerName, Color[] colors)
        {
            GetOrCreate(playerName).MessageColors = colors;
        }

        // ---- Validation and parsing ----

        public static bool IsValidTag(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag)
                && tag.Length <= 20
                && !tag.Contains("[")
                && !tag.Contains("]")
                && tag.IndexOfAny(new[] { '\n', '\r' }) < 0;
        }

        /// <summary>
        /// Parses a color spec: a name ("red"), "#RRGGBB", a gradient preset ("rainbow"), or 2 to 6 colors joined with "-"
        /// (each a name or hex), e.g. "#FF0000-#0000FF" or "red-yellow-lime".
        /// </summary>
        public static bool TryParseColorSpec(string value, out Color[] colors)
        {
            colors = null;
            if (string.IsNullOrEmpty(value))
                return false;

            if (GradientPresets.TryGetValue(value, out string preset))
                value = preset;

            string[] parts = value.Split('-');
            if (parts.Length > MaxGradientStops)
                return false;

            var parsed = new Color[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!TryParseSingleColor(parts[i], out parsed[i]))
                    return false;
            }

            colors = parsed;
            return true;
        }

        private static bool TryParseSingleColor(string value, out Color color)
        {
            if (NamedColors.TryGetValue(value, out string hex))
                return TryParseHexColor(hex, out color);

            return TryParseHexColor(value, out color);
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

        public static string DescribeColors(Color[] colors)
        {
            return colors.Length == 1 ? "color " + ColorsToString(colors) : "gradient " + ColorsToString(colors);
        }

        // ---- Color formatting ----

        private static string Hex(Color c)
        {
            return c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        private static string ColorsToString(Color[] colors)
        {
            if (colors == null || colors.Length == 0)
                return "";

            var parts = new string[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                parts[i] = "#" + Hex(colors[i]);
            return string.Join("-", parts);
        }

        private static Color[] ColorsFromString(string value)
        {
            return TryParseColorSpec(value, out Color[] colors) ? colors : null;
        }

        // "[text]" with the text colored; the brackets themselves stay the default color.
        private static string FormatTag(string text, Color[] colors)
        {
            if (colors == null || colors.Length == 0)
                return "[" + text + "]";

            return "[" + Colorize(text, colors) + "]";
        }

        /// <summary>
        /// Colors text with [c/RRGGBB:x] chat tags. One color gives a solid color; two or more give an even gradient.
        /// Whitespace and anything already inside [brackets] (item links and other chat tags) is left alone.
        /// Long text is colored in chunks so the result never has more than about 40 tags.
        /// </summary>
        public static string Colorize(string text, Color[] stops)
        {
            if (string.IsNullOrEmpty(text) || stops == null || stops.Length == 0)
                return text;

            // Find the characters we are allowed to color.
            var colorable = new bool[text.Length];
            int count = 0;
            int depth = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '[') { depth++; continue; }
                if (ch == ']') { if (depth > 0) depth--; continue; }
                if (depth == 0 && !char.IsWhiteSpace(ch))
                {
                    colorable[i] = true;
                    count++;
                }
            }

            if (count == 0)
                return text;

            int chunkSize = stops.Length == 1 ? int.MaxValue : Math.Max(1, (count + MaxTagsPerGradient - 1) / MaxTagsPerGradient);
            int chunkCount = stops.Length == 1 ? 1 : (count + chunkSize - 1) / chunkSize;

            var result = new StringBuilder(text.Length + chunkCount * 12);
            var run = new StringBuilder();
            int runChunk = -1;
            int seen = 0;

            void Flush()
            {
                if (run.Length == 0)
                    return;
                result.Append("[c/").Append(Hex(ColorAt(stops, chunkCount == 1 ? 0.0 : runChunk / (double)(chunkCount - 1)))).Append(':').Append(run).Append(']');
                run.Clear();
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (!colorable[i])
                {
                    Flush();
                    runChunk = -1;
                    result.Append(text[i]);
                    continue;
                }

                int chunk = stops.Length == 1 ? 0 : seen / chunkSize;
                if (chunk != runChunk)
                {
                    Flush();
                    runChunk = chunk;
                }

                run.Append(text[i]);
                seen++;
            }

            Flush();
            return result.ToString();
        }

        private static Color ColorAt(Color[] stops, double t)
        {
            if (stops.Length == 1)
                return stops[0];

            int segments = stops.Length - 1;
            double position = Math.Max(0.0, Math.Min(1.0, t)) * segments;
            int index = Math.Min((int)position, segments - 1);
            float fraction = (float)(position - index);
            return Color.Lerp(stops[index], stops[index + 1], fraction);
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
