/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/12/2025
 * Last Modified:   02/12/2025 (Ryan)
 * Notes:           
*/
using System;
using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Abilities
{
    public class AbilityFactory : MonoBehaviour
    {
        public static AbilityFactory Instance { get; private set; }

        private Dictionary<string, Func<AbilityData, Ability>> AbilityDict;

        // Player reference
        private PlayerStateMachine stateMachine;

        public void Awake()
        {
            // Handle singleton
            if (Instance && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void Start()
        {
            // PlayerStateMachine Singleton Ref.
            stateMachine = PlayerStateMachine.Instance;

            InitDictionary();
        }

        private void InitDictionary()
        {
            AbilityDict = new Dictionary<string, Func<AbilityData, Ability>>
            {
                { "UnarmedComboPrim", data => new UnarmedComboAttackPrimary(stateMachine, Animator.StringToHash(data.Animation), data.AbilityKnockback) },
                { "UnarmedPowerPrim", data => new UnarmedPowerAttackPrimary(stateMachine, Animator.StringToHash(data.Animation), data.AbilityKnockback) },
                { "UnarmedComboSec", data => new UnarmedComboAttackSecondary(stateMachine, Animator.StringToHash(data.Animation)) },
                { "UnarmedPowerSec", data => new UnarmedPowerAttackSecondary(stateMachine, Animator.StringToHash(data.Animation), data.AbilityKnockback) },
                { "BroadswordComboPrim", data => new BroadswordComboAttackPrimary(stateMachine, Animator.StringToHash(data.Animation), data.AbilityKnockback, data.BaseCooldown) },
                { "BroadswordPowerPrim", data => new BroadswordPowerAttackPrimary(stateMachine, Animator.StringToHash(data.Animation), data.AbilityKnockback, data.BaseCooldown) },
                { "BroadswordComboSec", data => new BroadswordComboAttackSecondary(stateMachine) },
                { "BroadswordPowerSec", data => new BroadswordPowerAttackSecondary(stateMachine) },
                { "BucklerComboSec", data => new BucklerComboAttackSecondary(stateMachine, Animator.StringToHash(data.Animation), data.BaseCooldown) },
                { "BucklerPowerSec", data => new BucklerPowerAttackSecondary(stateMachine, Animator.StringToHash(data.Animation), data.BaseCooldown) },
            };
        }

        /// <summary>
        /// Equip an ability from the AbilityData passed in. If the ability does not exist then return null to
        /// tell the system no ability exists.
        /// </summary>
        /// <param name="type">The ability key/category</param>
        /// <returns>A new instance of the ability</returns>
        public Ability GetAbility(AbilityData data)
        {
            if (AbilityDict.TryGetValue(data.AbilityID, out Func<AbilityData, Ability> abilityConstructor))
            {
                // The key was found, and abilityCreator is a delegate that can create the ability instance.
                return abilityConstructor(data);  // Call the delegate to create and return the ability instance.
            }
            else
            {
                // Handle the case where the key is not found in the dictionary.
                Debug.LogWarning("Ability Factory Warning: Ability does not exist.");
                return null;
            }
        }
    }

    #region Unarmed Abilities
    public class UnarmedComboAttackPrimary : Ability
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
                stateMachine.TransitionStates(PlayerStates.Idle);
        }
    }

    public class UnarmedPowerAttackPrimary : Ability
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
            stateMachine.TransitionStates(PlayerStates.Idle);
        }
    }

    public class UnarmedComboAttackSecondary : Ability
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
                stateMachine.TransitionStates(PlayerStates.Idle);
                return;
            }
        }

        public override void Exit() { }
    }

    public class UnarmedPowerAttackSecondary : Ability
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
            stateMachine.TransitionStates(PlayerStates.Idle);
        }
    }
    #endregion

    #region Broadsword Abilities
    public class BroadswordComboAttackPrimary : Ability
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

    public class BroadswordPowerAttackPrimary : Ability
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

    public class BroadswordComboAttackSecondary : Ability
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

    public class BroadswordPowerAttackSecondary : Ability
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
    #endregion

    #region Buckler Abilities
    public class BucklerComboAttackSecondary : Ability
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

    public class BucklerPowerAttackSecondary : Ability
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
    #endregion
}
