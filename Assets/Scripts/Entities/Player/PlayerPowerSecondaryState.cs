/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025 (Ryan)
 * Notes:           Player Power Secondary State
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPowerSecondaryState : PlayerState
{
    public PlayerPowerSecondaryState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        if (stateMachine.PowerAttackSecondaryAbility == null)
        {
            Debug.LogWarning("Power Secondary: Ability not Assigned.");
            stateMachine.TransitionStates(PlayerStates.Idle);
            return;
        }

        stateMachine.PowerAttackSecondaryAbility?.Enter();
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.PowerAttackSecondaryAbility?.Tick(deltaTime);
    }

    public override void Exit()
    {
        stateMachine.PowerAttackSecondaryAbility?.Exit();
    }
}
