using RyansLibrary.Abilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BroadswordPowerPrimaryAbility", menuName = "Scriptable Objects/Ability/Weapons/Broadsword/BroadswordPowerPrimaryAbility", order = 1)]
public class BroadswordPowerPrimary : Ability
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
        // Apply ambient forces
        stateMachine.ApplyAmbientForces(deltaTime);
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
