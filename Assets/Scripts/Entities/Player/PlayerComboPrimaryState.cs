using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComboPrimaryState : PlayerState
{
    public PlayerComboPrimaryState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        if (stateMachine.ComboAttackPrimary == null)
        {
            // TODO: Enter Default Combo
            Debug.Log("Default Combo Entered.");
            return;
        }

        stateMachine.ComboAttackPrimary?.Enter(stateMachine);
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

        stateMachine.ComboAttackPrimary?.Exit(stateMachine);
    }
}
