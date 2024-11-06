using BepInEx.IL2CPP.Utils.Collections;
using SteamworksNative;
using System.Collections;
using UnityEngine;
using static LobbyPlus.LobbyPlus;

namespace LobbyPlus.Systems
{
    internal static class MessageOfTheDay
    {
        internal static RandomEndlessQueue<string> motds;
        internal static Coroutine loop;

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
            if (LobbyPlus.LobbyConfig.motdCycleTime.Value == 0)
                return;

            motds = new(LobbyPlus.LobbyConfig.motds.Value);
            loop = LobbyManager.Instance.StartCoroutine(Loop().WrapToIl2Cpp());
        }

        internal static IEnumerator Loop()
        {
            while (true)
            {
                SteamMatchmaking.SetLobbyData(SteamManager.Instance.currentLobby, "LobbyName", motds.Dequeue());
                yield return new WaitForSeconds(LobbyPlus.LobbyConfig.motdCycleTime.Value);
            }
        }

        internal static void Stop()
        {
            if (loop != null)
            {
                LobbyManager.Instance.StopCoroutine(loop);
                loop = null;
            }
            motds = null;
        }
    }
}