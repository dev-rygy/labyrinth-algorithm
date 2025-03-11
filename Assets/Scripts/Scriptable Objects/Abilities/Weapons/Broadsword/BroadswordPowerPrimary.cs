/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/17/2025
 * Last Modified:   02/19/2025 (Ryan)
 * Notes:           Primary Combo Ability for the Broadsword
*/
using RyansLibrary.Abilities;
using UnityEngine;

[CreateAssetMenu(fileName = "BroadswordPowerPrimaryAbility", menuName = "Scriptable Objects/Ability/Weapons/Broadsword/BroadswordPowerPrimaryAbility", order = 1)]
public class BroadswordPowerPrimary : Ability
{
    [Header("Attack")]
    [SerializeField] private int attackDamage;
    [SerializeField] private float initialAttackForce;
    [SerializeField] private float attackForceGain;
    [SerializeField] private float maximumPowerTime;

    [Header("Animation")]
    [SerializeField] private string animHash;

    // The longer this variable is true the more power the ability will have
    private float attackForce;
    private bool poweringAbility;
    private float originalAnimSpeed;
    private float timerTime;

    public override void Enter()
    {
        stateMachine = PlayerStateMachine.Instance;

        // Init. Ability Cast
        attackForce = initialAttackForce;
        poweringAbility = true;
        timerTime = 0;

        stateMachine.hitbox.OnContact += DealDamageOnContact;

        stateMachine.AnimationTimestamps.OnAnimationExit += PowerExit;

        // Play the attack's animation
        stateMachine.Animator.CrossFadeInFixedTime(animHash, 0);

        originalAnimSpeed = stateMachine.Animator.speed;    // Store original animation speed
    }

    public override void Tick(float deltaTime)
    {
        // Apply ambient forces
        stateMachine.ApplyAmbientForces(deltaTime);

        // Start animation again and release force when the player is no longer holding down the primary power button
        if (poweringAbility && !stateMachine.Input.IsHoldingPrimaryPower)
            CommenceAttack();

        // Freeze animation and power up attack when holding down primary power button
        if (poweringAbility)
        {
            stateMachine.Animator.speed = 0;        // Stop Animation

            PowerTimer(deltaTime);
        }
    }

    public override void Exit()
    {
        StartCooldown();

        stateMachine.hitbox.OnContact -= DealDamageOnContact;

        stateMachine.AnimationTimestamps.OnAnimationExit -= PowerExit;
    }

    private void PowerTimer(float deltaTime)
    {
        timerTime += deltaTime;
        attackForce += attackForceGain * deltaTime;     // Add to force over time

        if (timerTime >= maximumPowerTime)
            CommenceAttack();
    }

    private void CommenceAttack()
    {
        poweringAbility = false;        // Flag to not stop the animation anymore

        stateMachine.Animator.speed = originalAnimSpeed;        // Restore original animation speed

        stateMachine.ForceReciever.AddForce(stateMachine.PlayerCharacter.transform.forward * attackForce, 0.75f);      // Attack Force
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
