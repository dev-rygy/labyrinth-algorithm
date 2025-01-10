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
            Debug.Log("Combo Primary: Ability not assigned.");
            stateMachine.SwitchState(new PlayerIdleState(stateMachine));
            return;
        }

        stateMachine.ComboAttackPrimary?.Enter();
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.ComboAttackPrimary?.Tick(deltaTime);
    }

    public override void Exit()
    {
        stateMachine.ComboAttackPrimary?.Exit();
    }
}
