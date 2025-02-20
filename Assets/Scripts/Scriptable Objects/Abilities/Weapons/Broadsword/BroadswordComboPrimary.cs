/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/17/2025
 * Last Modified:   02/19/2025 (Ryan)
 * Notes:           Primary Combo Ability for the Broadsword
*/
using RyansLibrary.Abilities;
using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BroadswordComboPrimaryAbility", menuName = "Scriptable Objects/Ability/Weapons/Broadsword/BroadswordComboPrimaryAbility", order = 0)]
public class BroadswordComboPrimary : Ability
{
    [SerializeField] private string animHash;
    [SerializeField] private float attackForce;

    private bool _hasPressedAttack;
    private bool _canContinue;

    public override void Enter()
    {
        stateMachine = PlayerStateMachine.Instance;

        // Input Events
        InputHandler.OnComboPrimary += OnComboPrimary;

        // When the combo is finished or if the player does not press the attack button again then exit the state
        stateMachine.AnimationTimestamps.OnComboPrimEnter += ComboEnter;
        stateMachine.AnimationTimestamps.OnComboPrimContinue += ComboContinue;
        stateMachine.AnimationTimestamps.OnComboPrimExit += ComboExit;

        stateMachine.AnimationTimestamps.OnComboPrimColliderEnable += stateMachine.PrimaryWeapon.EnableWeaponCollider;
        stateMachine.AnimationTimestamps.OnComboPrimColliderDisable += stateMachine.PrimaryWeapon.DisableWeaponCollider;

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

        // Input Events
        InputHandler.OnComboPrimary -= OnComboPrimary;

        // When the combo is finished or if the player does not press the attack button again then exit the state
        stateMachine.AnimationTimestamps.OnComboPrimEnter -= ComboEnter;
        stateMachine.AnimationTimestamps.OnComboPrimContinue -= ComboContinue;
        stateMachine.AnimationTimestamps.OnComboPrimExit -= ComboExit;

        stateMachine.AnimationTimestamps.OnComboPrimColliderEnable -= stateMachine.PrimaryWeapon.EnableWeaponCollider;
        stateMachine.AnimationTimestamps.OnComboPrimColliderDisable -= stateMachine.PrimaryWeapon.DisableWeaponCollider;
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
}
