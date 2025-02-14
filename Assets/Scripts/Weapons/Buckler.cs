/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/07/2025
 * Last Modified:   02/11/2025 (Ryan)
 * Notes:           Bucker stats and abilities
*/
using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Buckler : Weapon
{
    // Animator Hash Codes
    private readonly int ANIM_COMBO_SEC_HASH = Animator.StringToHash("Shield Block");
    private readonly int ANIM_POWER_SEC_HASH = Animator.StringToHash("Shield Bash");

    [Header("Cooldowns")]
    [SerializeField] private float _secondaryComboCooldown = 1f;
    [SerializeField] private float _secondaryPowerCooldown = 1f;

    private class BucklerComboAttackSecondary : Ability
    {
        private int animHash;
        private float cooldown;

        public BucklerComboAttackSecondary(PlayerStateMachine stateMachine, int animHash, float cooldown) : base(stateMachine) 
        {
            this.animHash = animHash;
            this.cooldown = cooldown;   
        }

        public override void Enter()
        {
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
            // Begin Cooldown
            StartCooldown(cooldown);
        }
    }

    private class BucklerPowerAttackSecondary : Ability
    {
        private int animHash;
        private float cooldown;

        public BucklerPowerAttackSecondary(PlayerStateMachine stateMachine, int animHash, float cooldown) : base(stateMachine) 
        {
            this.animHash = animHash;
            this.cooldown = cooldown;
        }

        public override void Enter()
        {
            stateMachine.AnimationTimestamps.OnComboPrimExit += PowerExit;

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
            // Begin Cooldown
            StartCooldown(cooldown);

            stateMachine.AnimationTimestamps.OnComboPrimExit -= PowerExit;
        }

        private void PowerExit()
        {
            stateMachine.TransitionStates(PlayerStates.Idle);
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
                stateMachine.SetAbilityImage(type, SecondaryComboSprite);
                return new BucklerComboAttackSecondary(stateMachine, ANIM_COMBO_SEC_HASH, _secondaryComboCooldown);
            case AbilityType.PowerAttackSecondary:
                stateMachine.SetAbilityImage(type, SecondaryPowerSprite);
                return new BucklerPowerAttackSecondary(stateMachine, ANIM_POWER_SEC_HASH, _secondaryPowerCooldown);
            default:
                return null;
        }
    }
}
