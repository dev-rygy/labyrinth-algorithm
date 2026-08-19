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
    private readonly int k_animClimbBlendTreeHash = Animator.StringToHash("Climbing Blend Tree");
    private readonly int k_animClimbSpeedHash = Animator.StringToHash("ClimbSpeed");

    // Time to transition between animtions
    private const float k_animatorDampTime = 0f;

    private const float k_finalPushForce = 3f;
    private const float k_finalPushTime = 0.3f;

    private bool _doneClimbing = false;

    // Constructor
    public PlayerClimbState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        InputHandler.OnInteract1 += CancelClimb;

        _doneClimbing = false;      // reset flag

        // Turn off gravity
        stateMachine.ForceReciever.HasGravity = false;

        stateMachine.SheatheWeapons();

        // Play the Running blend tree animations
        stateMachine.Animator.CrossFadeInFixedTime(k_animClimbBlendTreeHash, 0.1f);
    }

    public override void Tick(float deltaTime)
    {
        if (_doneClimbing)
        {
            stateMachine.Move(Vector3.zero, deltaTime);
            return;
        }

        // Move the player up and down based on the normalized movement input
        Vector3 moveInput = new Vector3(0, stateMachine.Input.MoveDirectionNormalized.y, 0);
        stateMachine.Move(moveInput * stateMachine.ClimbSpeed, deltaTime);

        // If the player has movement then play running animation
        stateMachine.Animator.SetFloat(k_animClimbSpeedHash, Mathf.Round(moveInput.y), k_animatorDampTime, deltaTime);    // Run Animation

        if (!stateMachine.CanClimb())
        {
           _doneClimbing = true;
           stateMachine.ForceReciever.HasGravity = true;
           stateMachine.ForceReciever.AddForce((stateMachine.PlayerCharacter.transform.forward + stateMachine.PlayerCharacter.transform.up) * k_finalPushForce);
           stateMachine.StartCoroutine(FinishCo());
        }
    }

    private IEnumerator FinishCo()
    {
        stateMachine.ForceReciever.EnableGroundCheck = false;
        yield return new WaitForSeconds(k_finalPushTime);
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
        stateMachine.TransitionStates(PlayerStates.Idle);
    }
}
