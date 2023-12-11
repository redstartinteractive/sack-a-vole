using Niantic.Lightship.AR.NavigationMesh;
using Unity.Netcode;
using UnityEngine;

public class Vole : NetworkBehaviour
{
    [SerializeField] private int scoreValue = 1;
    [SerializeField] private float moveMaxDistance = 0.5f;
    [SerializeField] private Vector2 updateIntervalRange = new(0.5f, 2);
    [SerializeField] private Vector2 maxLifetimeRange = new(2, 3);
    [SerializeField] private float sackAnimationTime = 1f;

    private AudioSource audioSource;
    private bool isGoingHome;
    private float lerpTime;

    private LightshipNavMeshAgent navMeshAgent;
    private LightshipNavMeshManager navMeshManager;

    private float nextMoveTime;
    private float returnToHoleTime;
    private Transform sackTargetTransform;
    private Vector3 startingHolePosition;
    private bool wasSacked;
    private Animator animator;
    private Vector2 lastPos;

    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int Sack = Animator.StringToHash("IsSacked");
    private const float k_SackTargetOffsetY = -0.25f;

    private void Awake()
    {
        navMeshAgent = GetComponent<LightshipNavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(!IsServer)
        {
            navMeshAgent.StopMoving();
            navMeshAgent.enabled = false;
        }

        navMeshManager = GameManager.Instance.NavMeshManager;

        startingHolePosition = transform.position;
        lastPos = startingHolePosition;
        NetworkObject.TrySetParent(GameManager.Instance.SharedSpaceManager.SharedArOriginObject);

        returnToHoleTime = Time.time + Random.Range(maxLifetimeRange.x, maxLifetimeRange.y);
        nextMoveTime = Time.time + Random.Range(updateIntervalRange.x, updateIntervalRange.y);
    }

    private void Update()
    {
        UpdateAnimations();

        if(!IsServer) return;

        UpdateMovement();
    }

    private void UpdateAnimations()
    {
        animator.SetBool(Sack, wasSacked);
        if(wasSacked) return;

        Vector2 pos = new(gameObject.transform.position.x, gameObject.transform.position.z);
        float dist = (pos - lastPos).magnitude;
        lastPos = pos;

        float speed = dist / Time.deltaTime;
        animator.SetBool(IsRunning, speed > 0);
    }

    private void UpdateMovement()
    {
        if(wasSacked && sackTargetTransform)
        {
            lerpTime += Time.deltaTime;
            Vector3 targetPos = sackTargetTransform.position + Vector3.down * k_SackTargetOffsetY;
            float clampedTime = Mathf.Clamp01(lerpTime / sackAnimationTime);
            transform.position = Vector3.Lerp(transform.position, targetPos, clampedTime);
            if(clampedTime >= 1)
            {
                RemoveFromGame();
            }

            return;
        }

        if(isGoingHome)
        {
            if(navMeshAgent.State == LightshipNavMeshAgent.AgentNavigationState.Idle)
            {
                // Arrived at home
                RemoveFromGame();
            }

            return;
        }

        if(!wasSacked && Time.time >= returnToHoleTime)
        {
            navMeshAgent.SetDestination(startingHolePosition);
            isGoingHome = true;
        }

        if(!wasSacked && Time.time >= nextMoveTime)
        {
            SetRandomDestination();
            nextMoveTime = Time.time + Random.Range(updateIntervalRange.x, updateIntervalRange.y);
        }
    }

    private void SetRandomDestination()
    {
        if(navMeshManager.LightshipNavMesh == null)
        {
            return;
        }

        navMeshManager.LightshipNavMesh.FindRandomPosition(transform.position, moveMaxDistance,
            out Vector3 randomPosition);

        navMeshAgent.SetDestination(randomPosition);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SackServerRpc(ulong clientId)
    {
        if(wasSacked) return;

        wasSacked = true;
        navMeshAgent.StopMoving();
        navMeshAgent.enabled = false;
        // Give points immediately, on the server only
        GameManager.Instance.RoundManager.GivePointsClientRpc(clientId, scoreValue);

        // Start sack animation
        lerpTime = 0;
        PlayerPositionTracking playerPos = GameManager.Instance.PlayerPositions[clientId];
        sackTargetTransform = playerPos.transform;

        // Remove from gameplay, but don't despawn yet
        GameManager.Instance.RoundManager.RemoveVole(this);

        PlaySackSoundClientRpc();
    }

    [ClientRpc]
    private void PlaySackSoundClientRpc()
    {
        audioSource.Play();
    }

    private void RemoveFromGame()
    {
        GameManager.Instance.RoundManager.RemoveVole(this);
        NetworkObject.Despawn();
    }
}
