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
            // TODO: Enter Default Combo
            Debug.Log("Default Combo Entered.");
            return;
        }

        stateMachine.ComboAttackSecondary?.Enter(stateMachine);
    }

    public override void Tick(float deltaTime)
    {
        ApplyGravity(deltaTime);

        if (stateMachine.ComboAttackSecondary == null)
        {
            // TODO: Tick Default Combo
            stateMachine.SwitchState(new PlayerIdleState(stateMachine));
            Debug.Log("Default Combo Ticked.");
            return;
        }

        stateMachine.ComboAttackSecondary?.Tick(deltaTime, stateMachine);
    }

    public override void Exit()
    {
        if (stateMachine.ComboAttackSecondary == null)
        {
            // TODO: Exit Default Combo
            Debug.Log("Default Combo Exited.");
            return;
        }

        stateMachine.ComboAttackSecondary?.Exit(stateMachine);
    }
}
