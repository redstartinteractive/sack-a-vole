using System;
using PrimeTween;
using UnityEngine;

public class VoleHole: MonoBehaviour
{
    private ParticleSystem particles;

    private void Awake()
    {
        particles = GetComponentInChildren<ParticleSystem>();
    }

    public void PlaySpawnAnimation(Vector3 position, float startDelay)
    {
        transform.position = position + Vector3.down;

        ParticleSystem.MainModule main = particles.main;
        main.startDelay = startDelay;
        particles.Play();

        Tween.PositionY(transform, position.y, 1f, Ease.OutSine, 1, CycleMode.Restart, startDelay);
    }

    public void RemoveFromGame(float startDelay)
    {
        ParticleSystem.MainModule main = particles.main;
        main.startDelay = startDelay;
        particles.Play();

        Tween.PositionY(transform, transform.position.y - 1f, 1f, Ease.InSine, 1, CycleMode.Restart, startDelay)
            .OnComplete(() => { Destroy(gameObject); });
    }
}
