/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/20/2025
 * Last Modified:   02/20/2025 (Ryan)
 * Notes:           Player Dash State
*/
using UnityEngine;

public class PlayerDashState : PlayerState
{
    public PlayerDashState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        if (stateMachine.DashAbility == null)
        {
            Debug.LogWarning("Dash: Ability not assigned.");
            stateMachine.TransitionStates(PlayerStates.Idle);
            return;
        }

        stateMachine.DashAbility?.Enter();
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.DashAbility?.Tick(deltaTime);
    }

    public override void Exit()
    {
        stateMachine.DashAbility?.Exit();
    }
}
