/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025 (Ryan)
 * Notes:           Entity Health Component 
 *                  - Take Damage
 *                  - Heal
 *                  - Death
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityHealth : MonoBehaviour, IDamagable
{
    [field: SerializeField] public int MaxHealth { get; protected set; }
    public int Health { get; protected set; }
    public bool Invulnerable { get; protected set; }

    public virtual void TakeDamage(int damage)         // Change health value with damage passed in; wait for invTime
    {
        if (Invulnerable)     // For debugging purposes only
            return;

        Health -= damage;

        if (Health <= 0)
            Death();
    }

    public virtual void Heal(int amount)
    {
        Health += amount;
    }

    public virtual void MakeInvulnerable(float invTime)         // Called ANY time the Entity becomes invulnerable
    {
        if (invTime > 0 && !Invulnerable)
        {
            StartCoroutine(InvulnerableCo(invTime));
        }
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
        Destroy(gameObject);
    }
}
