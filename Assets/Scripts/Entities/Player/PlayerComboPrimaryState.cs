/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025 (Ryan)
 * Notes:           Player Combo Primary State
*/
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
            Debug.LogWarning("Combo Primary: Ability not assigned.");
            stateMachine.TransitionStates(PlayerStates.Idle);
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
