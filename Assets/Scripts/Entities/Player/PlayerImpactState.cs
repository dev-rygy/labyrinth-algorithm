/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/06/2025
 * Last Modified:   02/06/2025 (Ryan)
 * Notes:           Player Idle State
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerImpactState : PlayerState
{
    private readonly int k_animImpactHash = Animator.StringToHash("Impact");

    // Time to transition between animations
    private const float k_animatorCrossfadeDuration = 0.1f;

    private float duration = 1f;

    public PlayerImpactState(PlayerStateMachine stateMachine) : base(stateMachine) { }
    public override void Enter()
    {
        // Play the impact animation
        stateMachine.Animator.CrossFadeInFixedTime(k_animImpactHash, k_animatorCrossfadeDuration);
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.Move(Vector3.zero, deltaTime);     // Continue to apply gravity

        duration -= deltaTime;      // Decrement duration of the impact every frame

        if (duration <= 0)  // Impact done? Return to idle
        {
            stateMachine.TransitionStates(PlayerStates.Idle);
        }
    }

    public override void Exit() { }
}
