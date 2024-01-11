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
    [SerializeField] private VoleHole voleHolePrefab;
    [Space]
    [SerializeField] private int numHolesToSpawn = 5;
    [SerializeField] private float spawnRange = 3f;
    [SerializeField] private float timeBetweenSpawns = 1f;
    [SerializeField] private float roundTime = 30f;
    [Space]
    [SerializeField] private Button startGameButton;
    [SerializeField] private CanvasGroup startGameCanvasGroup;
    [SerializeField] private PlayerListController playerList;
    [Space]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [Header("Audio")]
    [SerializeField] private AudioClip startRaceLowSound;
    [SerializeField] private AudioClip startRaceHighSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failureSound;
    private readonly Dictionary<ulong, int> playerScores = new();
    private readonly List<VoleHole> spawnedHoles = new();
    private readonly List<Vole> spawnedVoles = new();

    private AudioSource audioSource;
    private bool isRoundActive;

    private LightshipNavMeshManager navMeshManager;
    private float nextSpawnTime;
    private float roundStartTime;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        navMeshManager = GameManager.Instance.NavMeshManager;
        startGameCanvasGroup.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
        countdownText.gameObject.SetActive(false);
        startGameButton.onClick.AddListener(OnClickStartGame);
    }

    private void Update()
    {
        if(!isRoundActive) return;

        int secondsRemaining = Mathf.FloorToInt(roundTime - (Time.time - roundStartTime));
        timerText.text = secondsRemaining.ToString();

        if(!IsServer) return;

        if(secondsRemaining <= 0)
        {
            EndRound();
            return;
        }

        if(Time.time < nextSpawnTime || spawnedVoles.Count >= 10) return;

        SpawnVoleServer();
        nextSpawnTime = Time.time + timeBetweenSpawns;
    }

    public void ShowNewRoundUI()
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
        // Setup playing field
        Vector3 gameOrigin = GameManager.Instance.SharedSpaceManager.SharedArOriginObject.transform.position;
        for(int i = 0; i < numHolesToSpawn; i++)
        {
            SpawnHoleServer(gameOrigin, i);
        }

        // Let clients know its time to start
        SendStartGameClientRpc(NetworkManager.Singleton.ConnectedClientsIds.ToArray());
    }

    private void SpawnHoleServer(Vector3 gameOrigin, int index)
    {
        navMeshManager.LightshipNavMesh.FindRandomPosition(gameOrigin, spawnRange,
            out Vector3 randomPosition);

        navMeshManager.LightshipNavMesh.FindNearestFreePosition(randomPosition, out Vector3 positionOnNavMesh);
        VoleHole hole = Instantiate(voleHolePrefab, positionOnNavMesh, Quaternion.identity);
        hole.NetworkObject.Spawn();
        spawnedHoles.Add(hole);
        hole.PlaySpawnAnimation(positionOnNavMesh, index * (3f / numHolesToSpawn));
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
            .ChainCallback(() =>
            {
                countdownText.SetText("3");
                audioSource.clip = startRaceLowSound;
                audioSource.Play();
            })
            .Chain(Tween.PunchScale(countdownText.transform, Vector3.one, 1f, 1f))
            .ChainCallback(() =>
            {
                countdownText.SetText("2");
                audioSource.Play();
            })
            .Chain(Tween.PunchScale(countdownText.transform, Vector3.one, 1f, 1f))
            .ChainCallback(() =>
            {
                countdownText.SetText("1");
                audioSource.Play();
            })
            .Chain(Tween.PunchScale(countdownText.transform, Vector3.one, 1f, 1f))
            .ChainCallback(() =>
            {
                countdownText.SetText("GO!");
                audioSource.clip = startRaceHighSound;
                audioSource.Play();
                roundStartTime = Time.time;
                nextSpawnTime = Time.time;
                isRoundActive = true;
                timerText.gameObject.SetActive(true);
            })
            .ChainDelay(0.25f)
            .Chain(Tween.Alpha(countdownText, 0, 0.5f));
    }

    private void SpawnVoleServer()
    {
        VoleHole startingHole = spawnedHoles[Random.Range(0, spawnedHoles.Count - 1)];
        Vole spawnedVole = Instantiate(volePrefab, startingHole.transform.position, Quaternion.identity);
        spawnedVole.NetworkObject.Spawn();
        spawnedVoles.Add(spawnedVole);
    }

    public void RemoveVole(Vole vole)
    {
        spawnedVoles.Remove(vole);
    }

    [ClientRpc]
    public void GivePointsClientRpc(ulong clientId, int points)
    {
        playerScores[clientId] += points;
        playerList.SetPlayerScores(playerScores);
    }

    private void EndRound()
    {
        foreach(Vole vole in spawnedVoles)
        {
            vole.NetworkObject.Despawn();
        }

        for(int index = 0; index < spawnedHoles.Count; index++)
        {
            VoleHole hole = spawnedHoles[index];
            hole.PlayDespawnAnimation(index * 0.25f);
        }

        spawnedVoles.Clear();
        spawnedHoles.Clear();
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
            ShowNewRoundUI();
            return;
        }

        bool hasTieWithAnotherPlayer = false;
        bool isScoreHighest = true;

        IEnumerable<KeyValuePair<ulong, int>> otherPlayers = playerScores.Where(scoreEntry => scoreEntry.Key != NetworkManager.Singleton.LocalClientId);
        foreach(KeyValuePair<ulong, int> scoreEntry in otherPlayers)
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
            audioSource.clip = successSound;
            audioSource.Play();
        } else if(isScoreHighest)
        {
            countdownText.SetText("Winner!");
            audioSource.clip = successSound;
            audioSource.Play();
        } else
        {
            countdownText.SetText("Loser!");
            audioSource.clip = failureSound;
            audioSource.Play();
        }

        countdownText.gameObject.SetActive(true);
        countdownText.alpha = 1;
        Tween.PunchScale(countdownText.transform, Vector3.one, 1f, 1f, true, Ease.Default, 0, 3)
            .Chain(Tween.Alpha(countdownText, 0, 1f, Ease.Default, 1, CycleMode.Restart, 3f))
            .ChainCallback(ShowNewRoundUI);
    }
}
