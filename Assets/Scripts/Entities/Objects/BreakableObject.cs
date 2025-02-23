using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [SerializeField] private bool debug;

    private EntityHealth _health;

    private void OnEnable()
    {
        if (TryGetComponent<EntityHealth>(out EntityHealth health))
            _health = health;
        else
        {
            Debug.LogError("Breakable Object Error: This script requires a Entity Health Component.");
            return;
        }

        _health.OnTakeDamage += Damage;
        _health.OnDeath += Break;
    }

    private void OnDisable()
    {
        _health.OnTakeDamage -= Damage;
        _health.OnDeath -= Break;
    }

    private void Damage()
    {
        if (debug) Debug.Log(gameObject.name + " took damage!");
    }

    private void Break()
    {
        if (debug) Debug.Log(gameObject.name + " was broken!");

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _health.OnTakeDamage -= Damage;
        _health.OnDeath -= Break;
    }
}
