/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/10/2026
 * Last Modified:   06/10/2026 (Ryan)
 * Notes:           
*/
using System;
using UnityEngine;
using RyansLibrary.Input;

public class Trigger: MonoBehaviour
{
    public event Action OnTriggerActivated;

    private bool canActivate = false;

    private void Awake()
    {
        InputHandler.OnInteract1 += Activate;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
            canActivate = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
            canActivate = false;
    }

    public void Activate()
    {
        if (canActivate)
        {
            OnTriggerActivated?.Invoke();
        }
    }
}
