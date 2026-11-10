using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>Timed server features: auto-broadcast messages and the shutdown / restart countdown.</summary>
    public class ServerSystem : ModSystem
    {
        private static int broadcastTicks;
        private static int broadcastIndex;

        private static int shutdownTicksLeft = -1;
        private static bool shutdownIsRestart;

        public static bool ShutdownPending => shutdownTicksLeft >= 0;

        public static void StartShutdown(int minutes, bool restart)
        {
            shutdownIsRestart = restart;
            shutdownTicksLeft = Math.Max(0, minutes) * 60 * 60;
            if (shutdownTicksLeft == 0)
                shutdownTicksLeft = 1;
            AnnounceShutdown(shutdownTicksLeft / 60);
        }

        public static void CancelShutdown()
        {
            if (shutdownTicksLeft < 0)
                return;
            shutdownTicksLeft = -1;
            Broadcast("The scheduled " + (shutdownIsRestart ? "restart" : "shutdown") + " was cancelled.", Color.LightGreen);
        }

        public static void Broadcast(string text, Color color)
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
        }

        private static string FormatTime(int seconds)
        {
            if (seconds >= 120 && seconds % 60 == 0)
                return (seconds / 60) + " minutes";
            if (seconds == 60)
                return "1 minute";
            return seconds + " second" + (seconds == 1 ? "" : "s");
        }

        private static void AnnounceShutdown(int seconds)
        {
            string what = shutdownIsRestart ? "restarting" : "shutting down";
            Broadcast($"Server {what} in {FormatTime(seconds)}.", Color.OrangeRed);
        }

        public override void PostUpdateEverything()
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            // Auto-broadcast.
            var data = ModerationSystem.Data;
            if (data.BroadcastEnabled && data.Broadcasts.Count > 0)
            {
                if (++broadcastTicks >= Math.Max(30, data.BroadcastIntervalSeconds) * 60)
                {
                    broadcastTicks = 0;
                    if (broadcastIndex >= data.Broadcasts.Count)
                        broadcastIndex = 0;
                    var entry = data.Broadcasts[broadcastIndex++];
                    string text = ModerationSystem.Render(entry.Text, entry.Color, out Color color);
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
                }
            }
            else
            {
                broadcastTicks = 0;
            }

            // Shutdown / restart countdown.
            if (shutdownTicksLeft >= 0)
            {
                shutdownTicksLeft--;
                if (shutdownTicksLeft % 60 == 0)
                {
                    int seconds = shutdownTicksLeft / 60;
                    if (seconds == 0)
                    {
                        Broadcast(shutdownIsRestart ? "Server restarting now. See you in a moment!" : "Server shutting down now.", Color.OrangeRed);
                        WorldFile.SaveWorld();
                        shutdownTicksLeft = -1;
                        Netplay.Disconnect = true;
                    }
                    else if (seconds == 3600 || seconds == 1800 || seconds == 600 || seconds == 300 || seconds == 120
                        || seconds == 60 || seconds == 30 || seconds == 10 || seconds <= 5)
                    {
                        AnnounceShutdown(seconds);
                    }
                }
            }
        }
    }
}
