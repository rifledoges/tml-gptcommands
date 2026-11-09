# TML Admin

A server-side tModLoader 1.4.4 mod that adds dedicated-server-console administration commands. It is intended for servers running tModLoader and does not require Calamity.

## Commands

Run these from the **dedicated server console**:

- `admin help` — show command help
- `admin players` — list online players
- `admin announce <message>` — broadcast a message
- `admin heal <player>` — restore a player's life and mana
- `admin kick <player> [reason]` — disconnect a player
- `admin save` — save the current world

This mod is configured as server-only and registers its command with `CommandType.Console`; it is not intended for in-game chat.

## Build status

The source files are in this repository. A packaged `.tmod` has **not** yet been built or tested. The build automation is not configured yet, so do not install this source as a finished mod.

## Compatibility

Target: tModLoader 1.4.4. Calamity is not a dependency. Compatibility still needs to be verified on the actual server.
