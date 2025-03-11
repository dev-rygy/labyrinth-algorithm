/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/17/2025
 * Last Modified:   02/19/2025 (Ryan)
 * Notes:           Primary Combo Ability for the Broadsword
*/
using RyansLibrary.Abilities;
using RyansLibrary.Input;
using UnityEngine;

[CreateAssetMenu(fileName = "BroadswordComboPrimaryAbility", menuName = "Scriptable Objects/Ability/Weapons/Broadsword/BroadswordComboPrimaryAbility", order = 0)]
public class BroadswordComboPrimary : Ability
{
    [Header("Attack")]
    [SerializeField] private int attackDamage;
    [SerializeField] private float attackForce;

    [Header("Animation")]
    [SerializeField] private string animHash;

    private bool _hasPressedAttack;
    private bool _canContinue;

    public override void Enter()
    {
        stateMachine = PlayerStateMachine.Instance;

        // Input Events
        InputHandler.OnComboPrimary += OnComboPrimary;

        // Subscribe to player's Hitbox to recieve notifications of when an enemy's hit
        stateMachine.hitbox.OnContact += DealDamageOnContact;

        // When the combo is finished or if the player does not press the attack button again then exit the state
        stateMachine.AnimationTimestamps.OnAnimationEnter += ComboEnter;
        stateMachine.AnimationTimestamps.OnAnimationContinue += ComboContinue;
        stateMachine.AnimationTimestamps.OnAnimationExit += ComboExit;

        // Play the attack's animation
        stateMachine.Animator.CrossFadeInFixedTime(animHash, 0.1f);
    }

    public override void Tick(float deltaTime)
    {
        // Apply ambient forces
        stateMachine.ApplyAmbientForces(deltaTime);
    }

    public override void Exit()
    {
        StartCooldown();

        // Input Events
        InputHandler.OnComboPrimary -= OnComboPrimary;

        // Unubscribe to player's Hitbox so other abilities can use it
        stateMachine.hitbox.OnContact -= DealDamageOnContact;

        // When the combo is finished or if the player does not press the attack button again then exit the state
        stateMachine.AnimationTimestamps.OnAnimationEnter -= ComboEnter;
        stateMachine.AnimationTimestamps.OnAnimationContinue -= ComboContinue;
        stateMachine.AnimationTimestamps.OnAnimationExit -= ComboExit;
    }

    // The player presses the primary combo key
    private void OnComboPrimary()
    {
        if (_canContinue)
            _hasPressedAttack = true;
    }

    private void ComboEnter()
    {
        _canContinue = false;
        _hasPressedAttack = false;

        stateMachine.ForceReciever.AddForce(stateMachine.PlayerCharacter.transform.forward * attackForce);
    }

    private void ComboContinue()
    {
        _canContinue = true;
    }

    private void ComboExit()
    {
        if (!_hasPressedAttack)
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
