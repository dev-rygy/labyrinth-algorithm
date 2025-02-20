/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   02/11/2025 (Ryan)
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

    // Blend tree damp time
    private const float ANIMATOR_DAMP_TIME = 0.1f;
    // Time to transition between animations
    private const float ANIMATOR_CROSSFADE_DURATION = 0.1f;

    // Constuctor
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {   
        // Input Events
        InputHandler.OnComboPrimary += OnComboPrimary;          // Input: right_shoulder 
        InputHandler.OnComboSecondary += OnComboSecondary;      // Input: left_shoulder
        InputHandler.OnPowerPrimary += OnPoweredPrimary;        // Input: right_trigger
        InputHandler.OnPowerSecondary += OnPoweredSecondary;    // Input: left_trigger
        InputHandler.OnDash += OnDash;                          // Input: dash
        InputHandler.OnInteract1 += OnInteract1;                // Input: interact
        InputHandler.OnEmote += OnEmote;                        // Input: emote

        // Play the Running blend tree animations
        stateMachine.Animator.CrossFadeInFixedTime(ANIM_IDLE_BLEND_TREE_HASH, ANIMATOR_CROSSFADE_DURATION);

        // Switch state immediately if holding down combo button
        if (stateMachine.Input.IsHoldingPrimaryCombo)           // Input: right_shoulder
            OnComboPrimary();
        else if (stateMachine.Input.IsHoldingSecondaryCombo)    // Input: left_shoulder
            OnComboSecondary();
        else if (stateMachine.Input.IsHoldingPrimaryPower)      // Input: right_trigger
            OnPoweredPrimary();
        else if (stateMachine.Input.IsHoldingSecondaryPower)    // Input: left_trigger
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

        if (moveInput != Vector3.zero)  // Code below only needed if the player is moving
        {
            // Rotate the player character in the normalized direction of movement
            Vector3 moveInputNormalized = new Vector3(stateMachine.Input.MoveDirectionNormalized.x, 0, stateMachine.Input.MoveDirectionNormalized.y);
            stateMachine.ApplyCharacterRotation(moveInputNormalized, deltaTime);
        }

        // Falling state if the player is not grounded
        if (!stateMachine.ForceReciever.IsGrounded())
            stateMachine.TransitionStates(PlayerStates.Fall);
    }

    public override void Exit()
    {
        // Unsubscribe to transition events
        InputHandler.OnComboPrimary -= OnComboPrimary;
        InputHandler.OnComboSecondary -= OnComboSecondary;
        InputHandler.OnPowerPrimary -= OnPoweredPrimary;
        InputHandler.OnPowerSecondary -= OnPoweredSecondary;
        InputHandler.OnDash -= OnDash;
        InputHandler.OnInteract1 -= OnInteract1;
        InputHandler.OnEmote -= OnEmote;
    }

    // Transition Functions
    private void OnComboPrimary()
    {
        // If the ability is on cooldown then don't use at this time
        if (stateMachine.ComboAttackPrimaryAbility.OnCooldown)
        {
            if (stateMachine.DebugStateMachine) Debug.Log("Primary Combo Attack on Cooldown.");
            return;
        }

        stateMachine.TransitionStates(PlayerStates.ComboPrim);
    }

    private void OnComboSecondary()
    {
        // If the ability is on cooldown then don't use at this time
        if (stateMachine.ComboAttackSecondaryAbility.OnCooldown)
        {
            if (stateMachine.DebugStateMachine) Debug.Log("Secondary Combo Attack on Cooldown.");
            return;
        }

        stateMachine.TransitionStates(PlayerStates.ComboSec);
    }

    private void OnPoweredPrimary()
    {
        // If the ability is on cooldown then don't use at this time
        if (stateMachine.PowerAttackPrimaryAbility.OnCooldown)
        {
            if (stateMachine.DebugStateMachine) Debug.Log("Primary Power Attack on Cooldown.");
            return;
        }

        stateMachine.TransitionStates(PlayerStates.PowerPrim);
    }

    private void OnPoweredSecondary()
    {
        // If the ability is on cooldown then don't use at this time
        if (stateMachine.PowerAttackSecondaryAbility.OnCooldown)
        {
            if (stateMachine.DebugStateMachine) Debug.Log("Secondary Power Attack on Cooldown.");
            return;
        }

        stateMachine.TransitionStates(PlayerStates.PowerSec);
    }

    private void OnDash()
    {
        stateMachine.TransitionStates(PlayerStates.Dash);
    }

    private void OnInteract1()
    {
        if (stateMachine.CanClimb())
            stateMachine.TransitionStates(PlayerStates.Climb);
    }

    private void OnEmote()
    {
        stateMachine.TransitionStates(PlayerStates.Emote);
    }
}
