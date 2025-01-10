/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025
 * Notes:           Player Idle State
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RyansLibrary.Input;

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
        // Input Events
        InputHandler.OnComboPrimary += OnComboPrimary;
        InputHandler.OnComboSecondary += OnComboSecondary;
        InputHandler.OnPowerPrimary += OnPoweredPrimary;
        InputHandler.OnPowerSecondary += OnPoweredSecondary;

        // Play the Running blend tree animations
        stateMachine.Animator.CrossFadeInFixedTime(ANIM_IDLE_BLEND_TREE_HASH, 0.1f);

        // Switch state immediately if holding down combo button
        if (stateMachine.Input.IsHoldingPrimaryCombo)
            OnComboPrimary();
        else if (stateMachine.Input.IsHoldingSecondaryCombo)
            OnComboSecondary();
        else if (stateMachine.Input.IsHoldingPrimaryPower)
            OnPoweredPrimary();
        else if (stateMachine.Input.IsHoldingSecondaryPower)
            OnPoweredSecondary();
    }

    public override void Tick(float deltaTime)
    {
        // Translate the movement input to the correct world plane and move the player 
        Vector3 moveInput = new Vector3(stateMachine.Input.MovementInput.x, 0, stateMachine.Input.MovementInput.y);
        stateMachine.Move(moveInput * stateMachine.MovementSpeed, deltaTime);

        // The value that transitions the player to and from the idle/walking/running animations
        float playerActualSpeed = moveInput.magnitude;

        // If the player has movement then play running animation 
        stateMachine.Animator.SetFloat(ANIM_IDLE_SPEED_HASH, playerActualSpeed, ANIMATOR_DAMP_TIME, deltaTime);    // Run Animation

        if (moveInput == Vector3.zero)  // Code below only needed if the player is moving
            return;

        // Rotate the player character in the normalized direction of movement
        Vector3 moveInputNormalized = new Vector3(stateMachine.Input.MovementInputNormalized.x, 0, stateMachine.Input.MovementInputNormalized.y);
        stateMachine.ApplyCharacterRotation(moveInputNormalized, deltaTime);
    }

    public override void Exit()
    {
        // Unsubscribe to transition events
        InputHandler.OnComboPrimary -= OnComboPrimary;
        InputHandler.OnComboSecondary -= OnComboSecondary;
        InputHandler.OnPowerPrimary -= OnPoweredPrimary;
        InputHandler.OnPowerSecondary -= OnPoweredSecondary;
    }

    // Transition Functions
    private void OnComboPrimary()
    {
        stateMachine.SwitchState(new PlayerComboPrimaryState(stateMachine));
    }

    private void OnComboSecondary()
    {
        stateMachine.SwitchState(new PlayerComboSecondaryState(stateMachine));
    }

    private void OnPoweredPrimary()
    {
        stateMachine.SwitchState(new PlayerPowerPrimaryState(stateMachine));
    }

    private void OnPoweredSecondary()
    {
        stateMachine.SwitchState(new PlayerPowerSecondaryState(stateMachine));
    }
}
