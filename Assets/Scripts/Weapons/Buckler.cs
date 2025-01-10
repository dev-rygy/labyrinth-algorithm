using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buckler : Weapon
{
    // Animator Hash Codes
    private readonly int ANIM_COMBO_SEC_HASH = Animator.StringToHash("Shield Block");
    private readonly int ANIM_POWER_SEC_HASH = Animator.StringToHash("Shield Bash");

    private class BucklerComboAttackSecondary : Ability
    {
        private int animHash;

        public BucklerComboAttackSecondary(PlayerStateMachine stateMachine, int animHash) : base(stateMachine) 
        {
            this.animHash = animHash;
        }

        public override void Enter()
        {
            // Play the attack's animation
            stateMachine.Animator.CrossFadeInFixedTime(animHash, 0.1f);
        }

        public override void Tick(float deltaTime)
        {
            stateMachine.Move(Vector3.zero, deltaTime);

            if (!stateMachine.Input.IsHoldingSecondaryCombo)
            {
                stateMachine.SwitchState(new PlayerIdleState(stateMachine));
                return;
            }
        }

        public override void Exit() { }
    }

    private class BucklerPowerAttackSecondary : Ability
    {
        private int animHash;

        public BucklerPowerAttackSecondary(PlayerStateMachine stateMachine, int animHash) : base(stateMachine) 
        {
            this.animHash = animHash;
        }

        public override void Enter()
        {
            stateMachine.AnimationTimestamps.OnComboPrimExit += PowerExit;

            // Play the attack's animation
            stateMachine.Animator.CrossFadeInFixedTime(animHash, 0.1f);
        }

        public override void Tick(float deltaTime)
        {
            stateMachine.Move(Vector3.zero, deltaTime);
        }

        public override void Exit()
        {
            stateMachine.AnimationTimestamps.OnComboPrimExit -= PowerExit;
        }

        private void PowerExit()
        {
            stateMachine.SwitchState(new PlayerIdleState(stateMachine));
        }
    }

    /// <summary>
    /// Equip an ability from the weapon. If the ability does not exist then return null to
    /// tell the system no ability exists.
    /// </summary>
    /// <param name="type">The ability key/category</param>
    /// <returns>A new instance of the ability</returns>
    public override Ability GetAbility(AbilityType type, PlayerStateMachine stateMachine)
    {
        switch (type)
        {
            case AbilityType.ComboAttackSecondary:
                return new BucklerComboAttackSecondary(stateMachine, ANIM_COMBO_SEC_HASH);
            case AbilityType.PowerAttackSecondary:
                return new BucklerPowerAttackSecondary(stateMachine, ANIM_POWER_SEC_HASH);
            default:
                return null;
        }
    }
}
