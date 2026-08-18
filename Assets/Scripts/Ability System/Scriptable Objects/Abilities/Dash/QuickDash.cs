/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/17/2025
 * Last Modified:   03/06/2025 (Ryan)
 * Notes:           Default Dash when the player is unclothed or armor dash is the same as default
*/
using RyansLibrary.Abilities;
using UnityEngine;

[CreateAssetMenu(fileName = "NakedDashAbility", menuName = "Scriptable Objects/Ability/Armor/Naked/NakedDashAbility", order = 0)]
public class QuickDash : Ability
{
    [Header("Properties")]
    [SerializeField] private float dodgeDuration;
    [SerializeField] private float dodgeDistance;

    [Header("Animation")]
    [SerializeField] private string animHash;
    [SerializeField] private float crossfadeDuration = 0.1f;
  

    private Vector3 movement;
    private float _remainingDodgeTime;

    public override void Enter()
    {
        stateMachine = PlayerStateMachine.Instance;

        movement = new Vector3(stateMachine.Input.MoveDirectionNormalized.x, 0, stateMachine.Input.MoveDirectionNormalized.y) * (dodgeDistance / dodgeDuration);

        _remainingDodgeTime = dodgeDuration;

        stateMachine.Animator.CrossFadeInFixedTime(animHash, crossfadeDuration);

        stateMachine.Health.ToggleInvulnerable(true);       // Make player invulnerable while dodging
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.Move(movement, deltaTime);

        _remainingDodgeTime -= deltaTime;

        if (_remainingDodgeTime <= 0)
        {
            stateMachine.TransitionStates(PlayerStates.Idle);
        }
    }

    public override void Exit()
    {
        stateMachine.Health.ToggleInvulnerable(false);      // Make player vulnerable again when leaving dodge state
        StartCooldown();
    }
}
