/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/17/2025
 * Last Modified:   02/19/2025 (Ryan)
 * Notes:           Secondary Power Ability for the Buckler
*/
using RyansLibrary.Abilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuckerPowerSecondaryAbility", menuName = "Scriptable Objects/Ability/Weapons/Bucker/BuckerPowerSecondaryAbility", order = 1)]
public class BuckerPowerSecondary : Ability<PlayerStateMachine>
{
    [SerializeField] private string animHash;

    public override void Enter()
    {
        stateMachine = PlayerStateMachine.Instance;

        stateMachine.AnimationTimestamps.OnAnimationExit += PowerExit;

        // Play the attack's animation
        stateMachine.Animator.CrossFadeInFixedTime(animHash, 0.1f);
    }

    public override void Tick(float deltaTime)
    {
        // Apply ambient forces
        stateMachine.ApplyAmbientForces(deltaTime);
    }

    public override void Exit()
    {
        StartCooldown();

        stateMachine.AnimationTimestamps.OnAnimationExit -= PowerExit;
    }

    private void PowerExit()
    {
        stateMachine.TransitionStates(PlayerStates.Idle);
    }
}
