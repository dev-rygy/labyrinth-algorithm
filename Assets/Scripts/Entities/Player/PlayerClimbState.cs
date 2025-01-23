/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025 (Ryan)
 * Notes:           Player Climbing State
*/
using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerClimbState : PlayerState
{
    // Animator Hash Codes
    private readonly int ANIM_CLIMB_BLEND_TREE_HASH = Animator.StringToHash("Climbing Blend Tree");
    private readonly int ANIM_CLIMB_SPEED_HASH = Animator.StringToHash("ClimbSpeed");

    // Time to transition between animtions
    private const float ANIMATOR_DAMP_TIME = 0f;

    private const float FINAL_PUSH_FORCE = 3f;
    private const float FINAL_PUSH_TIME = 0.3f;

    private bool _doneClimbing = false;

    // Constructor
    public PlayerClimbState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        InputHandler.OnInteract1 += CancelClimb;

        // Turn off gravity
        stateMachine.ForceReciever.HasGravity = false;

        stateMachine.SheatheWeapons();

        // Play the Running blend tree animations
        stateMachine.Animator.CrossFadeInFixedTime(ANIM_CLIMB_BLEND_TREE_HASH, 0.1f);
    }

    public override void Tick(float deltaTime)
    {
        if (_doneClimbing)
        {
            stateMachine.Move(Vector3.zero, deltaTime);
            return;
        }

        // Move the player up and down based on the normalized movement input
        Vector3 moveInput = new Vector3(0, stateMachine.Input.MovementInputNormalized.y, 0);
        stateMachine.Move(moveInput * stateMachine.ClimbSpeed, deltaTime);

        // If the player has movement then play running animation
        stateMachine.Animator.SetFloat(ANIM_CLIMB_SPEED_HASH, Mathf.Round(moveInput.y), ANIMATOR_DAMP_TIME, deltaTime);    // Run Animation

        if (!stateMachine.CanClimb())
        {
           _doneClimbing = true;
           stateMachine.ForceReciever.HasGravity = true;
           stateMachine.ForceReciever.AddForce((stateMachine.PlayerCharacter.transform.forward + stateMachine.PlayerCharacter.transform.up) * FINAL_PUSH_FORCE);
           stateMachine.StartCoroutine(FinishCo());
        }
    }

    private IEnumerator FinishCo()
    {
        stateMachine.ForceReciever.EnableGroundCheck = false;
        yield return new WaitForSeconds(FINAL_PUSH_TIME);
        stateMachine.ForceReciever.EnableGroundCheck = true;
        CancelClimb();
    }

    public override void Exit()
    {
        // Turn gravity back on
        stateMachine.ForceReciever.HasGravity = true;

        stateMachine.UnsheatheWeapons();

        InputHandler.OnInteract1 -= CancelClimb;
    }

    private void CancelClimb()
    {
        stateMachine.SwitchState(new PlayerIdleState(stateMachine));
    }
}
