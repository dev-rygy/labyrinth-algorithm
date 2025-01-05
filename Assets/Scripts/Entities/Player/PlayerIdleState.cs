/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025
 * Notes:           Player Idle State
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is the default state for the player. Every state the player transitions to
/// will go back to this state by default. In this state the player is not pressing any
/// input besides the movement input.
/// </summary>
public class PlayerIdleState : PlayerState
{
    // Animator Hash Codes
    private readonly int ANIM_IDLE_BLEND_TREE_HASH = Animator.StringToHash("Idle Blend Tree");
    private readonly int ANIM_IDLE_SPEED_HASH = Animator.StringToHash("IdleSpeed");

    // Time to transition between animtions
    private const float ANIMATOR_DAMP_TIME = 0.1f;

    // Constuctor
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Play the Running blend tree animations
        stateMachine.Animator.CrossFadeInFixedTime(ANIM_IDLE_BLEND_TREE_HASH, 0.1f);
    }

    public override void Tick(float deltaTime)
    {
        // Translate the movement input to the correct world plane and move the player 
        Vector3 moveInput = new Vector3(stateMachine.Input.MovementInput.x, 0, stateMachine.Input.MovementInput.y);
        Move(moveInput * stateMachine.MovementSpeed, deltaTime);
        ApplyGravity(deltaTime);

        // If the player has no movement input enter the idle animation
        if (moveInput == Vector3.zero)
        {
            stateMachine.Animator.SetFloat(ANIM_IDLE_SPEED_HASH, 0f, ANIMATOR_DAMP_TIME, deltaTime);
            return;
        }

        // Rotate the player character in the direction of movement
        ApplyCharacterRotation(moveInput, deltaTime);

        // If the player has movement then play movement animation
        stateMachine.Animator.SetFloat(ANIM_IDLE_SPEED_HASH, 1f, ANIMATOR_DAMP_TIME, deltaTime);    }

    public override void Exit()
    {

    }
}
