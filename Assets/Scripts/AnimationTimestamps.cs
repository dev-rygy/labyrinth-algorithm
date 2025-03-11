/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025 (Ryan)
 * Notes:           Used to invoke animation events set in the animator window
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationTimestamps : MonoBehaviour
{
    // Animation Events
    public event System.Action OnAnimationEnter;
    public event System.Action OnAnimationContinue;
    public event System.Action OnAnimationExit;

    // Invoke methods
    public void InvokeComboPrimEnter()
    {
        OnAnimationEnter?.Invoke();
    }

    public void InvokeComboPrimContinue()
    {
        OnAnimationContinue?.Invoke();
    }

    public void InvokeCombroPrimExit()
    {
        OnAnimationExit?.Invoke();
    }
}
