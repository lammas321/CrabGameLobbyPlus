using BepInEx.IL2CPP.Utils.Collections;
using System.Collections;
using UnityEngine;

namespace LobbyPlus.Systems
{
    internal static class WinnerItemSpam
    {
        internal static void Start(ulong clientId)
        {
            if (LobbyPlus.LobbyConfig.winnerItemSpamItems.Value.Length != 0)
                GameManager.Instance.StartCoroutine(Coroutine(clientId).WrapToIl2Cpp());
        }

        internal static IEnumerator Coroutine(ulong clientId)
        {
            System.Random random = new();
            while (true)
            {
                if (!GameManager.Instance.activePlayers.ContainsKey(clientId) || GameManager.Instance.activePlayers[clientId].dead)
                    yield break;

                ServerSend.DropItem(clientId, LobbyPlus.LobbyConfig.winnerItemSpamItems.Value[random.Next(LobbyPlus.LobbyConfig.winnerItemSpamItems.Value.Length)], SharedObjectManager.Instance.GetNextId(), int.MaxValue);
                yield return new WaitForEndOfFrame();
            }
        }
    }
}