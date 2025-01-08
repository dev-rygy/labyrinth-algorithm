/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/07/2025
 * Last Modified:   01/07/2025
 * Notes:           Every active ability is required to inherit this
 *                      parent class in order to inherent the interface
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RyansLibrary.StateMachine;

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
    public string AnimationName { get; protected set; }

    public float TransitionDuration { get; protected set; } = 0.1f;

    public abstract void Enter(PlayerStateMachine stateMachine);

    public abstract void Tick(float deltaTime, PlayerStateMachine stateMachine);

    public abstract void Exit(PlayerStateMachine stateMachine);
}
