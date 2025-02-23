using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DealDamageOnContact : MonoBehaviour
{
    [SerializeField] private int damage = 10;

    private void OnTriggerEnter(Collider otherCollider)
    {
        // Object must have a rigidbody to take damage
        if (otherCollider.attachedRigidbody == null) return;

        // If the object has a Health component -> deal damage
        if (otherCollider.attachedRigidbody.TryGetComponent<EntityHealth>(out EntityHealth health))
        {
            health.TakeDamage(damage);
        }
    }
}
