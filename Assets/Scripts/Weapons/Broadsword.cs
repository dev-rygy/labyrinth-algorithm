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
        public override void Execute(StateMachine playerController)
        {
            Debug.Log("Broadsword Primary Combo Attack Performed.");
        }
    }

    private class BroadswordPowerAttackPrimary : Ability
    {
        public override void Execute(StateMachine stateMachine)
        {
            Debug.Log("Broadsword Primary Power Attack Performed.");
        }
    }

    private class BroadswordComboAttackSecondary : Ability
    {
        public override void Execute(StateMachine stateMachine)
        {
            Debug.Log("Broadsword Secondary Combo Attack Performed.");
        }
    }

    private class BroadswordPowerAttackSecondary : Ability
    {
        public override void Execute(StateMachine stateMachine)
        {
            Debug.Log("Broadsword Secondary Power Attack Performed.");
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

