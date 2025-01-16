using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPowerPrimaryState : PlayerState
{
    public PlayerPowerPrimaryState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        if (stateMachine.PowerAttackPrimary == null)
        {
            Debug.Log("Power Primary: Ability not assigned.");
            stateMachine.SwitchState(new PlayerIdleState(stateMachine));
            return;
        }

        stateMachine.PowerAttackPrimary?.Enter();
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.PowerAttackPrimary?.Tick(deltaTime);
    }

    public override void Exit()
    {
        stateMachine.PowerAttackPrimary?.Exit();
    }
}
