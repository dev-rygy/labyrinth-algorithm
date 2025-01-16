using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unarmed : Weapon
{
    [SerializeField] private float _primaryComboAttackForce = 3f;
    [SerializeField] private float _primaryPowerAttackForce = 5f;
    [SerializeField] private float _secondaryComboAttackForce = 3f;

    // Animator Hash Codes
    private readonly int ANIM_COMBO_PRIM_HASH = Animator.StringToHash("Unarmed Combo Primary");
    private readonly int ANIM_POWER_PRIM_HASH = Animator.StringToHash("Unarmed Power Primary");
    private readonly int ANIM_COMBO_SEC_HASH = Animator.StringToHash("Unarmed Combo Secondary");
    private readonly int ANIM_POWER_SEC_HASH = Animator.StringToHash("Unarmed Power Secondary");

    private class UnarmedComboAttackPrimary : Ability
    {
        private int animHash;
        private float attackForce;

        private bool _hasPressedAttack;
        private bool _canContinue;

        public UnarmedComboAttackPrimary(PlayerStateMachine stateMachine, int animHash, float attackForce) : base(stateMachine)
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
                stateMachine.SwitchState(new PlayerIdleState(stateMachine));
        }
    }

    private class UnarmedPowerAttackPrimary : Ability
    {
        private int animHash;
        private float attackForce;

        public UnarmedPowerAttackPrimary(PlayerStateMachine stateMachine, int animHash, float attackForce) : base(stateMachine)
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
            stateMachine.SwitchState(new PlayerIdleState(stateMachine));
        }
    }

    private class UnarmedComboAttackSecondary : Ability
    {
        private int animHash;

        public UnarmedComboAttackSecondary(PlayerStateMachine stateMachine, int animHash) : base(stateMachine)
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

    private class UnarmedPowerAttackSecondary : Ability
    {
        private int animHash;
        private float attackForce;

        public UnarmedPowerAttackSecondary(PlayerStateMachine stateMachine, int animHash, float attackForce) : base(stateMachine)
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
            case AbilityType.ComboAttackPrimary:
                return new UnarmedComboAttackPrimary(stateMachine, ANIM_COMBO_PRIM_HASH, _primaryComboAttackForce);
            case AbilityType.PowerAttackPrimary:
                return new UnarmedPowerAttackPrimary(stateMachine, ANIM_POWER_PRIM_HASH, _primaryPowerAttackForce);
            case AbilityType.ComboAttackSecondary:
                return new UnarmedComboAttackSecondary(stateMachine, ANIM_COMBO_SEC_HASH);
            case AbilityType.PowerAttackSecondary:
                return new UnarmedPowerAttackSecondary(stateMachine, ANIM_POWER_SEC_HASH, _secondaryComboAttackForce);
            default:
                return null;
        }
    }
}
