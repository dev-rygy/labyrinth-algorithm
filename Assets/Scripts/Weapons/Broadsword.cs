/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/07/2025
 * Last Modified:   01/07/2025
 * Notes:           Broadsword Weapon stats and abilities
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RyansLibrary.StateMachine;
using RyansLibrary.Input;

public class Broadsword : Weapon
{
    [SerializeField] private float _primaryComboAttackForce = 3f;
    [SerializeField] private float _primaryPowerAttackForce = 5f;

    // Animator Hash Codes
    private readonly int ANIM_COMBO_PRIM_HASH = Animator.StringToHash("Sword Combo Primary");
    private readonly int ANIM_POWER_PRIM_HASH = Animator.StringToHash("Sword Power Primary");
    private readonly int ANIM_COMBO_SEC_HASH = Animator.StringToHash("Sword Combo Secondary");
    private readonly int ANIM_POWER_SEC_HASH = Animator.StringToHash("Sword Power Secondary");

    private class BroadswordComboAttackPrimary : Ability
    {
        private int animHash;
        private float attackForce;

        private bool _hasPressedAttack;
        private bool _canContinue;

        public BroadswordComboAttackPrimary(PlayerStateMachine stateMachine, int animHash, float attackForce) : base(stateMachine) 
        {
            this.attackForce = attackForce;
            this.animHash = animHash;
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
            stateMachine.Move(Vector3.zero, deltaTime);
        }

        public override void Exit()
        {
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

        public BroadswordPowerAttackPrimary(PlayerStateMachine stateMachine, int animHash, float attackForce) : base(stateMachine) 
        {
            this.attackForce = attackForce;
            this.animHash = animHash;
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
            stateMachine.Move(Vector3.zero, deltaTime);
        }

        public override void Exit()
        {
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
                return new BroadswordComboAttackPrimary(stateMachine, ANIM_COMBO_PRIM_HASH, _primaryComboAttackForce);
            case AbilityType.PowerAttackPrimary:
                return new BroadswordPowerAttackPrimary(stateMachine, ANIM_POWER_PRIM_HASH, _primaryPowerAttackForce);
            case AbilityType.ComboAttackSecondary:
                return new BroadswordComboAttackSecondary(stateMachine);
            case AbilityType.PowerAttackSecondary:
                return new BroadswordPowerAttackSecondary(stateMachine);
            default:
                return null;
        }
    }
}

