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

### Chat styling
- `admin prefix <player> <text> [color]` — tag shown before the player's name in chat
- `admin suffix <player> <text> [color]` — tag shown after the player's name
- `admin prefixclear <player>`, `admin suffixclear <player>` — remove them
- `admin color <player> <color>` — color the player's chat messages

`[color]` can be a name (`red`, `orange`, `yellow`, `gold`, `lime`, `green`, `teal`, `turquoise`, `cyan`, `blue`, `purple`, `magenta`, `pink`, `hotpink`, `brown`, `maroon`, `white`, `silver`, `gray`, `black`), a hex code (`#RRGGBB`), a gradient preset (`rainbow`, `fire`, `ocean`, `sunset`, `forest`, `ice`, `neon`, `pastel`, `galaxy`, `goldrush`, `toxic`, `candy`), or 2–6 colors joined with `-` for a custom gradient, e.g. `red-yellow-lime` or `#FF0000-#0000FF`. `admin colors` lists them in the console. Gradients use Terraria chat color tags per letter; very long messages are colored in chunks.

### Targeting
Anywhere a `<player>` is expected you can use `#<slot>` (slot numbers come from `admin players`, handy for names with spaces). These commands also accept `all` in place of the player to affect everyone online: `heal`, `give`, `buff`, `clearbuffs`, `clearinv`, `freeze`, `godmode`, `summonslots`, `slap`, `tpspawn`, `rocket`, `clone`, `lightning`, `pvp`. `admin giveall <item-id> [amount]` is shorthand for `admin give all ...`.

### Fun
- `admin freeze <player> [on|off]` — hold a player in place
- `admin clearinv <player>` — wipe a player's inventory
- `admin rocket <player> [power 1-60]` — launch a player into the air (fall damage disabled for 10 seconds)
- `admin clone <player>` — drop a copy of the item the player is holding
- `admin lightning <player> [damage]`, `admin meteor`
- `admin spawnrate <0-10>` — enemy spawn multiplier (1 = normal, 0 = none); resets on restart
- `admin pvp <player> <on|off>`
- `admin event bossrush <player> [pre|hard|all]` — spawn bosses one after another as each is defeated; `admin event stop` ends it

### Server
- `admin motd [<text> [color] | off]` — message shown to players a few seconds after they join
- `admin autobroadcast add <text> [color] | remove <n> | list | interval <seconds> | on | off` — rotating announcements
- `admin chatfilter add|remove|list|clear [word]` — filtered words become asterisks
- `admin slowmode <seconds>` — limit how often each player can chat (resets on restart)
- `admin lockdown [on|off]` — stop new players joining (resets on restart)
- `admin whitelist on|off|add|remove|list [name]` — only listed character names can join (names are not verified)
- `admin shutdown [minutes|cancel]`, `admin restart [minutes]` — countdown announcements, saves the world, then exits. A restart only comes back if your host auto-restarts the server
- `admin backup` — copy the world to a `TMLAdmin-Backups` folder next to it

Freeze, godmode, rocket, summon slots and PvP changes work through the mod on each player's client, so players need the mod (they download it automatically on joining).

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
