/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/07/2025
 * Last Modified:   02/11/2025 (Ryan)
 * Notes:           Broadsword Weapon stats and abilities
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RyansLibrary.StateMachine;
using RyansLibrary.Input;
using Unity.VisualScripting;

public class Broadsword : Weapon
{
    [Header("Knockbacks")]
    [SerializeField] private float _primaryComboAttackForce = 3f;
    [SerializeField] private float _primaryPowerAttackForce = 5f;

    [Header("Cooldowns")]
    [SerializeField] private float _primaryComboCooldown = 1f;
    [SerializeField] private float _secondaryComboCooldown = 1f;
    [SerializeField] private float _primaryPowerCooldown = 1f;
    [SerializeField] private float _secondaryPowerCooldown = 1f;

    // Animator Hash Codes
    private readonly int ANIM_COMBO_PRIM_HASH = Animator.StringToHash("Sword Combo Primary");
    private readonly int ANIM_POWER_PRIM_HASH = Animator.StringToHash("Sword Power Primary");
    private readonly int ANIM_COMBO_SEC_HASH = Animator.StringToHash("Sword Combo Secondary");
    private readonly int ANIM_POWER_SEC_HASH = Animator.StringToHash("Sword Power Secondary");

    private class BroadswordComboAttackPrimary : Ability
    {
        private int animHash;
        private float attackForce;
        private float cooldown;

        private bool _hasPressedAttack;
        private bool _canContinue;

        public BroadswordComboAttackPrimary(PlayerStateMachine stateMachine, int animHash, float attackForce, float cooldown) : base(stateMachine) 
        {
            this.attackForce = attackForce;
            this.animHash = animHash;
            this.cooldown = cooldown;
        }

        public override void Enter()
        {
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
            // Begin Cooldown
            StartCooldown(cooldown);

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

    private class BroadswordPowerAttackPrimary : Ability
    {
        private int animHash;
        private float attackForce;
        private float cooldown;

        public BroadswordPowerAttackPrimary(PlayerStateMachine stateMachine, int animHash, float attackForce, float cooldown) : base(stateMachine) 
        {
            this.attackForce = attackForce;
            this.animHash = animHash;
            this.cooldown = cooldown;
        }

        public override void Enter()
        {

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
            // Begin Cooldown
            StartCooldown(cooldown);

            stateMachine.AnimationTimestamps.OnComboPrimExit -= PowerExit;

            stateMachine.AnimationTimestamps.OnComboPrimColliderEnable -= stateMachine.PrimaryWeapon.EnableWeaponCollider;
            stateMachine.AnimationTimestamps.OnComboPrimColliderDisable -= stateMachine.PrimaryWeapon.DisableWeaponCollider;
        }

        private void PowerExit()
        {
            stateMachine.TransitionStates(PlayerStates.Idle);
        }
    }

    private class BroadswordComboAttackSecondary : Ability
    {
        public BroadswordComboAttackSecondary(PlayerStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            stateMachine.TransitionStates(PlayerStates.Idle);
        }

        public override void Tick(float deltaTime)
        {
            throw new System.NotImplementedException();
        }

        public override void Exit()
        {
            throw new System.NotImplementedException();
        }
    }

    private class BroadswordPowerAttackSecondary : Ability
    {
        public BroadswordPowerAttackSecondary(PlayerStateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            stateMachine.TransitionStates(PlayerStates.Idle);
        }

        public override void Exit()
        {
            throw new System.NotImplementedException();
        }

        public override void Tick(float deltaTime)
        {
            throw new System.NotImplementedException();
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
            case AbilityType.ComboAttackPrimary:
                stateMachine.SetAbilityImage(type, PrimaryComboSprite);
                return new BroadswordComboAttackPrimary(stateMachine, ANIM_COMBO_PRIM_HASH, _primaryComboAttackForce, _primaryComboCooldown);
            case AbilityType.PowerAttackPrimary:
                stateMachine.SetAbilityImage(type, PrimaryPowerSprite);
                return new BroadswordPowerAttackPrimary(stateMachine, ANIM_POWER_PRIM_HASH, _primaryPowerAttackForce, _primaryPowerCooldown);
            case AbilityType.ComboAttackSecondary:
                stateMachine.SetAbilityImage(type, SecondaryComboSprite);
                return new BroadswordComboAttackSecondary(stateMachine);
            case AbilityType.PowerAttackSecondary:
                stateMachine.SetAbilityImage(type, SecondaryPowerSprite);
                return new BroadswordPowerAttackSecondary(stateMachine);
            default:
                return null;
        }
    }
}

