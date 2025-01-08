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

public class Broadsword : Weapon
{
    private class BroadswordComboAttackPrimary : Ability
    {
        public override void Enter(PlayerStateMachine stateMachine)
        {
            AnimationName = "Sword Combo Primary";

            stateMachine.AnimationTimestamps.OnComboPrimColliderEnable += stateMachine.PrimaryWeapon.EnableWeaponCollider;
            stateMachine.AnimationTimestamps.OnComboPrimColliderDisable += stateMachine.PrimaryWeapon.DisableWeaponCollider;
            Debug.Log("Broadsword Primary Combo Entered.");
        }

        public override void Tick(float deltaTime, PlayerStateMachine stateMachine) 
        {
            Debug.Log("Broadsword Primary Combo Ticked.");
        }

        public override void Exit(PlayerStateMachine stateMachine)
        {
            stateMachine.AnimationTimestamps.OnComboPrimColliderEnable -= stateMachine.PrimaryWeapon.EnableWeaponCollider;
            stateMachine.AnimationTimestamps.OnComboPrimColliderDisable -= stateMachine.PrimaryWeapon.DisableWeaponCollider;
            Debug.Log("Broadsword Primary Combo Exited.");
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

