/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/10/2026
 * Last Modified:   06/10/2026 (Ryan)
 * Notes:           
*/
using System;
using UnityEngine;
using RyansLibrary.Input;
using RyansLibrary.Labyrinth;

public class Trigger: MonoBehaviour
{
    public event Action OnTriggerActivated;

    private bool canActivate = false;

    private void Awake()
    {
        InputHandler.OnInteract1 += Activate;

        MapGeneratorController.OnGenerationReset += DestroyTrigger;
    }

    private void OnDestroy()
    {
        InputHandler.OnInteract1 -= Activate;

        MapGeneratorController.OnGenerationReset -= DestroyTrigger;
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

    public void DestroyTrigger()
    {
        Destroy(gameObject);
    }
}
