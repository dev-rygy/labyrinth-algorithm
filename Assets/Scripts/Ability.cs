/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/07/2025
 * Last Modified:   02/11/2025 (Ryan)
 * Notes:           Every active ability is required to inherit this
 *                      parent class in order to inherent the interface
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RyansLibrary.StateMachine;
using System;

public enum AbilityType
{
    None,
    Passive,
    ComboAttackPrimary,
    ComboAttackSecondary,
    PowerAttackPrimary,
    PowerAttackSecondary,
    Dash,
    DashAttack,
    ChargeAttack,
    CastAttack
}

public abstract class Ability : IAbility
{
    // Event for UI
    public event Action<float, float> OnAbilityCooldown;

    protected PlayerStateMachine stateMachine;

    public bool OnCooldown { get; private set; } = false;

    public Ability(PlayerStateMachine stateMachine) 
    {
        this.stateMachine = stateMachine;
    }

    public abstract void Enter();

    public abstract void Tick(float deltaTime);

    public abstract void Exit();

    /// <summary>
    /// Start the ability's cooldown. The player may not use the ability again until the
    /// cooldown time is exhausted.
    /// </summary>
    /// <param name="cooldownTime">Cooldown time</param>
    protected void StartCooldown(float cooldownTime)
    {
        if (OnCooldown)       // If the ability is already on cooldown then return
            return;

        stateMachine.StartCoroutine(CooldownCo(cooldownTime));
    }

    private IEnumerator CooldownCo(float cooldownTime)
    {
        OnCooldown = true;
        float timeLeft = cooldownTime;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            OnAbilityCooldown?.Invoke(timeLeft, cooldownTime);

            yield return null;
        }

        OnAbilityCooldown?.Invoke(0, cooldownTime);       // Invoke one last time at 0 to ensure 0 was reached

        OnCooldown = false;
    }
}
