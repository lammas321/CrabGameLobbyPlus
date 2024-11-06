# CrabGameLobbyPlus
A BepInEx mod for Crab Game that adds lobby configs(aka presets) with additional settings.

Depends on [ChatCommands](https://github.com/lammas321/CrabGameCustomCommands)

## The Main Config
The main config file ( at "BepInEx/config/lammas123.LobbyPlus.cfg") allows you to change the max value of the max players slide in the main menu, as well as pick what lobby config to load by default.

## What are Lobby Configs?
Lobby+ allows you to have as many lobby configs as you want that can all be swapped between at any time, either in the lobby creation menu or in game with the loadlobby command.

![Lobby+ Config Option](https://github.com/user-attachments/assets/5bcb63d7-0bd8-486d-8889-c592f765395b)

These configs are saved as separate .cfg files in your "BepInEx/config/lammas123.LobbyPlus" folder.

By default, you will only have a Default.cfg file, but you are free to modify and or copy and rename it to create more configs.

## Lobby Config Options
NameOfTheOption: The option's description.

### Auto Start
AutoStartCountdownMinPlayers: The minimum number of players to start the auto start countdown, set to 0 to disable.

AutoStartCountdownLength: The length of the auto start countdown in seconds, must be 5 seconds or higher.

AutoStartMinPlayers: he minimum number of players to automatically start, set to 0 to disable. The lobby will be skipped if there are enough players at the end of the previous games.

### Extra Settings
WinnerItemSpamItems: The ids of the items you want to have spammed when a player wins, item ids are between 0 and 13 inclusive.

FreezePhaseTime: The length of the freeze phase in seconds, must be 5 seconds or higher.

RoundOverPhaseTime: The length of the round over phase in seconds, must be 3 seconds or higher.

GameOverPhaseTime: The length of the game over phase in seconds, must be 3 seconds or higher.

WinScreenTime: The length of the win screen in seconds, set to 0 to skip.

OnlyOneRound: When enabled, only one round will be played before sending everyone back to the lobby (or to the win screen if it isn't to be skipped and only 1 player is alive). This can be paired with AutoStartMinPlayers to play round after round of custom game modes like Infection and Manhunt.

### Lobby Settings
Type: The type of lobby. 2=Public, 1=Friends Only, 0=Code Only

VoiceChatEnabled: If voice chat should be enabled in your lobby.

MaxPlayers: The max number of players that can join your lobby, must be set to 2 or higher.

EnabledGameModes: The names of the game modes you want to have enabled, or '*' to enable them all.

EnabledMaps: The names of the maps you want to have enabled, or '*' to enable them all.

LobbyMap: The name of the map you want to use as the lobby. If it is not set to Dorm, then the only way players can ready up is via the !ready command.

### MOTD
MOTDCycleTime: The cycling time between MOTDs in seconds, setting to 0 will disable cycling.

MOTDs: The motds (messages of the day) to cycle through over time.

### Server Messages
ServerMessageCycleTime: The cycling time between server messages in seconds, setting to 0 will disable cycling and not show any server messages.

ServerMessages: The server messages to cycle through over time in a random order.

ServerDeathMessages: The server death messages to show when someone dies, leave empty to not show death messages.

ServerKillMessages: The server kill messages to show when someone kills another player, leave empty to not show kill messages.

ServerWinMessages: The server win messages to show when someone wins, leave empty to not show win messages.

## Extra Info
Any options that take a list of options (game modes, maps, ect) are separated by commas.

Game modes and maps are not case sensitive, and ignore spaces, so for "Tiny Town 2" the config will accept things like "tinyTOWN 2", "t i n y t o w n 2", "T  iN y   ToW  n2", ect.

The limits on how short the phases can be were place by me intentionally. Technically I could make the phases 1 second or even less, but that felt too quick in testing.

The only exception to this is FreezePhaseTime, as if it is too short, the round will start before everyone has loaded in and cause some to become spectators. This can even still happen at the shortest time I allowed of 5 seconds.

Currently, no custom game modes have been made to be compatible with OnlyOneRound, but it is there so that custom game modes can utilize it in the future. 

When using a lobby map with moving or bounce obstacles, those without Lobby+ will not see them move and won't bounce off of them.

MOTDs are the names to show as the lobby's name in the server list.
