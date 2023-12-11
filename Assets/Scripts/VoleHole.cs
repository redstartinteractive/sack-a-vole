using PrimeTween;
using Unity.Netcode;
using UnityEngine;

public class VoleHole : NetworkBehaviour
{
    private ParticleSystem particles;

    private void Awake()
    {
        particles = GetComponentInChildren<ParticleSystem>();
    }

    public void PlaySpawnAnimation(Vector3 position, float startDelay)
    {
        transform.position = position + Vector3.down;
        Tween.PositionY(transform, position.y, 1f, Ease.OutSine, 1, CycleMode.Restart, startDelay);
        PlaySpawnParticlesClientRpc(startDelay);
    }

    public void PlayDespawnAnimation(float startDelay)
    {
        Tween.PositionY(transform, transform.position.y - 1f, 1f, Ease.InSine, 1, CycleMode.Restart, startDelay)
            .OnComplete(() => { NetworkObject.Despawn(); });

        PlaySpawnParticlesClientRpc(startDelay);
    }

    [ClientRpc]
    private void PlaySpawnParticlesClientRpc(float startDelay)
    {
        ParticleSystem.MainModule main = particles.main;
        main.startDelay = startDelay;
        particles.Play();
    }
}
