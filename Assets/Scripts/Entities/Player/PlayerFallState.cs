using System.Collections;
using UnityEngine;

public class PlayerFallState : PlayerState
{
    // Animator Hash Codes
    private readonly int ANIM_FALLING_HASH = Animator.StringToHash("Falling");
    private readonly int ANIM_LANDING_HASH = Animator.StringToHash("Landing");

    public PlayerFallState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    private bool _isLanding = false;

    public override void Enter()
    {
        // Play the Falling animation
        stateMachine.Animator.CrossFadeInFixedTime(ANIM_FALLING_HASH, 0.1f);
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
            Vector3 moveInputNormalized = new Vector3(stateMachine.Input.MovementInputNormalized.x, 0, stateMachine.Input.MovementInputNormalized.y);
            stateMachine.ApplyCharacterRotation(moveInputNormalized, deltaTime);
        }

        // Falling state if the player is not grounded
        if (stateMachine.ForceReciever.IsGrounded())
        {
            _isLanding = true;
            stateMachine.StartCoroutine(LandCo());
        }
    }

    public override void Exit() { }

    private IEnumerator LandCo()
    {
        stateMachine.ForceReciever.EnableGroundCheck = false;

        // Start the landing animation
        stateMachine.Animator.CrossFadeInFixedTime(ANIM_LANDING_HASH, 0.1f);

        // Wait until the animator is fully in the landing animation state
        yield return new WaitUntil(() => stateMachine.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == ANIM_LANDING_HASH);

        // Now wait until this specific animation has finished
        yield return new WaitUntil(() => stateMachine.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1.0f < 0.01f && !stateMachine.Animator.IsInTransition(0));

        stateMachine.ForceReciever.EnableGroundCheck = true;

        stateMachine.SwitchState(new PlayerIdleState(stateMachine));
    }
}
