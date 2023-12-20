using System;
using System.Collections;
using System.Collections.Generic;
using Niantic.Lightship.SharedAR.Colocalization;
using PrimeTween;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Random = UnityEngine.Random;

public class LobbyController : NetworkBehaviour
{
    [SerializeField] private int minPlayerCount = 2;
    [Header("Start New Game")]
    [SerializeField] private CanvasGroup newGameCanvasGroup;
    [SerializeField] private Button hostRoomButton;
    [SerializeField] private Button joinRoomButton;
    [Header("Lobby Setup")]
    [SerializeField] private CanvasGroup lobbyInfoGroup;
    [SerializeField] private TextMeshProUGUI lobbyInfoText;
    [SerializeField] private TMP_InputField roomCodeInputField;
    [SerializeField] private PlayerListController playerList;
    [SerializeField] private CanvasGroup playerListGroup;
    [Header("Colocalization Setup")]
    [SerializeField] private SharedSpaceManager sharedSpaceManager;
    [SerializeField] private Texture2D targetImage;
    [SerializeField] private float targetImageSize;
    [SerializeField] private CanvasGroup colocalizeGroup;
    [SerializeField] private GameObject sharedOriginPrefab;
    [Header("Environment Scanning")]
    [SerializeField] private CanvasGroup scanCanvasGroup;
    [SerializeField] private Slider scanPercentSlider;
    [SerializeField] private ARMeshManager meshManager;

    public event Action OnAllPlayersReady;

    private Dictionary<ulong, bool> clientsInLobby;
    private bool hasAllPlayersInLobby;
    private string lobbyCode;
    private bool startAsHost;
    private readonly WaitForSeconds waitForMeshArea = new(.25f);

    private const float k_MinimumMeshArea = 1.75f;

    private void Awake()
    {
        newGameCanvasGroup.gameObject.SetActive(false);
        lobbyInfoGroup.gameObject.SetActive(false);
        scanCanvasGroup.gameObject.SetActive(false);
        playerListGroup.gameObject.SetActive(false);
        colocalizeGroup.gameObject.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        clientsInLobby = new Dictionary<ulong, bool> { { NetworkManager.LocalClientId, false } };

        if(!IsServer) return;

        hasAllPlayersInLobby = false;
        NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
    }

    public void ShowHostOrJoinUI(bool show = true)
    {
        AnimateCanvasGroup(newGameCanvasGroup, show);
        if(show)
        {
            hostRoomButton.onClick.AddListener(StartNewRoom);
            joinRoomButton.onClick.AddListener(JoinRoom);
        }
    }

    private void StartNewRoom()
    {
        // generate a new room name 3 digit number
        int code = (int)Random.Range(0.0f, 999.0f);
        SetLobbyCode(code.ToString("D3"));
        ShowHostOrJoinUI(false);
        startAsHost = true;
        StartNetworkSharedSpace();
    }

    private void JoinRoom()
    {
        // TODO display input validation errors
        if(roomCodeInputField.text.Length <= 0)
        {
            return;
        }

        SetLobbyCode(roomCodeInputField.text);
        ShowHostOrJoinUI(false);
        startAsHost = false;
        StartNetworkSharedSpace();
    }

    private void SetLobbyCode(string code)
    {
        lobbyCode = code;
        lobbyInfoText.SetText($"Lobby PIN: {lobbyCode}");
        lobbyInfoGroup.gameObject.SetActive(true);
    }

    private void StartNetworkSharedSpace()
    {
        ISharedSpaceTrackingOptions imageTrackingOptions =
            ISharedSpaceTrackingOptions.CreateImageTrackingOptions(targetImage, targetImageSize);

        ISharedSpaceRoomOptions roomOptions =
            ISharedSpaceRoomOptions.CreateLightshipRoomOptions(lobbyCode + "SackAVole", 4, "Sack A Vole Room");

        sharedSpaceManager.sharedSpaceManagerStateChanged += OnColocalizationTrackingStateChanged;
        sharedSpaceManager.StartSharedSpace(imageTrackingOptions, roomOptions);

        if(startAsHost)
        {
            NetworkManager.Singleton.StartHost();
        } else
        {
            NetworkManager.Singleton.StartClient();
        }

        AnimateCanvasGroup(colocalizeGroup, true);
    }

