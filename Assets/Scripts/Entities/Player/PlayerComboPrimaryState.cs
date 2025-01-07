using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComboPrimaryState : PlayerState
{
    public PlayerComboPrimaryState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Entered PlayerComboPrimaryState");

        if (stateMachine.ComboAttackPrimary == null)
        {
            Debug.Log("Default Primary Combo Attack Performed.");
        }

        stateMachine.ComboAttackPrimary?.Execute(stateMachine);

        stateMachine.SwitchState(new PlayerIdleState(stateMachine));
    }

    public override void Tick(float deltaTime)
    {
        ApplyGravity(deltaTime);
    }

    public override void Exit()
    {
        Debug.Log("Exited PlayerComboPrimaryState");
    }
}
