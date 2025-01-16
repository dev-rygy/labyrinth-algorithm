using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationTimestamps : MonoBehaviour
{
    public event System.Action OnComboPrimColliderEnable;
    public event System.Action OnComboPrimColliderDisable;
    public event System.Action OnComboPrimEnter;
    public event System.Action OnComboPrimContinue;
    public event System.Action OnComboPrimExit;

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
