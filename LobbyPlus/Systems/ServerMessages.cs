using BepInEx.IL2CPP.Utils.Collections;
using SteamworksNative;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LobbyPlus.LobbyPlus;

namespace LobbyPlus.Systems
{
    internal static class ServerMessages
    {
        internal static RandomEndlessQueue<string> serverMessages;
        internal static Coroutine loop;
        internal static RandomEndlessQueue<string> serverDeathMessages;
        internal static RandomEndlessQueue<string> serverKillMessages;
        internal static RandomEndlessQueue<string> serverWinMessages;

        internal static Dictionary<ulong, ulong> lastAttackers = [];
        
        internal static void Init()
            => onLobbyConfigLoaded += LobbyConfigLoaded;

        internal static void LobbyConfigLoaded()
        {
            if (SteamManager.Instance.currentLobby.m_SteamID == 0UL || !SteamManager.Instance.IsLobbyOwner())
                return;

            Stop();
            Start();
        }

        internal static void Start()
        {
            if (LobbyPlus.LobbyConfig.serverMessageCycleTime.Value != 0)
            {
                serverMessages = new(LobbyPlus.LobbyConfig.serverMessages.Value);
                loop = LobbyManager.Instance.StartCoroutine(Loop().WrapToIl2Cpp());
            }
            serverDeathMessages = new(LobbyPlus.LobbyConfig.serverDeathMessages.Value);
            serverKillMessages = new(LobbyPlus.LobbyConfig.serverKillMessages.Value);
            serverWinMessages = new(LobbyPlus.LobbyConfig.serverWinMessages.Value);
        }

        internal static IEnumerator Loop()
        {
            while (true)
            {
                yield return new WaitForSeconds(LobbyPlus.LobbyConfig.serverMessageCycleTime.Value);
                Utility.SendMessage(serverMessages.Dequeue());
            }
        }

        internal static void SendDeathMessage(ulong clientId)
        {
            if (LobbyPlus.LobbyConfig.serverDeathMessages.Value.Length != 0)
                Utility.SendMessage(serverDeathMessages.Dequeue().Replace("{PLAYER}", SteamFriends.GetFriendPersonaName(new(clientId))));
        }

        internal static void SendKillMessage(ulong clientId, ulong killerClientId)
        {
            if (LobbyPlus.LobbyConfig.serverKillMessages.Value.Length != 0)
                Utility.SendMessage(serverKillMessages.Dequeue().Replace("{KILLER}", SteamFriends.GetFriendPersonaName(new(killerClientId))).Replace("{PLAYER}", SteamFriends.GetFriendPersonaName(new(clientId))));
        }

        internal static void SendWinMessage(ulong clientId)
        {
            if (LobbyPlus.LobbyConfig.serverWinMessages.Value.Length != 0)
                Utility.SendMessage(serverWinMessages.Dequeue().Replace("{PLAYER}", SteamFriends.GetFriendPersonaName(new(clientId))));
        }

        internal static void Stop()
        {
            if (loop != null)
            {
                LobbyManager.Instance.StopCoroutine(loop);
                loop = null;
            }

            serverMessages = null;
            serverDeathMessages = null;
            serverKillMessages = null;
            serverWinMessages = null;
        }
    }
}