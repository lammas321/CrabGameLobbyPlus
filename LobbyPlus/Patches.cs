using HarmonyLib;
using LobbyPlus.Systems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using static LobbyPlus.LobbyPlus;

namespace LobbyPlus
{
    internal static class Patches
    {
        /*[HarmonyPatch(typeof(GameUiChatBox), nameof(GameUiChatBox.SendMessage))]
        [HarmonyPrefix]
        internal static bool PreGameUiChatBoxSendMessage(string param_1)
        {
            string[] args = param_1.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (args[0] == ".mic")
            {
                string mic = MonoBehaviourPublicObdicoObInGaObdiUnique.Instance.comms.prop_String_1; // Dissonance
                GameUiChatBox.Instance.ForceMessage($"Current mic: {mic}");

                string newMic = Microphone.devices[(Microphone.devices.IndexOf(mic) + 1) % Microphone.devices.Count];
                GameUiChatBox.Instance.ForceMessage($"New mic: {newMic}");
                MonoBehaviourPublicObdicoObInGaObdiUnique.Instance.comms.prop_String_1 = newMic; // Dissonance
                return false;
            }

            return true;
        }*/




        // Init some systems
        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.StartLobby))]
        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.StartPracticeLobby))]
        [HarmonyPostfix]
        internal static void PostLobbyManagerStartLobby()
        {
            MessageOfTheDay.Start();
            ServerMessages.Start();
        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.CloseLobby))]
        [HarmonyPostfix]
        internal static void PostLobbyManagerCloseLobby()
        {
            MessageOfTheDay.Stop();
            ServerMessages.Stop();
        }
        
        // Correct waiting room ready players ui
        [HarmonyPatch(typeof(GameUiStatus), nameof(GameUiStatus.Awake))]
        [HarmonyPostfix]
        internal static void PostGameUiStatusAwake()
        {
            GameUiStatus.Instance.playerCount.rectTransform.anchorMax = Vector2.one;
            GameUiStatus.Instance.countdown.rectTransform.anchorMax = Vector2.one;
        }


        // Lobby+ config scroller
        [HarmonyPatch(typeof(MenuUiCreateLobbySettings), nameof(MenuUiCreateLobbySettings.Start))]
        [HarmonyPostfix]
        internal static void PostMenuUiCreateLobbySettingsStart(MenuUiCreateLobbySettings __instance)
        {
            LobbyGameSettings.lobbySettings = __instance;

            GameObject lobbyConfig = UnityEngine.Object.Instantiate(__instance.lobbyType.gameObject, __instance.lobbyType.transform.parent);
            lobbyConfig.name = "LobbyPlusConfig";
            lobbyConfig.transform.SetSiblingIndex(0);
            lobbyConfig.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = "Lobby+ Config";

            List<string> configs = [
                .. Directory.GetFiles(Path.Combine([BepInEx.Paths.ConfigPath, $"lammas123.{MyPluginInfo.PLUGIN_GUID}"]))
                    .Select(file => Path.GetFileNameWithoutExtension(Path.GetFileName(file)))
            ];
            if (!configs.Contains(LobbyPlus.LobbyConfig.Name))
                configs.Add(LobbyPlus.LobbyConfig.Name);
            int index = Math.Max(0, configs.IndexOf(LobbyPlus.LobbyConfig.Name)) + 1;
            configs.Insert(0, string.Empty);

            LobbyGameSettings.lobbyConfigScroll = lobbyConfig.GetComponent<GeneralUiSettingsScroll>();
            LobbyGameSettings.lobbyConfigScroll.SetSettings(configs.ToArray(), index);

            Instance.LoadConfig(LobbyGameSettings.lobbyConfigScroll.field_Private_ArrayOf_0[index]);
        }
        [HarmonyPatch(typeof(MenuUiCreateLobbyGameModesAndMaps), nameof(MenuUiCreateLobbyGameModesAndMaps.Start))]
        [HarmonyPostfix]
        internal static void PostMenuUiCreateLobbyGameModesAndMapsStart(MenuUiCreateLobbyGameModesAndMaps __instance)
            => LobbyGameSettings.lobbyGameModesAndMaps = __instance;

        [HarmonyPatch(typeof(GeneralUiSettingsScroll), nameof(GeneralUiSettingsScroll.Method_Private_Void_0))]
        [HarmonyPrefix]
        internal static void PreGeneralUiSettingsScrollSettingUpdated(GeneralUiSettingsScroll __instance)
        {
            if (__instance == LobbyGameSettings.lobbyConfigScroll)
            {
                List<string> configs = [
                    .. Directory.GetFiles(Path.Combine([BepInEx.Paths.ConfigPath, $"lammas123.{MyPluginInfo.PLUGIN_GUID}"]))
                        .Select(file => Path.GetFileNameWithoutExtension(Path.GetFileName(file)))
                ];
                if (!configs.Contains(LobbyPlus.LobbyConfig.Name))
                    configs.Add(LobbyPlus.LobbyConfig.Name);
                configs.Insert(0, string.Empty);

                LobbyGameSettings.lobbyConfigScroll.field_Private_ArrayOf_0 = configs.ToArray();
                LobbyGameSettings.lobbyConfigScroll.currentSetting = Math.Clamp(LobbyGameSettings.lobbyConfigScroll.currentSetting, 1, LobbyGameSettings.lobbyConfigScroll.field_Private_ArrayOf_0.Length - 1);

                Instance.LoadConfig(LobbyGameSettings.lobbyConfigScroll.field_Private_ArrayOf_0[LobbyGameSettings.lobbyConfigScroll.currentSetting]);
                return;
            }
        }


        // Update Lobby+ settings
        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.Awake))]
        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.NewLobbySettings))]
        [HarmonyPostfix]
        internal static void PostLobbyManagerInitLobbySettings(LobbyManager __instance)
        {
            if (__instance == LobbyManager.Instance)
                LobbyGameSettings.UpdateSettings();
        }
        [HarmonyPatch(typeof(GameModeManager), nameof(GameModeManager.Awake))]
        [HarmonyPostfix]
        internal static void PostGameModeManagerAwake(GameModeManager __instance)
        {
            if (__instance == GameModeManager.Instance)
                LobbyGameSettings.UpdateGameModeSettings();
        }
        [HarmonyPatch(typeof(MapManager), nameof(MapManager.Awake))]
        [HarmonyPostfix]
        internal static void PostMapManagerAwake(MapManager __instance)
        {
            if (__instance == MapManager.Instance)
                LobbyGameSettings.UpdateMapSettings();
        }


        // Death/kill messages
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.LoadMap), [typeof(int), typeof(int)])]
        [HarmonyPostfix]
        internal static void PostServerSendLoadMap()
            => ServerMessages.lastAttackers.Clear();

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.PunchPlayer))]
        [HarmonyPostfix]
        internal static void PostServerSendPunchPlayer(ulong param_0, ulong param_1)
        {
            if (SteamManager.Instance.IsLobbyOwner() && param_1 > 1 && param_0 != param_1)
                ServerMessages.lastAttackers[param_1] = param_0;
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.PlayerDamage))]
        [HarmonyPostfix]
        internal static void PostServerSendPlayerDamage(ulong param_0, ulong param_1)
        {
            if (SteamManager.Instance.IsLobbyOwner() && param_1 > 1 && param_0 != param_1)
                ServerMessages.lastAttackers[param_1] = param_0;
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.PlayerDied))]
        [HarmonyPrefix]
        internal static void PreServerSendPlayerDied(ulong param_0, ulong param_1)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || !GameManager.Instance.activePlayers.ContainsKey(param_0) || GameManager.Instance.activePlayers[param_0].dead)
                return;

            if (param_1 > 1 && param_0 != param_1)
                ServerMessages.lastAttackers[param_0] = param_1;

            if (ServerMessages.lastAttackers.TryGetValue(param_0, out ulong killerClientId))
                ServerMessages.SendKillMessage(param_0, killerClientId);
            else
                ServerMessages.SendDeathMessage(param_0);
            
            ServerMessages.lastAttackers.Remove(param_0);
        }

        
        // Winner detection
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.GameOver))]
        [HarmonyPostfix]
        internal static void PostServerSendGameOver(ulong param_0)
        {
            if (!SteamManager.Instance.IsLobbyOwner() || param_0 == 0)
                return;

            ServerMessages.SendWinMessage(param_0);
            WinnerItemSpam.Start(param_0);
        }


        // Autostart
        [HarmonyPatch(typeof(GameModeWaiting), nameof(GameModeWaiting.Update))]
        [HarmonyPostfix]
        internal static void PostGameModeWaitingUpdate()
        {
            if (SteamManager.Instance.IsLobbyOwner() && !LobbyManager.Instance.started)
                AutoStart.Update();
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.RestartLobby))]
        [HarmonyPrefix]
        internal static bool PreGameLoopRestartLobby()
        {
            if (SteamManager.Instance.IsLobbyOwner() && LobbyPlus.LobbyConfig.autoStartMinPlayers.Value != 0 && LobbyManager.steamIdToUID.Count >= LobbyPlus.LobbyConfig.autoStartMinPlayers.Value)
            {
                AutoStart.skippingLobby = true;
                GameLoop.Instance.StartGames();
                AutoStart.skippingLobby = false;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.Method_Private_Void_0))]
        [HarmonyPrefix]
        internal static bool PreGameLoopEliminatePlayersThatAreParticipatingButNotLoaded()
            => !AutoStart.skippingLobby;

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.GetPlayersAlive))]
        [HarmonyPrefix]
        internal static bool PreGameManagerGetPlayersAlive(ref int __result)
        {
            if (!AutoStart.skippingLobby)
                return true;

            __result = LobbyManager.steamIdToUID.Count;
            return false;
        }



        // Win screen time patchews
        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendWinner))]
        [HarmonyPrefix]
        internal static void PreServerSendSendWinner()
        {
            if (LobbyPlus.LobbyConfig.winScreenTime.Value == 0)
                GameLoop.Instance.Invoke(nameof(GameLoop.RestartLobby), 0.25f);
        }

        [HarmonyPatch(typeof(WinManager), nameof(WinManager.Start))]
        [HarmonyPostfix]
        internal static void PostWinManagerStart(WinManager __instance)
        {
            if (SteamManager.Instance.IsLobbyOwner())
            {
                __instance.CancelInvoke(nameof(WinManager.Continue));
                __instance.Invoke(nameof(WinManager.Continue), LobbyPlus.LobbyConfig.winScreenTime.Value);
            }
        }



        // Only one round patches
        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.StartGames))]
        [HarmonyPrefix]
        internal static void PreGameLoopStartGames()
            => LobbyGameSettings.shouldPlayRound = true;

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.NextGame))]
        [HarmonyPrefix]
        internal static bool PreGameLoopNextGame()
        {
            if (LobbyGameSettings.shouldPlayRound)
                return true;

            LobbyGameSettings.shouldPlayRound = true;
            GameLoop.Instance.RestartLobby();
            return false;
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.StartGames))]
        [HarmonyPostfix]
        internal static void PostGameLoopStartGames()
        {
            if (LobbyPlus.LobbyConfig.onlyOneRound.Value)
                LobbyGameSettings.shouldPlayRound = false;
        }



        // Make obstacles actually function in the lobby
        internal static bool isWaiting = false;
        [HarmonyPatch(typeof(ServerClock), nameof(ServerClock.Update))]
        [HarmonyPrefix]
        internal static void PreServerClockUpdate()
        {
            if (LobbyManager.Instance.gameMode.type == GameModeType.Waiting)
            {
                LobbyManager.Instance.gameMode.type = GameModeType.Practice;
                isWaiting = true;
            }
        }
        [HarmonyPatch(typeof(ServerClock), nameof(ServerClock.Update))]
        [HarmonyPostfix]
        internal static void PostServerClockUpdate()
        {
            if (isWaiting)
            {
                LobbyManager.Instance.gameMode.type = GameModeType.Waiting;
                isWaiting = false;
            }
        }

        [HarmonyPatch(typeof(BounceObstacle), nameof(BounceObstacle.OnCollisionEnter))]
        [HarmonyPrefix]
        internal static void PreBounceObstacleOnCollisionEnter()
        {
            if (LobbyManager.Instance.gameMode.type == GameModeType.Waiting)
            {
                LobbyManager.Instance.gameMode.type = GameModeType.Practice;
                isWaiting = true;
            }
        }
        [HarmonyPatch(typeof(BounceObstacle), nameof(BounceObstacle.OnCollisionEnter))]
        [HarmonyPostfix]
        internal static void PostBounceObstacleOnCollisionEnter()
        {
            if (isWaiting)
            {
                LobbyManager.Instance.gameMode.type = GameModeType.Waiting;
                isWaiting = false;
            }
        }

        [HarmonyPatch(typeof(SpinnerObstacle), nameof(SpinnerObstacle.FixedUpdate))]
        [HarmonyPrefix]
        internal static void PreSpinnerObstacleFixedUpdate()
        {
            if (LobbyManager.Instance.gameMode.type == GameModeType.Waiting)
            {
                LobbyManager.Instance.gameMode.type = GameModeType.Practice;
                isWaiting = true;
            }
        }
        [HarmonyPatch(typeof(SpinnerObstacle), nameof(SpinnerObstacle.FixedUpdate))]
        [HarmonyPostfix]
        internal static void PostSpinnerObstacleFixedUpdate()
        {
            if (isWaiting)
            {
                LobbyManager.Instance.gameMode.type = GameModeType.Waiting;
                isWaiting = false;
            }
        }



        // Spawn dead bodies in practice, and don't mute players that die in the waiting room
        internal static bool callingPlayerDied = false;
        internal static bool wasPractice = false;
        internal static bool wasWaitingRoom = false;
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.PlayerDied))]
        [HarmonyPrefix]
        internal static void PreGameManagerPlayerDied()
        {
            callingPlayerDied = true;
            if (LobbyManager.Instance.gameMode.type == GameModeType.Practice)
            {
                LobbyManager.Instance.gameMode.type = GameModeType.Waiting;
                wasPractice = true;
            }
        }
        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.CheckGameOver))]
        [HarmonyPrefix]
        internal static void PreGameLoopCheckGameOver()
        {
            if (callingPlayerDied && wasPractice)
            {
                LobbyManager.Instance.gameMode.type = GameModeType.Practice;
                wasPractice = false;
            }
        }
        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.CheckGameOver))]
        [HarmonyPostfix]
        internal static void PostGameLoopCheckGameOver()
        {
            if (callingPlayerDied && LobbyManager.Instance.gameMode.type == GameModeType.Waiting)
            {
                LobbyManager.Instance.gameMode.type = GameModeType.Practice;
                wasWaitingRoom = true;
            }
        }
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.PlayerDied))]
        [HarmonyPostfix]
        internal static void PostGameManagerPlayerDied()
        {
            if (callingPlayerDied && wasWaitingRoom)
            {
                LobbyManager.Instance.gameMode.type = GameModeType.Waiting;
                wasWaitingRoom = false;
            }
            callingPlayerDied = false;
        }
    }
}