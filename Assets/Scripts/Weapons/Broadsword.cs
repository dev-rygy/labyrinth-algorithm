/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/07/2025
 * Last Modified:   01/07/2025
 * Notes:           Broadsword Weapon stats and abilities
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RyansLibrary.StateMachine;
using RyansLibrary.Input;

public class Broadsword : Weapon
{
    private class BroadswordComboAttackPrimary : Ability
    {
        private PlayerStateMachine stateMachine;

        private string animationName = "Sword Combo Primary";

        private bool _hasPressedAttack;
        private bool _canContinue;

        public override void Enter(PlayerStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;

            // Input Events
            InputHandler.OnComboPrimary += OnComboPrimary;

            // When the combo is finished or if the player does not press the attack button again then exit the state
            this.stateMachine.AnimationTimestamps.OnComboPrimEnter += ComboEnter;
            this.stateMachine.AnimationTimestamps.OnComboPrimContinue += ComboContinue;
            this.stateMachine.AnimationTimestamps.OnComboPrimExit += ComboExit;

            this.stateMachine.AnimationTimestamps.OnComboPrimColliderEnable += this.stateMachine.PrimaryWeapon.EnableWeaponCollider;
            this.stateMachine.AnimationTimestamps.OnComboPrimColliderDisable += this.stateMachine.PrimaryWeapon.DisableWeaponCollider;
            Debug.Log("Broadsword Primary Combo Entered.");


            // Play the attack's animation
            this.stateMachine.Animator.CrossFadeInFixedTime(animationName, 0.3f);
        }

        public override void Tick(float deltaTime, PlayerStateMachine stateMachine) 
        {
            Debug.Log("Broadsword Primary Combo Ticked.");
        }

        public override void Exit(PlayerStateMachine stateMachine)
        {
            // Input Events
            InputHandler.OnComboPrimary -= OnComboPrimary;

            // When the combo is finished or if the player does not press the attack button again then exit the state
            stateMachine.AnimationTimestamps.OnComboPrimEnter -= ComboEnter;
            stateMachine.AnimationTimestamps.OnComboPrimContinue -= ComboContinue;
            stateMachine.AnimationTimestamps.OnComboPrimExit -= ComboExit;

            stateMachine.AnimationTimestamps.OnComboPrimColliderEnable -= stateMachine.PrimaryWeapon.EnableWeaponCollider;
            stateMachine.AnimationTimestamps.OnComboPrimColliderDisable -= stateMachine.PrimaryWeapon.DisableWeaponCollider;
            Debug.Log("Broadsword Primary Combo Exited.");
        }

        // The player presses the primary combo key
        private void OnComboPrimary()
        {
            if (_canContinue)
                _hasPressedAttack = true;
        }

        private void ComboEnter()
        {
            _canContinue = false;
            _hasPressedAttack = false;
        }

        private void ComboContinue()
        {
            _canContinue = true;
        }

        private void ComboExit()
        {
            if (!_hasPressedAttack)
                stateMachine.SwitchState(new PlayerIdleState(stateMachine));
        }
    }

    private class BroadswordPowerAttackPrimary : Ability
    {
        public override void Enter(PlayerStateMachine stateMachine)
        {
            throw new System.NotImplementedException();
        }

        public override void Exit(PlayerStateMachine stateMachine)
        {
            throw new System.NotImplementedException();
        }

        public override void Tick(float deltaTime, PlayerStateMachine stateMachine)
        {
            throw new System.NotImplementedException();
        }
    }

    private class BroadswordComboAttackSecondary : Ability
    {
        public override void Enter(PlayerStateMachine stateMachine)
        {
            throw new System.NotImplementedException();
        }

        public override void Exit(PlayerStateMachine stateMachine)
        {
            throw new System.NotImplementedException();
        }

        public override void Tick(float deltaTime, PlayerStateMachine stateMachine)
        {
            throw new System.NotImplementedException();
        }
    }

    private class BroadswordPowerAttackSecondary : Ability
    {
        public override void Enter(PlayerStateMachine stateMachine)
        {
            throw new System.NotImplementedException();
        }

        public override void Exit(PlayerStateMachine stateMachine)
        {
            throw new System.NotImplementedException();
        }

        public override void Tick(float deltaTime, PlayerStateMachine stateMachine)
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
            case AbilityType.ComboAttackPrimary:
                return new BroadswordComboAttackPrimary();
            case AbilityType.ComboAttackSecondary:
                return new BroadswordComboAttackSecondary();
            case AbilityType.PowerAttackPrimary:
                return new BroadswordPowerAttackPrimary();
            case AbilityType.PowerAttackSecondary:
                return new BroadswordPowerAttackSecondary();
            default:
                return null;
        }
    }
}

