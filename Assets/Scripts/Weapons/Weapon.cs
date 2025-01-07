/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/07/2025
 * Last Modified:   01/07/2025
 * Notes:           Every active ability is required to inherit this
 *                      parent class in order to inherent the interface
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public abstract Ability EquipAbility(AbilityType type);
}
