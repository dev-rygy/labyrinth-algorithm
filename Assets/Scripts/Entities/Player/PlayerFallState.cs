/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/22/2025 (Ryan)
 * Notes:           Player Fall State
*/
using System.Collections;
using UnityEngine;

public class PlayerFallState : PlayerState
{
    // Animator Hash Codes
    private readonly int k_animFallingHash = Animator.StringToHash("Falling");
    private readonly int k_animLandingHash = Animator.StringToHash("Landing");

    public PlayerFallState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    private bool _isLanding = false;

    public override void Enter()
    {
        _isLanding = false;     // reset flag

        // Play the Falling animation
        stateMachine.Animator.CrossFadeInFixedTime(k_animFallingHash, 0.1f);
    }

    public override void Tick(float deltaTime)
    {
        if (_isLanding)
        {
            stateMachine.Move(Vector3.zero, deltaTime); // Just fall with gravity
            return;
        }

        // The play can move the character very slowly while falling
        Vector3 moveInput = new Vector3(stateMachine.Input.MovementInput.x, 0, stateMachine.Input.MovementInput.y);
        stateMachine.Move(moveInput * (stateMachine.MovementSpeed / 2), deltaTime);

        if (moveInput != Vector3.zero)  // Code below only needed if the player is moving
        {
            // Rotate the player character in the normalized direction of movement
            Vector3 moveInputNormalized = new Vector3(stateMachine.Input.MoveDirectionNormalized.x, 0, stateMachine.Input.MoveDirectionNormalized.y);
            stateMachine.ApplyCharacterRotation(moveInputNormalized, deltaTime);
        }

        // Falling state if the player is not grounded
        if (stateMachine.ForceReciever.IsGrounded())
        {
            _isLanding = true;
            stateMachine.StartCoroutine(LandCo());
        }
    }

    public override void Exit()
    {
        Debug.Log("Fall state exited.");
    }

    private IEnumerator LandCo()
    {
        // Start the landing animation
        stateMachine.Animator.CrossFadeInFixedTime(k_animLandingHash, 0.1f);

        // Wait until the animator is fully in the landing animation state
        yield return new WaitUntil(() => stateMachine.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == k_animLandingHash);

        // Now wait until this specific animation has finished. normalizedTime increases monotonically past 1
        // once the clip completes (it does not wrap on its own), so this can't be missed the way a narrow
        // "% 1.0f < 0.01f" window could if a frame landed just past the wrap point.
        yield return new WaitUntil(() => stateMachine.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f && !stateMachine.Animator.IsInTransition(0));

        stateMachine.TransitionStates(PlayerStates.Idle);
    }
}
