using SteamworksNative;
using System;
using System.Collections.Generic;
using System.Linq;
using static LobbyPlus.LobbyPlus;

namespace LobbyPlus.Systems
{
    internal static class LobbyGameSettings
    {
        internal static MenuUiCreateLobbySettings lobbySettings;
        internal static MenuUiCreateLobbyGameModesAndMaps lobbyGameModesAndMaps;
        internal static GeneralUiSettingsScroll lobbyConfigScroll;
        
        internal static bool shouldPlayRound = true;

        internal static void Init()
            => onLobbyConfigLoaded += LobbyConfigLoaded;

        internal static void LobbyConfigLoaded()
        {
            UpdateSettings();

            if (lobbySettings != null)
                UpdateLobbySettings();
            if (lobbyGameModesAndMaps != null)
                UpdateLobbyGameModesAndMaps();

            if (SteamManager.Instance.currentLobby.m_SteamID == 0UL || !SteamManager.Instance.IsLobbyOwner())
                return;

            ServerSend.LobbySettingsUpdate(LobbyManager.Instance.gameSettings);

            switch (LobbyManager.Instance.gameSettings.field_Public_EnumNPublicSealedvaLoFrPu4vUnique_0)
            {
                case LobbyManagerGameVisibility.LobbyCodeOnly:
                    SteamMatchmaking.SetLobbyType(SteamManager.Instance.currentLobby, ELobbyType.k_ELobbyTypeInvisible);
                    SteamMatchmaking.SetLobbyType(SteamManager.Instance.currentLobby, ELobbyType.k_ELobbyTypePrivate);
                    break;
                case LobbyManagerGameVisibility.Friends:
                    SteamMatchmaking.SetLobbyType(SteamManager.Instance.currentLobby, ELobbyType.k_ELobbyTypeFriendsOnly);
                    break;
                case LobbyManagerGameVisibility.Public:
                    SteamMatchmaking.SetLobbyType(SteamManager.Instance.currentLobby, ELobbyType.k_ELobbyTypePublic);
                    break;
            }
            SteamMatchmaking.SetLobbyData(SteamManager.Instance.currentLobby, "Voice Chat", (LobbyManager.Instance.gameSettings.field_Public_Int32_3 == 1).ToString());
            SteamMatchmaking.SetLobbyMemberLimit(SteamManager.Instance.currentLobby, LobbyManager.Instance.gameSettings.field_Public_Int32_4);

            SteamMatchmaking.SetLobbyData(SteamManager.Instance.currentLobby, "Modes", GameModeManager.Instance.GetAvailableModesString());
            SteamMatchmaking.SetLobbyData(SteamManager.Instance.currentLobby, "Maps", MapManager.Instance.GetAvailableMapsString());

            if (LobbyManager.Instance.gameMode == GameModeManager.Instance.defaultMode && LobbyManager.Instance.map != MapManager.Instance.defaultMap)
                ServerSend.LoadMap(MapManager.Instance.defaultMap.id, GameModeManager.Instance.defaultMode.id);
        }

        internal static void UpdateSettings()
        {
            LobbyManager.Instance.gameSettings.field_Public_EnumNPublicSealedvaLoFrPu4vUnique_0 = (LobbyManagerGameVisibility)LobbyPlus.LobbyConfig.type.Value;
            LobbyManager.Instance.gameSettings.field_Public_Int32_3 = LobbyPlus.LobbyConfig.voiceChatEnabled.Value ? 1 : 0;
            LobbyManager.Instance.gameSettings.field_Public_Int32_4 = LobbyPlus.LobbyConfig.maxPlayers.Value;

            if (GameModeManager.Instance != null)
                UpdateGameModeSettings();

            if (MapManager.Instance != null)
                UpdateMapSettings();

            StaticConstants.field_Public_Static_Int32_5 = LobbyPlus.LobbyConfig.freezePhaseTime.Value;
            StaticConstants.field_Public_Static_Int32_7 = LobbyPlus.LobbyConfig.roundOverPhaseTime.Value;
            StaticConstants.field_Public_Static_Int32_8 = LobbyPlus.LobbyConfig.gameOverPhaseTime.Value;
        }
        internal static void UpdateGameModeSettings()
        {
            GameModeManager.Instance.allPlayableGameModes.Clear();
            if (LobbyPlus.LobbyConfig.enabledGameModes.Value[0] == "*")
                foreach (GameModeData gameModeData in GameModeManager.Instance.allGameModes)
                    GameModeManager.Instance.allPlayableGameModes.Add(gameModeData);
            else
            {
                IEnumerable<string> gameModes = LobbyPlus.LobbyConfig.enabledGameModes.Value.Select(gameMode => gameMode.Replace(" ", "").ToLower());
                foreach (GameModeData gameModeData in GameModeManager.Instance.allGameModes)
                    if (gameModes.Contains(gameModeData.modeName.Replace(" ", "").ToLower()))
                        GameModeManager.Instance.allPlayableGameModes.Add(gameModeData);
            }
        }
        internal static void UpdateMapSettings()
        {
            MapManager.Instance.playableMaps.Clear();
            if (LobbyPlus.LobbyConfig.enabledMaps.Value[0] == "*")
                foreach (Map map in MapManager.Instance.maps)
                    MapManager.Instance.playableMaps.Add(map);
            else
            {
                IEnumerable<string> maps = LobbyPlus.LobbyConfig.enabledMaps.Value.Select(map => map.Replace(" ", "").ToLower());
                foreach (Map map in MapManager.Instance.maps)
                    if (maps.Contains(map.mapName.Replace(" ", "").ToLower()))
                        MapManager.Instance.playableMaps.Add(map);
            }

            MapManager.Instance.defaultMap = null;
            foreach (Map map in MapManager.Instance.maps)
                if (LobbyPlus.LobbyConfig.lobbyMap.Value.Replace(" ", "").ToLower() == map.mapName.Replace(" ", "").ToLower())
                    MapManager.Instance.defaultMap = map;
            if (MapManager.Instance.defaultMap == null)
                MapManager.Instance.defaultMap = MapManager.Instance.maps[6]; // Dorm
        }

        internal static void UpdateLobbySettings()
        {
            Instance.Config.Reload();
            if (MaxPlayersSlider.Value < 40)
                MaxPlayersSlider.Value = 40;
            StaticConstants.field_Public_Static_Int32_0 = MaxPlayersSlider.Value;
            lobbySettings.maxPlayers.slider.maxValue = StaticConstants.field_Public_Static_Int32_0;

            lobbySettings.serverNameField.text = LobbyPlus.LobbyConfig.motds.Value[0];
            lobbySettings.UpdateServerName();

            lobbySettings.lobbyType.SetSettings(Enum.GetNames(typeof(LobbyManagerGameVisibility)), LobbyPlus.LobbyConfig.type.Value);
            lobbySettings.proximityChat.SetSetting(LobbyPlus.LobbyConfig.voiceChatEnabled.Value);
            lobbySettings.maxPlayers.SetSettings(LobbyPlus.LobbyConfig.maxPlayers.Value);
        }
        internal static void UpdateLobbyGameModesAndMaps()
        {
            for (int i = lobbyGameModesAndMaps.modeContainer.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(lobbyGameModesAndMaps.modeContainer.GetChild(i).gameObject);
            for (int i = lobbyGameModesAndMaps.mapContainer.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(lobbyGameModesAndMaps.mapContainer.GetChild(i).gameObject);

            lobbyGameModesAndMaps.Start();
        }
    }
}