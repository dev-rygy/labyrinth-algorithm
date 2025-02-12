/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/07/2025
 * Last Modified:   02/11/2025
 * Notes:           Every active ability is required to inherit this
 *                      parent class in order to inherent the interface
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    Primary,            // Primary weapons can be equipped in the primary weapon slot only
    Secondary,          // Secondary weapons can be equipped in the secondary weapon slot only
    Hybrid              // Hybrid weapons can be equipped in both primary and secondary slots
}

public abstract class Weapon : MonoBehaviour
{
    [field: SerializeField] public WeaponType Type { get; private set; }
    [field: SerializeField] public GameObject WeaponColliderObject { get; private set; }

    public abstract Ability GetAbility(AbilityType type, PlayerStateMachine stateMachine);

    public void EnableWeaponCollider()
    {
        if (WeaponColliderObject == null)
            return;

        WeaponColliderObject.SetActive(true);
    }

    public void DisableWeaponCollider()
    {
        if (WeaponColliderObject == null)
            return;

        WeaponColliderObject.SetActive(false);
    }
}
