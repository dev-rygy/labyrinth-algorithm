/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   02/11/2025 (Ryan)
 * Notes:           Player Idle State
*/
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
    private readonly int k_animIdleBlendTreeHash = Animator.StringToHash("Idle Blend Tree");
    private readonly int k_animIdleSpeedHash = Animator.StringToHash("IdleSpeed");

    // Blend tree damp time
    private const float k_animatorDampTime = 0.1f;
    // Time to transition between animations
    private const float k_animatorCrossfadeDuration = 0.1f;

    // Constuctor
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Input Events
        // ABILITIES DISABLED FOR DEMO
        //InputHandler.OnComboPrimary += OnComboPrimary;          // Input: right_shoulder 
        //InputHandler.OnComboSecondary += OnComboSecondary;      // Input: left_shoulder
        //InputHandler.OnPowerPrimary += OnPoweredPrimary;        // Input: right_trigger
        //InputHandler.OnPowerSecondary += OnPoweredSecondary;    // Input: left_trigger
        //InputHandler.OnDash += OnDash;                          // Input: dash
        InputHandler.OnInteract1 += OnInteract1;                // Input: interact
        InputHandler.OnEmote += OnEmote;                        // Input: emote

        // Play the Running blend tree animations
        stateMachine.Animator.CrossFadeInFixedTime(k_animIdleBlendTreeHash, k_animatorCrossfadeDuration);

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
        float playerActualSpeed = 0f;

        if (stateMachine.AutoMoveDirection != AutoMoveDirections.None)
        {
            Vector3 autoMoveInput = AutoMove(stateMachine.AutoMoveDirection, deltaTime);
            playerActualSpeed = autoMoveInput.magnitude;
        }
        else
        {
            stateMachine.Move(moveInput * stateMachine.MovementSpeed, deltaTime);
            // The value that transitions the player to and from the idle/walking/running animations
            playerActualSpeed = moveInput.magnitude;
        }

        // If the player has movement then play running animation 
        stateMachine.Animator.SetFloat(k_animIdleSpeedHash, playerActualSpeed, k_animatorDampTime, deltaTime);    // Run Animation

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
        // ABILITIES DISABLED FOR DEMO
        //InputHandler.OnComboPrimary -= OnComboPrimary;
        //InputHandler.OnComboSecondary -= OnComboSecondary;
        //InputHandler.OnPowerPrimary -= OnPoweredPrimary;
        //InputHandler.OnPowerSecondary -= OnPoweredSecondary;
        //InputHandler.OnDash -= OnDash;
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
        // If the ability is on cooldown then don't use at this time
        if (stateMachine.DashAbility.OnCooldown)
        {
            if (stateMachine.DebugStateMachine) Debug.Log("Dash Ability on Cooldown.");
            return;
        }

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

    private Vector3 AutoMove(AutoMoveDirections direction, float deltaTime)
    {
        Vector3 Direction = Vector3.zero;

        switch (direction)
        {
            case AutoMoveDirections.None:
                Direction = Vector3.zero;
                stateMachine.Move(Direction, deltaTime);
                break;
            case AutoMoveDirections.Forward:
                Direction = Vector3.forward;
                stateMachine.Move(Direction * stateMachine.MovementSpeed, deltaTime);
                break;
            case AutoMoveDirections.Backward:
                Direction = Vector3.back;
                stateMachine.Move(Direction * stateMachine.MovementSpeed, deltaTime);
                break;
            case AutoMoveDirections.Left:
                Direction = Vector3.left;
                stateMachine.Move(Direction * stateMachine.MovementSpeed, deltaTime);
                break;
            case AutoMoveDirections.Right:
                Direction = Vector3.right;
                stateMachine.Move(Direction * stateMachine.MovementSpeed, deltaTime);
                break;
            default:
                if (stateMachine.DebugStateMachine) Debug.Log("Auto Move Direction Not Found.");
                break;
        }

        return Direction;
    }
}
