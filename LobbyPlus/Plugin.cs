using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using LobbyPlus.Systems;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/* TODO

Fix players not spawning in the next round if they die and send their death packet after the host has gone to the next game? (autostart)



More lobby creation options in the ui

Pick times to remind/update the auto start timer in chat
More auto start options? (Should the countdown stop when the player count falls below the threshhold?)

Afk command
Auto ready command

Properly sync readied players
Prevent dying as a 'bad' player in games modes like tag and h&s when the last 'good' player dies, leading to nobody winning
*/

namespace LobbyPlus
{
    [BepInPlugin($"lammas123.{MyPluginInfo.PLUGIN_NAME}", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("lammas123.ChatCommands")]
    public class LobbyPlus : BasePlugin
    {
        internal static LobbyPlus Instance;

        public delegate void LobbyConfigLoaded();
        public static LobbyConfigLoaded onLobbyConfigLoaded;

        public static LobbyConfig LobbyConfig { get; internal set; }
        public static ConfigEntry<int> MaxPlayersSlider { get; internal set; }

        
        internal readonly string[] MotdsDefault = [
            "Crab Gaming",
            "This lobby's name keeps changing!!!",
            "Powered by Lobby+"
        ];
        internal readonly string[] GameModesAndMapsDefault = ["*"];
        internal readonly string[] ServerMessagesDefault = [
            "W",
            "This lobby is powered by Lobby+!",
            "I like Turtles"
        ];
        internal readonly string[] ServerDeathMessagesDefault = [
            "{PLAYER} died",
            "{PLAYER} fell off",
            "{PLAYER} got mucked",
            "{PLAYER} failed us",
            "{PLAYER} didn't try hard enough",
            "{PLAYER} threw",
            "{PLAYER} couldn't take the pressure",
            "{PLAYER} croaked",
            "{PLAYER} needs a gaming chair",
            "{PLAYER}'s brother was playing"
        ];
        internal readonly string[] ServerKillMessagesDefault = [
            "{KILLER} killed {PLAYER}",
            "{KILLER} murdered {PLAYER}",
            "{KILLER} obliterated {PLAYER}",
            "{KILLER} destroyed {PLAYER}",
            "{KILLER} deleted {PLAYER}",
            "{KILLER} curb stomped {PLAYER}",
            "{KILLER} drop kicked {PLAYER}",
            "{KILLER} blasted {PLAYER}",
            "{KILLER} unalived {PLAYER}",
            "{KILLER} mucked {PLAYER}"
        ];
        internal readonly string[] ServerWinMessagesDefault = [
            "{PLAYER} won!",
            "{PLAYER} got the W!",
            "{PLAYER} mucked everyone else! Ayo?"
        ];
        internal readonly int[] WinnerItemSpamItems = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13];


        public override void Load()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            Instance = this;

            if (!TomlTypeConverter.CanConvert(typeof(string[])))
                TomlTypeConverter.AddConverter(typeof(string[]), new TypeConverter
                {
                    ConvertToString = (obj, type) =>
                    {
                        string[] strs = (string[])obj;
                        List<string> filteredStrs = [];

                        foreach (string str in strs)
                        {
                            string escapedStr = Utility.Escape(str.Trim());
                            if (escapedStr.Length != 0) // No empty strings
                                filteredStrs.Add(escapedStr);
                        }
                        return string.Join(", ", filteredStrs);
                    },
                    ConvertToObject = (str, type) =>
                    {
                        List<string> strs = [];
                        int commaIndex = -1;

                        while (commaIndex + 1 < str.Length)
                        {
                            commaIndex = str.IndexOf(',', commaIndex + 1);
                            if (commaIndex == -1) // Final string, no ending comma
                            {
                                strs.Add(str);
                                break;
                            }

                            if (commaIndex > 0 && str[commaIndex - 1] == '\\') // Potentially an escaped comma
                            {
                                int backslashCount = 1;
                                while (commaIndex - backslashCount >= 0 && str[commaIndex - backslashCount] == '\\') // How many backslashes come before this point?
                                    backslashCount++;
                                if (backslashCount % 2 == 1) // Count is odd (ex. '\\\,' escapes the comma), not the end and should continue finding the next comma
                                    continue;
                            }

                            if (commaIndex != 0) // Not an empty string, add to strs
                            {
                                string s = str[..commaIndex].TrimEnd();
                                strs.Add(
                                    Regex.IsMatch(s, @"^""?\w:\\(?!\\)(?!.+\\\\)")
                                        ? s
                                        : Utility.Unescape(s)
                                );
                            }
                            str = str[(commaIndex + 1)..].TrimStart();
                            commaIndex = -1;
                        }

                        return strs.ToArray();
                    }
                });
            if (!TomlTypeConverter.CanConvert(typeof(int[])))
                TomlTypeConverter.AddConverter(typeof(int[]), new TypeConverter
                {
                    ConvertToString = (obj, type) => string.Join(", ", (int[])obj),
                    ConvertToObject = (str, type) => str.Split(',').Select(s => int.Parse(s.Trim())).ToArray()
                });

            LoadConfig(Config.Bind("Lobby+", "DefaultLobbyConfig", "Default", "The name of the lobby config to load by default.").Value);

            MaxPlayersSlider = Config.Bind($"Lobby+", "MaxPlayersSlider", 40,
                "The highest the max players slider can go to, must be set between 40 and 250.");
            if (MaxPlayersSlider.Value < 40)
                MaxPlayersSlider.Value = 40;
            else if (MaxPlayersSlider.Value > 250)
                MaxPlayersSlider.Value = 250;


            MessageOfTheDay.Init();
            LobbyGameSettings.Init();
            ServerMessages.Init();

            ChatCommands.Api.RegisterCommand(new ReadyCommand());
            ChatCommands.Api.RegisterCommand(new UnreadyCommand());
            ChatCommands.Api.RegisterCommand(new LoadLobbyCommand());
            ChatCommands.Api.RegisterCommand(new ReloadLobbyCommand());

            Harmony.CreateAndPatchAll(typeof(Patches));
            Log.LogInfo($"Loaded [{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION}]");
        }

