# RF5-DRP (Rune Factory 5 Discord Rich Presence)

This plugin display your characters:

- Name
- Gender
- Level
- Money
- Location
- Current in game date

All of these settings are customizable through the config file.<br />
There is also an alternate layout if you don't like the default.

![Layout Example](https://user-images.githubusercontent.com/77337386/182056592-0f0418ea-5e22-4e78-a9c8-d80306f17c0b.png)


## Installation
- This plugin requires BepInEx 6.0.0-be.572 or installing [RF5Fix](https://github.com/Lyall/RF5Fix) (it comes with BepInEx version you need)
- Drop the folder 'DiscordRichPresence' into Rune Factory 5/BepInEx/plugins. (You could put all the dll's into it without the folder it just organizes it)
- Due to using [Lachee's Discord-RPC](https://github.com/Lachee/discord-rpc-csharp) it needs both Newtonsoft.Json.dll & DiscordRPC.dll. If you remove them the plugin won't work.

## Configuration
All of the display options are optional and are changed through the config file located at **Rune Factory 5/BepInEx/config/RF5DRP.cfg**.
<br />All settings are default to true except alternate layout.
<br />This file will not show up until you've run the game at least once while the plugin is in your plugins folder.

## Issues & WIP
This is my first ever mod for a game so it may not be perfect. There are a few issues that I hope to iron out later but for now they're not big deals.
- There are multiples "areas" within Rigbarth like "Great Tree Plaza" and "Phoros Woodlands" the event I have used to catch these area names won't
trigger if the character is just walking to them. I need more time to figure them out but if you teleport to areas such as 'Phoros Woodlands' it will change.
- ~~If the player stays up all night and the day passes it won't update on the presence to that new day.~~ **1.0.1 has a "fix" to change the day accordingly.**
- There are no area thumbnails due to mostly laziness and I wasn't sure if they would look good.

I have not tested out the area output as much as I haven't played much of the game so there could possibly be problems with later areas.<br />
If you come across any issues or problems that aren't listed here feel free to open an issue. Also any suggestions are gladly welcome!

