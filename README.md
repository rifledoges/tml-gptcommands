# TML Admin

A tModLoader 1.4.4 mod that adds dedicated-server-console administration commands. It is intended for servers running tModLoader and does not require Calamity.

## Commands

Run these from the **dedicated server console**:

- `admin help` — show command help
- `admin players` — list online players
- `admin announce <message>` — broadcast a message
- `admin heal <player>` — restore a player's life and mana
- `admin kick <player> [reason]` — disconnect a player
- `admin spawn <npc-id> [amount]` — spawn 1–20 NPCs by numeric ID near world spawn
- `admin kill <player>` — apply an in-game kill to a named online player
- `admin give <player> <item-id> [amount]` — give an online player an item by numeric ID (amount capped at the item's max stack)
- `admin summonslots <player> <extra-slots>` — add 0–100 extra summon slots (max minions) for a player; `0` resets. Session-only: resets when the player rejoins or the server restarts
- `admin save` — save the current world

### Moderation
- `admin ban <player> [reason]` / `admin banname <name> [reason]` — ban by name and IP (online player) or by name only (offline). Saved to `<save path>/TMLAdmin/moderation.json`
- `admin unban <name|ip>`, `admin banlist`
- `admin mute <player>`, `admin unmute <name>`, `admin mutelist` — muted players' chat is blocked
- `admin whois <player>` — slot, IP, life/mana, summon slots, position
- `admin slap <player> [damage]`

### World
- `admin time <day|noon|dusk|night|midnight|HH:MM>`
- `admin rain|sandstorm|slimerain <on|off>`, `admin bloodmoon`, `admin eclipse`
- `admin invade <goblins|frostlegion|pirates|martians|stop>`
- `admin boss <name> <player>` — summon a vanilla boss on a player
- `admin butcher` (kill hostile NPCs), `admin clearitems`, `admin clearprojectiles`
- `admin hardmode`, `admin settle`, `admin setspawn <player>`

### Player tools
- `admin godmode <player> [on|off]` (session-only)
- `admin buff <player> <buff-id> [seconds]`, `admin clearbuffs <player>`
- `admin tpspawn <player>`

This mod is configured as `side = Both` (players download it automatically on joining) because summon slots are enforced on each player's own client. The `admin` command is registered with `CommandType.Console`; it is not intended for in-game chat.

## Build status

The source files are in this repository. A packaged `.tmod` has **not** yet been built or tested. The build automation is not configured yet, so do not install this source as a finished mod.

## Compatibility

Works with TModLoader 1.4.4, no dependencies, still work in progress 
