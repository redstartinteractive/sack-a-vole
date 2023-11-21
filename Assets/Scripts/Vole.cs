using UnityEngine;
using Niantic.Lightship.AR.NavigationMesh;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class Vole : NetworkBehaviour
{
    [SerializeField] private int scoreValue = 1;
    [SerializeField] private float moveMaxDistance = 0.5f;
    [SerializeField] private Vector2 updateIntervalRange = new(2, 3);

    private LightshipNavMeshAgent navMeshAgent;
    private LightshipNavMeshManager navMeshManager;

    private float nextMoveTime;
    private bool wasSacked;

    private void Awake()
    {
        navMeshAgent = GetComponent<LightshipNavMeshAgent>();
    }

    private void Start()
    {
        navMeshManager = GameManager.Instance.NavMeshManager;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(!IsHost)
        {
            navMeshAgent.enabled = false;
        }
    }

    private void Update()
    {
        if(!IsHost || wasSacked) return;
        if(Time.time < nextMoveTime) return;

        SetRandomDestination();
    }

    public void SetDestination(Vector3 point)
    {
        navMeshAgent.SetDestination(point);
        nextMoveTime = Time.time + Random.Range(updateIntervalRange.x, updateIntervalRange.y);
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
        nextMoveTime = Time.time + Random.Range(updateIntervalRange.x, updateIntervalRange.y);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SackServerRpc(ulong clientId)
    {
        if(wasSacked) return;
        gameObject.SetActive(false);
        GameManager.Instance.RoundManager.GivePointsClientRpc(clientId, scoreValue);
        GameManager.Instance.RoundManager.RemoveVole(this);
        wasSacked = true;
        NetworkObject.Despawn();
    }

    public void RemoveFromGame()
    {
        // TODO spawn dust particles on all clients
        NetworkObject.Despawn();
    }
}
