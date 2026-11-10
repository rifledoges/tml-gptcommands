using Terraria;
using Terraria.ModLoader;

namespace TMLAdmin
{
    /// <summary>
    /// Holds a per-player bonus to summon slots (max minions).
    /// Session-only: not saved, so it resets when the player rejoins or the server restarts.
    /// The bonus must exist on the player's own client because that is where the minion limit is enforced.
    /// </summary>
    public class SummonSlotPlayer : ModPlayer
    {
        public const int MaxBonus = 100;

        public int BonusMinions;

        public override void PostUpdateEquips()
        {
            if (BonusMinions != 0)
                Player.maxMinions += BonusMinions;
        }
    }
}
