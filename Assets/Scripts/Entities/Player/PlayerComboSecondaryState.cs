using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComboSecondaryState : PlayerState
{
    public PlayerComboSecondaryState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        if (stateMachine.ComboAttackSecondary == null)
        {
            Debug.Log("Combo Secondary: Ability not assigned.");
            stateMachine.SwitchState(new PlayerIdleState(stateMachine));
            return;
        }

        stateMachine.ComboAttackSecondary?.Enter();
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.ComboAttackSecondary?.Tick(deltaTime);
    }

    public override void Exit()
    {
        stateMachine.ComboAttackSecondary?.Exit();
    }
}
