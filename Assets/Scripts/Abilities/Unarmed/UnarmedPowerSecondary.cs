using RyansLibrary.Abilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnarmedPowerSecondaryAbility", menuName = "Scriptable Objects/Ability/Weapons/Unarmed/UnarmedPowerSecondaryAbility", order = 3)]
public class UnarmedPowerSecondary : Ability
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
