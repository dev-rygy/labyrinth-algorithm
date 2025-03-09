/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/17/2025
 * Last Modified:   02/19/2025 (Ryan)
 * Notes:           Primary Combo Ability for the Broadsword
*/
using RyansLibrary.Abilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BroadswordPowerPrimaryAbility", menuName = "Scriptable Objects/Ability/Weapons/Broadsword/BroadswordPowerPrimaryAbility", order = 1)]
public class BroadswordPowerPrimary : Ability
{
    [Header("Attack")]
    [SerializeField] private int attackDamage;
    [SerializeField] private float attackForce;

    [Header("Animation")]
    [SerializeField] private string animHash;

    public override void Enter()
    {
        stateMachine = PlayerStateMachine.Instance;

        stateMachine.hitbox.OnContact += DealDamageOnContact;

        stateMachine.AnimationTimestamps.OnComboPrimExit += PowerExit;

        stateMachine.AnimationTimestamps.OnComboPrimColliderEnable += stateMachine.PrimaryWeapon.EnableWeaponCollider;
        stateMachine.AnimationTimestamps.OnComboPrimColliderDisable += stateMachine.PrimaryWeapon.DisableWeaponCollider;

        stateMachine.ForceReciever.AddForce(stateMachine.PlayerCharacter.transform.forward * attackForce, 0.75f);

        // Play the attack's animation
        stateMachine.Animator.CrossFadeInFixedTime(animHash, 0.3f);
    }

    public override void Tick(float deltaTime)
    {
        // Apply ambient forces
        stateMachine.ApplyAmbientForces(deltaTime);
    }

    public override void Exit()
    {
        StartCooldown();

        stateMachine.hitbox.OnContact -= DealDamageOnContact;

        stateMachine.AnimationTimestamps.OnComboPrimExit -= PowerExit;

        stateMachine.AnimationTimestamps.OnComboPrimColliderEnable -= stateMachine.PrimaryWeapon.EnableWeaponCollider;
        stateMachine.AnimationTimestamps.OnComboPrimColliderDisable -= stateMachine.PrimaryWeapon.DisableWeaponCollider;
    }

    private void PowerExit()
    {
        stateMachine.TransitionStates(PlayerStates.Idle);
    }

    /// <summary>
    /// Deal damage to an entity that contacts with this attack's hitbox
    /// </summary>
    /// <param name="health">The entity's health</param>
    private void DealDamageOnContact(EntityHealth health)
    {
        health.TakeDamage(attackDamage);
    }
}
