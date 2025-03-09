/*
 * Created By:      Ryan Carpenter
 * Date Created:    03/08/2025
 * Last Modified:   03/08/2025 (Ryan)
 * Notes:           Primary Combo Ability for the Broadsword
*/
using System;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    public Action<EntityHealth> OnContact;

    [SerializeField] private bool _debug;

    private void OnTriggerEnter(Collider otherCollider)
    {
        if (_debug) Debug.Log("Collider detected.");

        // Object must have a rigidbody to take damage
        if (otherCollider.attachedRigidbody == null) return;

        // If the object has a Health component -> deal damage
        if (otherCollider.attachedRigidbody.TryGetComponent<EntityHealth>(out EntityHealth health))
        {
            if (_debug) Debug.Log("Entity detected.");

            OnContact?.Invoke(health);
        }
    }
}
