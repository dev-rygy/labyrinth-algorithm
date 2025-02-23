/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025 (Ryan)
 * Notes:           Entity Health Component 
 *                  - Take Damage
 *                  - Heal
 *                  - Death
*/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityHealth : MonoBehaviour, IDamagable
{
    // Events
    public event Action OnTakeDamage;
    public event Action OnDeath;

    [field: SerializeField] public int MaxHealth { get; protected set; }
    public int Health { get; protected set; }
    public bool Invulnerable { get; protected set; }

    public void Start()
    {
        Health = MaxHealth;
    }

    public virtual void TakeDamage(int damage)         // Change health value with damage passed in; wait for invTime
    {
        if (Health <= 0)
            return;

        if (Invulnerable)
            return;

        Health = Mathf.Max(Health - damage, 0);     // Make sure that health goes to 0 if negative
        OnTakeDamage?.Invoke();

        if (Health <= 0)
            Death();
    }

    public virtual void Heal(int amount)
    {
        Health += amount;
    }

    public virtual void SetInvulnerableTimer(float invTime)         // Called ANY time the Entity becomes invulnerable
    {
        if (invTime > 0 && !Invulnerable)
            StartCoroutine(InvulnerableCo(invTime));
    }

    public virtual void ToggleInvulnerable(bool toggle)         // Called ANY time the Entity becomes invulnerable
    {
        Invulnerable = toggle;
    }

    private IEnumerator InvulnerableCo(float invTime)
    {
        Invulnerable = true;
        yield return new WaitForSeconds(invTime);
        Invulnerable = false;
    }

    protected virtual void Death()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
