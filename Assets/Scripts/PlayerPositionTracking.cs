using Unity.Netcode.Components;
using UnityEngine;

public class PlayerPositionTracking : NetworkTransform
{
    private Transform arCameraTransform;

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

    public override void OnNetworkSpawn()
    {
        if(IsOwner)
        {
            if(Camera.main)
            {
                arCameraTransform = Camera.main.transform;
            }
        }

        base.OnNetworkSpawn();
    }

    protected override void Update()
    {
        if(IsOwner)
        {
            if(arCameraTransform)
            {
                arCameraTransform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
                transform.SetPositionAndRotation(pos, rot);
            }
        }

        base.Update();
    }
}