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

This mod is configured as `side = Both` (players download it automatically on joining) because summon slots are enforced on each player's own client. The `admin` command is registered with `CommandType.Console`; it is not intended for in-game chat.

## Build status

The source files are in this repository. A packaged `.tmod` has **not** yet been built or tested. The build automation is not configured yet, so do not install this source as a finished mod.

## Compatibility

Works with TModLoader 1.4.4, no dependencies, still work in progress 