        internal void LoadConfig(string name)
        {
            ConfigFile config = new(Path.Combine([BepInEx.Paths.ConfigPath, $"lammas123.{MyPluginInfo.PLUGIN_GUID}", $"{name}.cfg"]), true);

            LobbyConfig = new()
            {
                Name = name,
                config = config,

                motdCycleTime = config.Bind("MOTD", "MOTDCycleTime", 60,
                    "The cycling time between MOTDs in seconds, setting to 0 will disable cycling."),
                motds = config.Bind<string[]>("MOTD", "MOTDs", [.. MotdsDefault],
                    "The motds (messages of the day) to cycle through over time."),
                type = config.Bind("Lobby Settings", "Type", 2,
                    "The type of lobby. 2=Public, 1=Friends Only, 0=Code Only"),
                voiceChatEnabled = config.Bind("Lobby Settings", "VoiceChatEnabled", true,
                    "If voice chat should be enabled in your lobby."),
                maxPlayers = config.Bind("Lobby Settings", "MaxPlayers", 15,
                    "The max number of players that can join your lobby, must be set between 2 and 250."),
                enabledGameModes = config.Bind<string[]>("Lobby Settings", "EnabledGameModes", [.. GameModesAndMapsDefault],
                    "The names of the game modes you want to have enabled, or '*' to enable them all."),
                enabledMaps = config.Bind<string[]>("Lobby Settings", "EnabledMaps", [.. GameModesAndMapsDefault],
                    "The names of the maps you want to have enabled, or '*' to enable them all."),
                lobbyMap = config.Bind("Lobby Settings", "LobbyMap", "Dorm",
                    "The name of the map you want to use as the lobby. If it is not set to Dorm, then the only way players can ready up is via the !ready command."),

                serverMessageCycleTime = config.Bind("Server Messages", "ServerMessageCycleTime", 45,
                    "The cycling time between server messages in seconds, setting to 0 will disable cycling and not show any server messages."),
                serverMessages = config.Bind<string[]>("Server Messages", "ServerMessages", [.. ServerMessagesDefault],
                    "The server messages to cycle through over time in a random order."),
                serverDeathMessages = config.Bind<string[]>("Server Messages", "ServerDeathMessages", [.. ServerDeathMessagesDefault],
                    "The server death messages to show when someone dies, leave empty to not show death messages."),
                serverKillMessages = config.Bind<string[]>("Server Messages", "ServerKillMessages", [.. ServerKillMessagesDefault],
                    "The server kill messages to show when someone kills another player, leave empty to not show kill messages."),
                serverWinMessages = config.Bind<string[]>("Server Messages", "ServerWinMessages", [.. ServerWinMessagesDefault],
                    "The server win messages to show when someone wins, leave empty to not show win messages."),

                autoStartCountdownMinPlayers = config.Bind("Auto Start", "AutoStartCountdownMinPlayers", 0,
                    "The minimum number of players to start the auto start countdown, set to 0 to disable."),
                autoStartCountdownTime = config.Bind("Auto Start", "AutoStartCountdownLength", 5,
                    "The length of the auto start countdown in seconds, must be 5 seconds or higher."),
                autoStartMinPlayers = config.Bind("Auto Start", "AutoStartMinPlayers", 0,
                    "The minimum number of players to automatically start, set to 0 to disable. The lobby will be skipped if there are enough players at the end of the previous games."),

                winnerItemSpamItems = config.Bind<int[]>("Extra Settings", "WinnerItemSpamItems", [.. WinnerItemSpamItems],
                    "The ids of the items you want to have spammed when a player wins, item ids are between 0 and 13 inclusive."),
                freezePhaseTime = config.Bind("Extra Settings", "FreezePhaseTime", 11,
                    "The length of the freeze phase in seconds, must be 5 seconds or higher."),
                roundOverPhaseTime = config.Bind("Extra Settings", "RoundOverPhaseTime", 7,
                    "The length of the round over phase in seconds, must be 3 seconds or higher."),
                gameOverPhaseTime = config.Bind("Extra Settings", "GameOverPhaseTime", 7,
                    "The length of the game over phase in seconds, must be 3 seconds or higher."),
                winScreenTime = config.Bind("Extra Settings", "WinScreenTime", 11,
                    "The length of the win screen in seconds, set to 0 to skip."),
                onlyOneRound = config.Bind("Extra Settings", "OnlyOneRound", false,
                    "When enabled, only one round will be played before sending everyone back to the lobby (or to the win screen if it isn't to be skipped and only 1 player is alive). This can be paired with AutoStartMinPlayers to play round after round of custom game modes like Infection and Manhunt.")
            };

            if (LobbyConfig.motdCycleTime.Value < 0)
                LobbyConfig.motdCycleTime.Value = 0;
            if (LobbyConfig.motds.Value.Length == 0)
                LobbyConfig.motds.Value = [.. MotdsDefault];
            if (LobbyConfig.type.Value < 0 || LobbyConfig.type.Value > 2)
                LobbyConfig.type.Value = 2;
            if (LobbyConfig.maxPlayers.Value < 2)
                LobbyConfig.maxPlayers.Value = 2;
            else if (LobbyConfig.maxPlayers.Value > 250)
                LobbyConfig.maxPlayers.Value = 250;
            if (LobbyConfig.enabledGameModes.Value.Length == 0)
                LobbyConfig.enabledGameModes.Value = [.. GameModesAndMapsDefault];
            if (LobbyConfig.enabledMaps.Value.Length == 0)
                LobbyConfig.enabledMaps.Value = [.. GameModesAndMapsDefault];

            if (LobbyConfig.serverMessageCycleTime.Value < 0)
                LobbyConfig.serverMessageCycleTime.Value = 0;
            if (LobbyConfig.serverMessages.Value.Length == 0)
                LobbyConfig.serverMessages.Value = [.. ServerMessagesDefault];

            if (LobbyConfig.autoStartCountdownMinPlayers.Value < 0)
                LobbyConfig.autoStartCountdownMinPlayers.Value = 0;
            if (LobbyConfig.autoStartCountdownTime.Value < 5)
                LobbyConfig.autoStartCountdownTime.Value = 5;
            if (LobbyConfig.autoStartMinPlayers.Value < 0)
                LobbyConfig.autoStartMinPlayers.Value = 0;

            LobbyConfig.winnerItemSpamItems.Value = [.. LobbyConfig.winnerItemSpamItems.Value.Where(itemId => itemId >= 0 && itemId <= 13)];
            if (LobbyConfig.freezePhaseTime.Value < 5)
                LobbyConfig.freezePhaseTime.Value = 5;
            if (LobbyConfig.roundOverPhaseTime.Value < 3)
                LobbyConfig.roundOverPhaseTime.Value = 3;
            if (LobbyConfig.gameOverPhaseTime.Value < 3)
                LobbyConfig.gameOverPhaseTime.Value = 3;
            if (LobbyConfig.winScreenTime.Value < 0)
                LobbyConfig.winScreenTime.Value = 0;

            onLobbyConfigLoaded?.Invoke();
        }
    }

    public struct LobbyConfig
    {
        public string Name { get; internal set; }
        internal ConfigFile config;

        internal ConfigEntry<int> motdCycleTime;
        internal ConfigEntry<string[]> motds;
        internal ConfigEntry<int> type;
        internal ConfigEntry<bool> voiceChatEnabled;
        internal ConfigEntry<int> maxPlayers;
        internal ConfigEntry<string[]> enabledGameModes;
        internal ConfigEntry<string[]> enabledMaps;
        internal ConfigEntry<string> lobbyMap;
        internal ConfigEntry<int[]> winnerItemSpamItems;

        internal ConfigEntry<int> serverMessageCycleTime;
        internal ConfigEntry<string[]> serverMessages;
        internal ConfigEntry<string[]> serverDeathMessages;
        internal ConfigEntry<string[]> serverKillMessages;
        internal ConfigEntry<string[]> serverWinMessages;
        
        internal ConfigEntry<int> autoStartCountdownMinPlayers;
        internal ConfigEntry<int> autoStartCountdownTime;
        internal ConfigEntry<int> autoStartMinPlayers;

        internal ConfigEntry<int> freezePhaseTime;
        internal ConfigEntry<int> roundOverPhaseTime;
        internal ConfigEntry<int> gameOverPhaseTime;
        internal ConfigEntry<int> winScreenTime;
        internal ConfigEntry<bool> onlyOneRound;
    }
}