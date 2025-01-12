using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbState : PlayerState
{
    // Animator Hash Codes
    private readonly int ANIM_CLIMB_BLEND_TREE_HASH = Animator.StringToHash("Climbing Blend Tree");
    private readonly int ANIM_CLIMB_SPEED_HASH = Animator.StringToHash("ClimbSpeed");

    // Time to transition between animtions
    private const float ANIMATOR_DAMP_TIME = 0f;

    // Constructor
    public PlayerClimbState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        InputHandler.OnInteract1 += CancelClimb;

        // Turn off gravity
        stateMachine.ForceReciever.HasGravity = false;

        // Play the Running blend tree animations
        stateMachine.Animator.CrossFadeInFixedTime(ANIM_CLIMB_BLEND_TREE_HASH, 0.1f);
    }

    public override void Tick(float deltaTime)
    {
        // Move the player up and down based on the normalized movement input
        Vector3 moveInput = new Vector3(0, stateMachine.Input.MovementInputNormalized.y, 0);
        stateMachine.Move(moveInput * stateMachine.ClimbSpeed, deltaTime);

        // If the player has movement then play running animation
        stateMachine.Animator.SetFloat(ANIM_CLIMB_SPEED_HASH, Mathf.Round(moveInput.y), ANIMATOR_DAMP_TIME, deltaTime);    // Run Animation

        if (!stateMachine.CanClimb())
            CancelClimb();
    }

    public override void Exit()
    {
        // Turn gravity back on
        stateMachine.ForceReciever.HasGravity = true;

        InputHandler.OnInteract1 -= CancelClimb;
    }

    private void CancelClimb()
    {
        // TODO: Enter Falling State if not grounded

        stateMachine.SwitchState(new PlayerIdleState(stateMachine));
    }
}
