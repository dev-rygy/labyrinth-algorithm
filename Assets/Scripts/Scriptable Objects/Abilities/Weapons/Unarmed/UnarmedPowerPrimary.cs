/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/17/2025
 * Last Modified:   02/19/2025 (Ryan)
 * Notes:           Unarmed Power Primary Ability for Unarmed
*/
using RyansLibrary.Abilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnarmedPowerPrimaryAbility", menuName = "Scriptable Objects/Ability/Weapons/Unarmed/UnarmedPowerPrimaryAbility", order = 1)]
public class UnarmedPowerPrimary : Ability
{
    [SerializeField] private string animHash;
    [SerializeField] private float attackForce;

    public override void Enter()
    {
        stateMachine = PlayerStateMachine.Instance;

        stateMachine.AnimationTimestamps.OnComboPrimExit += PowerExit;

        stateMachine.AnimationTimestamps.OnComboPrimColliderEnable += stateMachine.PrimaryWeapon.EnableWeaponCollider;
        stateMachine.AnimationTimestamps.OnComboPrimColliderDisable += stateMachine.PrimaryWeapon.DisableWeaponCollider;

        stateMachine.ForceReciever.AddForce(stateMachine.PlayerCharacter.transform.forward * attackForce, 0.75f);

        // Play the attack's animation
        stateMachine.Animator.CrossFadeInFixedTime(animHash, 0.3f);
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.Move(Vector3.zero, deltaTime);
    }

    public override void Exit()
    {
        StartCooldown();

        stateMachine.AnimationTimestamps.OnComboPrimExit -= PowerExit;

        stateMachine.AnimationTimestamps.OnComboPrimColliderEnable -= stateMachine.PrimaryWeapon.EnableWeaponCollider;
        stateMachine.AnimationTimestamps.OnComboPrimColliderDisable -= stateMachine.PrimaryWeapon.DisableWeaponCollider;
    }

    private void PowerExit()
    {
        stateMachine.TransitionStates(PlayerStates.Idle);
    }
}
