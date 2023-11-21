using System.Collections.Generic;
using System.Linq;
using Niantic.Lightship.AR.NavigationMesh;
using PrimeTween;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : NetworkBehaviour
{
    [SerializeField] private Vole volePrefab;
    [SerializeField] private float timeBetweenSpawns = 1f;
    [SerializeField] private float roundTime = 30f;
    [Space]
    [SerializeField] private Button startGameButton;
    [SerializeField] private CanvasGroup startGameCanvasGroup;
    [SerializeField] private PlayerListController playerList;

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI countdownText;

    private bool isRoundActive;
    private float nextSpawnTime;
    private float roundStartTime;

    private LightshipNavMeshManager navMeshManager;
    private readonly Dictionary<ulong, int> playerScores = new();
    private readonly List<Vole> activeSpawnedVoles = new();

    private void Start()
    {
        navMeshManager = GameManager.Instance.NavMeshManager;
        startGameCanvasGroup.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
        countdownText.gameObject.SetActive(false);
        startGameButton.onClick.AddListener(OnClickStartGame);
    }

    public void StartNewRound()
    {
        countdownText.gameObject.SetActive(false);
        startGameCanvasGroup.gameObject.SetActive(true);
    }

    private void OnClickStartGame()
    {
        StartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        SendStartGameClientRpc(NetworkManager.Singleton.ConnectedClientsIds.ToArray());
    }

    [ClientRpc]
    private void SendStartGameClientRpc(ulong[] connectedClientIds)
    {
        playerList.ClearAllScores();
        playerScores.Clear();
        foreach(ulong clientId in connectedClientIds)
        {
            playerScores.Add(clientId, 0);
        }

        startGameCanvasGroup.gameObject.SetActive(false);

        countdownText.SetText(string.Empty);
        countdownText.gameObject.SetActive(true);
        countdownText.alpha = 1;

        Sequence.Create()
            .ChainCallback(() => countdownText.SetText("3"))
            .Group(Tween.PunchScale(countdownText.transform, Vector3.one, 1f, 1f))
            .ChainCallback(() => countdownText.SetText("2"))
            .Group(Tween.PunchScale(countdownText.transform, Vector3.one, 1f, 1f))
            .ChainCallback(() => countdownText.SetText("1"))
            .Group(Tween.PunchScale(countdownText.transform, Vector3.one, 1f, 1f))
            .ChainCallback(() =>
            {
                countdownText.SetText("GO!");
                roundStartTime = Time.time;
                nextSpawnTime = Time.time;
                isRoundActive = true;
                timerText.gameObject.SetActive(true);
            })
            .ChainDelay(0.25f)
            .Chain(Tween.Alpha(countdownText, 0, 0.5f));
    }

    private void Update()
    {
        if(!isRoundActive) return;

        int secondsRemaining = Mathf.FloorToInt(roundTime - (Time.time - roundStartTime));
        timerText.text = secondsRemaining.ToString();

        if(!IsHost) return;

        if(secondsRemaining <= 0)
        {
            EndRound();
            return;
        }

        if(Time.time < nextSpawnTime) return;

        SpawnVole();
    }

    private void SpawnVole()
    {
        navMeshManager.LightshipNavMesh.FindRandomPosition(out Vector3 randomPosition);
        Vole spawnedVole = Instantiate(volePrefab, randomPosition, Quaternion.identity);
        spawnedVole.SetDestination(randomPosition);
        spawnedVole.NetworkObject.Spawn();
        activeSpawnedVoles.Add(spawnedVole);
        nextSpawnTime = Time.time + timeBetweenSpawns;
    }

    public void RemoveVole(Vole vole)
    {
        activeSpawnedVoles.Remove(vole);
    }

    [ClientRpc]
    public void GivePointsClientRpc(ulong clientId, int points)
    {
        playerScores[clientId] += points;
        playerList.SetPlayerScores(playerScores);
    }

    private void EndRound()
    {
        foreach(Vole vole in activeSpawnedVoles)
        {
            vole.RemoveFromGame();
        }

        activeSpawnedVoles.Clear();
        NotifyEndRoundClientRpc();
    }

    [ClientRpc]
    private void NotifyEndRoundClientRpc()
    {
        isRoundActive = false;
        timerText.gameObject.SetActive(false);

        countdownText.SetText("Finished!");
        countdownText.gameObject.SetActive(true);
        countdownText.alpha = 1;
        Tween.PunchScale(countdownText.transform, Vector3.one, 1f, 1f)
            .Chain(Tween.Alpha(countdownText, 0, 1f))
            .ChainDelay(1f)
            .ChainCallback(DisplayWinner);
    }

    private void DisplayWinner()
    {
        if(!playerScores.TryGetValue(NetworkManager.Singleton.LocalClientId, out int localPlayerScore))
        {
            // Error, we cannot get our own score?
            StartNewRound();
            return;
        }

        bool hasTieWithAnotherPlayer = false;
        bool isScoreHighest = true;

        foreach(KeyValuePair<ulong, int> scoreEntry in playerScores
                    .Where(scoreEntry => scoreEntry.Key != NetworkManager.Singleton.LocalClientId))
        {
            if(scoreEntry.Value > localPlayerScore)
            {
                isScoreHighest = false;
            } else if(scoreEntry.Value == localPlayerScore)
            {
                hasTieWithAnotherPlayer = true;
            }
        }

        if(isScoreHighest && hasTieWithAnotherPlayer)
        {
            countdownText.SetText("Tie!");
        } else if(isScoreHighest)
        {
            countdownText.SetText("Winner!");
        } else
        {
            countdownText.SetText("Loser!");
        }

        countdownText.gameObject.SetActive(true);
        countdownText.alpha = 1;
        Tween.PunchScale(countdownText.transform, Vector3.one, 1f, 1f, true, Ease.Default, 0, 3)
            .Chain(Tween.Alpha(countdownText, 0, 2f, Ease.Default, 1, CycleMode.Restart, 3f))
            .ChainCallback(StartNewRound);
    }
}
