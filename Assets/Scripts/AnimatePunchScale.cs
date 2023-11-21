using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.Serialization;

public class AnimatePunchScale : MonoBehaviour
{
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private ShakeSettings animationSettings;

    private void OnEnable()
    {
        if(playOnEnable)
        {
            Tween.PunchScale(transform, animationSettings);
        }
    }

    private void OnDisable()
    {
        Tween.CompleteAll(transform);
    }

    public void PlayAnimation()
    {
        Tween.PunchScale(transform, animationSettings);
    }

    public void StopAnimation()
    {
        Tween.CompleteAll(transform);
    }
}
