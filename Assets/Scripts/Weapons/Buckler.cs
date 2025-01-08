using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buckler : Weapon
{
    private class BucklerComboAttackSecondary : Ability
    {
        private string animationName = "Shield Block";

        public override void Enter(PlayerStateMachine stateMachine)
        {
            // Play the attack's animation
            stateMachine.Animator.CrossFadeInFixedTime(animationName, 0.1f);
        }

        public override void Tick(float deltaTime, PlayerStateMachine stateMachine)
        {
            if (!stateMachine.Input.IsHoldingSecondaryCombo)
            {
                stateMachine.SwitchState(new PlayerIdleState(stateMachine));
                return;
            }
        }

        public override void Exit(PlayerStateMachine stateMachine)
        {

        }
    }

    private class BucklerPowerAttackSecondary : Ability
    {
        public override void Enter(PlayerStateMachine stateMachine)
        {
            throw new System.NotImplementedException();
        }

        public override void Tick(float deltaTime, PlayerStateMachine stateMachine)
        {
            throw new System.NotImplementedException();
        }

        public override void Exit(PlayerStateMachine stateMachine)
        {
            throw new System.NotImplementedException();
        }
    }

    /// <summary>
    /// Equip an ability from the weapon. If the ability does not exist then return null to
    /// tell the system no ability exists.
    /// </summary>
    /// <param name="type">The ability key/category</param>
    /// <returns>A new instance of the ability</returns>
    public override Ability GetAbility(AbilityType type)
    {
        switch (type)
        {
            case AbilityType.ComboAttackSecondary:
                return new BucklerComboAttackSecondary();
            case AbilityType.PowerAttackSecondary:
                return new BucklerPowerAttackSecondary();
            default:
                return null;
        }
    }
}
