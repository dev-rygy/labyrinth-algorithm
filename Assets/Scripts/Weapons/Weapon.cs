/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/07/2025
 * Last Modified:   02/11/2025 (Ryan)
 * Notes:           Every active ability is required to inherit this
 *                      parent class in order to inherent the interface
*/
using RyansLibrary.Abilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    Primary,            // Primary weapons can be equipped in the primary weapon slot only
    Secondary,          // Secondary weapons can be equipped in the secondary weapon slot only
    Hybrid              // Hybrid weapons can be equipped in both primary and secondary slots
}

public class Weapon : MonoBehaviour
{
    [field: SerializeField] public WeaponType Type { get; private set; }
    [field: SerializeField] public GameObject WeaponHitbox { get; private set; }

    [field: SerializeField] protected Ability primaryComboAbility;
    [field: SerializeField] protected Ability primaryPowerAbility;
    [field: SerializeField] protected Ability secondaryComboAbility;
    [field: SerializeField] protected Ability secondaryPowerAbility;
    [field: SerializeField] protected Ability dashAbility;

    /// <summary>
    /// Equip an ability from the weapon. If the ability does not exist then return null to
    /// tell the system no ability exists.
    /// </summary>
    /// <param name="type">The ability key/category</param>
    /// <returns>A new instance of the ability</returns>
    public Ability GetAbility(AbilityType type)
    {
        switch (type)
        {
            case AbilityType.ComboAttackPrimary:
                return primaryComboAbility;
            case AbilityType.PowerAttackPrimary:
                return primaryPowerAbility;
            case AbilityType.ComboAttackSecondary:
                return secondaryComboAbility;
            case AbilityType.PowerAttackSecondary:
                return secondaryPowerAbility;
            case AbilityType.Dash:
                return dashAbility;
            default:
                return null;
        }
    }

    public void EnableWeaponCollider()
    {
        if (WeaponHitbox == null)
            return;

        WeaponHitbox.SetActive(true);
    }

    public void DisableWeaponCollider()
    {
        if (WeaponHitbox == null)
            return;

        WeaponHitbox.SetActive(false);
    }
}
