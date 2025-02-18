using RyansLibrary.Abilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnarmedComboSecondaryAbility", menuName = "Scriptable Objects/Ability/Weapons/Unarmed/UnarmedComboSecondaryAbility", order = 2)]
public class UnarmedComboSecondary : Ability
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
        stateMachine.Move(Vector3.zero, deltaTime);

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
