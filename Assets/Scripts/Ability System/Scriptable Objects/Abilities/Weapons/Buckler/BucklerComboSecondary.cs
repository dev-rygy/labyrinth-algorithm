/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/17/2025
 * Last Modified:   02/19/2025 (Ryan)
 * Notes:           Secondary Combo Ability for the Buckler
*/
using RyansLibrary.Abilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuckerComboSecondaryAbility", menuName = "Scriptable Objects/Ability/Weapons/Bucker/BuckerComboSecondaryAbility", order = 0)]
public class BucklerComboSecondary : Ability<PlayerStateMachine>
{
    [SerializeField] private string animHash;

    public override void Enter()
    {
        stateMachine = PlayerStateMachine.Instance;

        // Play the attack's animation
        stateMachine.Animator.CrossFadeInFixedTime(animHash, 0.1f);
    }

    public override void Tick(float deltaTime)
    {
        // Apply ambient forces
        stateMachine.ApplyAmbientForces(deltaTime);

        // While holding the primary combo button down stay in blocking state
        if (!stateMachine.Input.IsHoldingSecondaryCombo)
        {
            stateMachine.TransitionStates(PlayerStates.Idle);
            return;
        }
    }

    public override void Exit() 
    {
        StartCooldown();
    }
}
