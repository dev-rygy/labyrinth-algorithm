using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Scriptable Object that contains the data for a phase shift ability </summary>
[CreateAssetMenu(fileName = "AnimationTimestamp", menuName = "ScriptableObjects/Animation Timestamp")]
public class ScriptableAnimationTimestamp : ScriptableObject
{
    public event Action Event;


}
