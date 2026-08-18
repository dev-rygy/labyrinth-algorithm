/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/07/2025
 * Last Modified:   02/17/2025 (Ryan)
 * Notes:           Every active ability is required to inherit this
 *                      parent class in order to inherent the interface
*/
using System.Collections;
using UnityEngine;
using System;

namespace RyansLibrary.Abilities
{
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

    public abstract class Ability : ScriptableObject, IAbility
    {
        public static float cooldownUpdateRate = 0.1f;      // cooldown update rate for all abilities

        // Event for UI
        public event Action<float, float> OnAbilityCooldown;

        protected PlayerStateMachine stateMachine;

        [SerializeField] public AbilityType Type;
        [SerializeField] public Sprite abilityIcon;
        [SerializeField] public float cooldown;

        private bool _onCooldown = false;
        public bool OnCooldown => _onCooldown;

        private Coroutine coroutine;

        public abstract void Enter();

        public abstract void Tick(float deltaTime);

        public abstract void Exit();

        /// <summary>
        /// Start the ability's cooldown. The player may not use the ability again until the
        /// cooldown time is exhausted.
        /// </summary>
        /// <param name="cooldownTime">Cooldown time</param>
        public void StartCooldown()
        {
            if (OnCooldown)       // If the ability is already on cooldown then return
                return;

            coroutine = stateMachine.StartCoroutine(CooldownCo());
        }

        /// <summary>
        /// Reset the cooldown parameter and coroutine
        /// </summary>
        public void ResetCooldown()
        {
            _onCooldown = false;
            if (coroutine != null)
                stateMachine.StopCoroutine(coroutine);
        }

        private IEnumerator CooldownCo()
        {
            _onCooldown = true;
            float timeLeft = cooldown;

            while (timeLeft > 0)
            {
                OnAbilityCooldown?.Invoke(timeLeft, cooldown);

                yield return new WaitForSeconds(cooldownUpdateRate);

                timeLeft -= cooldownUpdateRate;
            }

            OnAbilityCooldown?.Invoke(0, cooldown);       // Invoke one last time at 0 to ensure 0 was reached

            _onCooldown = false;
        }
    }
}
