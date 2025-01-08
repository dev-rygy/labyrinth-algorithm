using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable] // Makes the class and all of it's values visable in the inspector
public class Attack : MonoBehaviour
{
    [field: SerializeField] public string AnimationName { get; private set; }   // The animation of the attack
    [field: SerializeField] public float TransitionDuration { get; private set; }   // The amount of time before the animation will transition to another 
    [field: SerializeField] public int ComboStateIndex { get; private set; } = -1;  // The index of the next attack in the attack array
    [field: SerializeField] public float ComboAttackTime { get; private set; }  // When to start playing the next animation
    [field: SerializeField] public float ForceTime { get; private set; }  // When to start playing the next animation
    [field: SerializeField] public float Force { get; private set; }  // When to start playing the next animation
    [field: SerializeField] public int Damage { get; private set; }  // When to start playing the next animation
}
