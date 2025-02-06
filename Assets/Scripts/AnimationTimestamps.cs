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
    public event System.Action OnComboPrimColliderEnable;
    public event System.Action OnComboPrimColliderDisable;
    public event System.Action OnComboPrimEnter;
    public event System.Action OnComboPrimContinue;
    public event System.Action OnComboPrimExit;

    // Invoke methods
    public void InvokeComboPrimEnable()
    {
        OnComboPrimColliderEnable?.Invoke();
    }

    public void InvokeComboPrimDisable()
    {
        OnComboPrimColliderDisable?.Invoke();
    }

    public void InvokeComboPrimEnter()
    {
        OnComboPrimEnter?.Invoke();
    }

    public void InvokeComboPrimContinue()
    {
        OnComboPrimContinue?.Invoke();
    }

    public void InvokeCombroPrimExit()
    {
        OnComboPrimExit?.Invoke();
    }
}
