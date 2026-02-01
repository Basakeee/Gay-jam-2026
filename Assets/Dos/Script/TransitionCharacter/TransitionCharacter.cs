using System;
using UnityEngine;

public class TransitionCharacter : MonoBehaviour
{
    private Animator _animator;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    public void TriggerDashTransition()
    {
        _animator.SetTrigger("DashTransition");
    }

    public void TriggerHookTranstion()
    {
        _animator.SetTrigger("HookTransition");
    }

    public void TriggerPlatformTransition()
    {
        _animator.SetTrigger("PlatformTransition");
    }
}
