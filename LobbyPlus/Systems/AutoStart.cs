using UnityEngine;
using static LobbyPlus.LobbyPlus;

namespace LobbyPlus.Systems
{
    internal static class AutoStart
    {
        internal static bool skippingLobby = false;
        internal static float timer = float.MaxValue;

        internal static void Update()
        {
            int playerCount = GameManager.Instance.activePlayers.Count;
            if (LobbyPlus.LobbyConfig.autoStartMinPlayers.Value != 0 && playerCount >= LobbyPlus.LobbyConfig.autoStartMinPlayers.Value)
            {
                timer = float.MaxValue;
                GameLoop.Instance.StartGames();
                return;
            }

            if (LobbyPlus.LobbyConfig.autoStartCountdownMinPlayers.Value == 0)
                return;
            
            if (timer != float.MaxValue && playerCount < LobbyPlus.LobbyConfig.autoStartCountdownMinPlayers.Value)
            {
                timer = float.MaxValue;
                Utility.SendMessage($"Player count fell below {LobbyPlus.LobbyConfig.autoStartCountdownMinPlayers.Value}, cancelling countdown", Utility.MessageType.Styled, "AutoStart");
                return;
            }


            if (playerCount < LobbyPlus.LobbyConfig.autoStartCountdownMinPlayers.Value)
                return;

            if (timer == float.MaxValue)
            {
                timer = LobbyPlus.LobbyConfig.autoStartCountdownTime.Value;
                Utility.SendMessage($"Starting games in {LobbyPlus.LobbyConfig.autoStartCountdownTime.Value} seconds", Utility.MessageType.Styled, "AutoStart");
            }

            if ((timer -= Time.deltaTime) > 0f)
                return;

            timer = float.MaxValue;
            GameLoop.Instance.StartGames();
        }
    }
}