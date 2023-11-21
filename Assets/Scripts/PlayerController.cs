using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private InputAction clickAction;
    [SerializeField] private InputAction positionAction;

    private int rayCastMask;

    private void Awake()
    {
        rayCastMask = LayerMask.GetMask("Default");
    }

    public override void OnNetworkSpawn()
    {
        if(IsOwner)
        {
            clickAction.Enable();
            positionAction.Enable();
            clickAction.performed += OnTapped;
        }

        base.OnNetworkSpawn();
    }

    private void OnTapped(InputAction.CallbackContext ctx)
    {
        Vector2 touchPosition = positionAction.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        Physics.Raycast(ray, out RaycastHit hitInfo, 200f, rayCastMask);
        if(!hitInfo.collider || !hitInfo.collider.TryGetComponent(out Vole tappedVole))
        {
            return;
        }

        tappedVole.SackServerRpc(OwnerClientId);
    }
}
