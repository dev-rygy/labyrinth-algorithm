/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/12/2025
 * Last Modified:   02/12/2025 (Ryan)
 * Notes:           
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityData", menuName = "Scriptable Objects/AbilityData", order = 3)]
public class AbilityData : ScriptableObject
{
    [field: SerializeField] public AbilityType Type { get; private set; }
    [field: SerializeField] public string AbilityID { get; private set; }
    [field: SerializeField] public float AbilityKnockback { get; private set; }
    [field: SerializeField] public float BaseCooldown { get; private set; }
    [field: SerializeField] public string Animation { get; private set; }
}
