using System.Collections.Generic;
using Niantic.Lightship.AR.NavigationMesh;
using Niantic.Lightship.SharedAR.Colocalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private LobbyController lobbyController;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private SharedSpaceManager sharedSpaceManager;
    [Space]
    [SerializeField] private Button debugLogToggleButton;
    [SerializeField] private TextMeshProUGUI debugLogText;
    [SerializeField] private CanvasGroup debugLogCanvasGroup;
    public readonly Dictionary<ulong, PlayerPositionTracking> PlayerPositions = new();

    public LightshipNavMeshManager NavMeshManager { get; private set; }
    public RoundManager RoundManager => roundManager;
    public SharedSpaceManager SharedSpaceManager => sharedSpaceManager;

    protected override void Initialize()
    {
        base.Initialize();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        NavMeshManager = GetComponent<LightshipNavMeshManager>();
        NavMeshManager.enabled = false;
    }

    protected void Start()
    {
        lobbyController.ShowHostOrJoinUI();
        lobbyController.OnAllPlayersReady += OnAllPlayersReady;

        Application.logMessageReceived += OnLogMessage;
        debugLogToggleButton.onClick.AddListener(OnDebugLogClicked);
    }

    private void OnAllPlayersReady()
    {
        roundManager.ShowNewRoundUI();
    }

    private void OnDebugLogClicked()
    {
        bool setActive = !debugLogCanvasGroup.interactable;
        debugLogCanvasGroup.interactable = setActive;
        debugLogCanvasGroup.blocksRaycasts = setActive;
        debugLogCanvasGroup.alpha = setActive ? 1 : 0;
    }

    private void OnLogMessage(string condition, string stacktrace, LogType type)
    {
        debugLogText.text += $"[{type}] {condition}\n\n";
    }
}