    private void OnColocalizationTrackingStateChanged(SharedSpaceManager.SharedSpaceManagerStateChangeEventArgs args)
    {
        if(!args.Tracking) return;
        AnimateCanvasGroup(colocalizeGroup, false);
        Instantiate(sharedOriginPrefab, sharedSpaceManager.SharedArOriginObject.transform, false);
        ScanEnvironmentForMesh();
        sharedSpaceManager.sharedSpaceManagerStateChanged -= OnColocalizationTrackingStateChanged;
    }

    private void ScanEnvironmentForMesh()
    {
        AnimateCanvasGroup(scanCanvasGroup, true);
        meshManager.gameObject.SetActive(true);
        GameManager.Instance.NavMeshManager.enabled = true;

        StartCoroutine(WaitForMeshArea());

        IEnumerator WaitForMeshArea()
        {
            while(GameManager.Instance.NavMeshManager.LightshipNavMesh == null)
            {
                yield return waitForMeshArea;
            }

            while(GameManager.Instance.NavMeshManager.LightshipNavMesh.Area < k_MinimumMeshArea)
            {
                float percentScanned = GameManager.Instance.NavMeshManager.LightshipNavMesh.Area / k_MinimumMeshArea;
                scanPercentSlider.value = Mathf.Clamp01(percentScanned);
                yield return waitForMeshArea;
            }

            scanPercentSlider.value = 1;
            AnimateCanvasGroup(scanCanvasGroup, false);
            SetPlayerReady();
        }
    }

    private void SetPlayerReady()
    {
        clientsInLobby[NetworkManager.Singleton.LocalClientId] = true;

        if(IsServer)
        {
            UpdateAndCheckPlayersInLobby();
        } else
        {
            OnClientIsReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if(!IsServer) return;

        clientsInLobby.TryAdd(clientId, false);
        UpdateAndCheckPlayersInLobby();
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnClientIsReadyServerRpc(ulong clientId)
    {
        if(!clientsInLobby.ContainsKey(clientId)) return;

        clientsInLobby[clientId] = true;
        UpdateAndCheckPlayersInLobby();
    }

    private void UpdateAndCheckPlayersInLobby()
    {
        hasAllPlayersInLobby = clientsInLobby.Count >= minPlayerCount;

        foreach(KeyValuePair<ulong, bool> clientLobbyStatus in clientsInLobby)
        {
            SendClientReadyStatusUpdatesClientRpc(clientLobbyStatus.Key, clientLobbyStatus.Value);
            if(!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientLobbyStatus.Key))
            {
                hasAllPlayersInLobby = false;
            }
        }

        PopulatePlayerListUI();
        CheckForAllPlayersReady();
    }

    [ClientRpc]
    private void SendClientReadyStatusUpdatesClientRpc(ulong clientId, bool isReady)
    {
        if(IsServer) return;
        clientsInLobby[clientId] = isReady;
        PopulatePlayerListUI();
    }

    private void PopulatePlayerListUI()
    {
        playerList.SetupPlayers(clientsInLobby);
        AnimateCanvasGroup(playerListGroup, true);
    }

    private void CheckForAllPlayersReady()
    {
        if(!hasAllPlayersInLobby) return;

        bool allPlayersAreReady = true;
        foreach(KeyValuePair<ulong, bool> clientLobbyStatus in clientsInLobby)
        {
            if(!clientLobbyStatus.Value)
            {
                allPlayersAreReady = false;
            }
        }

        if(!allPlayersAreReady) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        SendAllPlayersReadyClientRpc();
    }

    [ClientRpc]
    private void SendAllPlayersReadyClientRpc()
    {
        OnAllPlayersReady?.Invoke();
    }

    private void AnimateCanvasGroup(CanvasGroup canvasGroup, bool setDisplayed)
    {
        if(setDisplayed == canvasGroup.gameObject.activeSelf)
        {
            return;
        }

        Tween.CompleteAll(canvasGroup);
        canvasGroup.interactable = setDisplayed;
        canvasGroup.blocksRaycasts = setDisplayed;
        canvasGroup.alpha = setDisplayed ? 0f : 1f;
        if(setDisplayed)
        {
            canvasGroup.gameObject.SetActive(true);
        }

        Tween.Alpha(canvasGroup, setDisplayed ? 1f : 0f, 0.5f)
            .OnComplete(() =>
            {
                if(!setDisplayed) canvasGroup.gameObject.SetActive(false);
            });
    }
}
