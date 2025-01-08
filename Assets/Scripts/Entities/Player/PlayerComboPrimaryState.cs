using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComboPrimaryState : PlayerState
{
    private bool _hasPressedAttack;
    private bool _canContinue;

    public PlayerComboPrimaryState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        _canContinue = false;
        _hasPressedAttack = false;

        // Input Events
        InputHandler.OnComboPrimary += OnComboPrimary;

        // When the combo is finished or if the player does not press the attack button again then exit the state
        stateMachine.AnimationTimestamps.OnComboPrimEnter += ComboEnter;
        stateMachine.AnimationTimestamps.OnComboPrimContinue += ComboContinue;
        stateMachine.AnimationTimestamps.OnComboPrimExit += ComboExit;

        if (stateMachine.ComboAttackPrimary == null)
        {
            // TODO: Enter Default Combo
            Debug.Log("Default Combo Entered.");
            return;
        }

        stateMachine.ComboAttackPrimary?.Enter(stateMachine);

        // Play the attack's animation
        stateMachine.Animator.CrossFadeInFixedTime(stateMachine.ComboAttackPrimary.AnimationName, stateMachine.ComboAttackPrimary.TransitionDuration);
    }

    public override void Tick(float deltaTime)
    {
        ApplyGravity(deltaTime);

        if (stateMachine.ComboAttackPrimary == null)
        {
            // TODO: Tick Default Combo
            stateMachine.SwitchState(new PlayerIdleState(stateMachine));
            Debug.Log("Default Combo Ticked.");
            return;
        }

        stateMachine.ComboAttackPrimary?.Tick(deltaTime, stateMachine);
    }

    public override void Exit()
    {
        if (stateMachine.ComboAttackPrimary == null)
        {
            // TODO: Exit Default Combo
            Debug.Log("Default Combo Exited.");
            return;
        }

        // Input Events
        InputHandler.OnComboPrimary -= OnComboPrimary;

        // When the combo is finished or if the player does not press the attack button again then exit the state
        stateMachine.AnimationTimestamps.OnComboPrimEnter -= ComboEnter;
        stateMachine.AnimationTimestamps.OnComboPrimContinue -= ComboContinue;
        stateMachine.AnimationTimestamps.OnComboPrimExit -= ComboExit;

        stateMachine.ComboAttackPrimary?.Exit(stateMachine);
    }

    // The player presses the primary combo key
    private void OnComboPrimary()
    {
        if (_canContinue)
            _hasPressedAttack = true;
    }

    private void ComboEnter()
    {
        _canContinue = false;
        _hasPressedAttack = false;
    }

    private void ComboContinue()
    {
        _canContinue = true;
    }

    private void ComboExit()
    {
        if (!_hasPressedAttack)
            stateMachine.SwitchState(new PlayerIdleState(stateMachine));
    }
}
