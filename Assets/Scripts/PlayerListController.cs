using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerListController : MonoBehaviour
{
    [SerializeField] private RectTransform playerListContainer;
    [SerializeField] private PlayerListEntry playerListEntryPrefab;

    private readonly Dictionary<ulong, PlayerListEntry> playerList = new();

    public void SetupPlayers(Dictionary<ulong, bool> playerStatusDict)
    {
        int index = 0;
        foreach(KeyValuePair<ulong, bool> playerStatus in playerStatusDict)
        {
            PlayerListEntry entry = GetOrCreateEntry(playerStatus);

            string playerName = $"Player {playerStatus.Key + 1}";
            if(playerStatus.Key == NetworkManager.Singleton.LocalClientId)
            {
                playerName += " (You)";
            }

            entry.SetIsReady(playerName, playerStatus.Value);
            entry.gameObject.SetActive(true);

            index++;
        }

        // Hide any extra UI elements that are not used
        for(int i = index; i < playerList.Count; i++)
        {
            playerListContainer.GetChild(i).gameObject.SetActive(false);
        }
    }

    private PlayerListEntry GetOrCreateEntry(KeyValuePair<ulong, bool> playerStatus)
    {
        PlayerListEntry entry;
        if(playerList.TryGetValue(playerStatus.Key, out PlayerListEntry value))
        {
            entry = value;
        } else
        {
            entry = Instantiate(playerListEntryPrefab, playerListContainer);
            playerList.Add(playerStatus.Key, entry);
        }

        return entry;
    }

    public void SetPlayerScores(Dictionary<ulong, int> pointsDict)
    {
        foreach(KeyValuePair<ulong, int> score in pointsDict)
        {
            playerList[score.Key].SetScore(score.Value);
        }
    }

    public void ClearAllScores()
    {
        foreach(KeyValuePair<ulong, PlayerListEntry> entry in playerList)
        {
            entry.Value.SetScore(0);
        }
    }
}
