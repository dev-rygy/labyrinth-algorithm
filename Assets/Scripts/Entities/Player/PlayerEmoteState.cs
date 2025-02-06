/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/06/2025
 * Last Modified:   02/06/2025 (Ryan)
 * Notes:           Player Emote State; emote the player on a button press and exit on
 *                      almost any other condition (damaged, new input)
*/
using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEmoteState : PlayerState
{
    // Animator Hash Codes
    private readonly int ANIM_EMOTE_HASH = Animator.StringToHash("Dance");
    // Time to transition between animtions
    private const float ANIMATOR_TRANSITION_DURATION = 0.1f;

    public PlayerEmoteState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Input Event
        InputHandler.OnAny += OnAnyKey;

        // Sheathe weapons to hide any weird effect with weapons
        stateMachine.SheatheWeapons();

        // Play selected emote animation
        stateMachine.Animator.CrossFadeInFixedTime(ANIM_EMOTE_HASH, ANIMATOR_TRANSITION_DURATION);
    }

    public override void Tick(float deltaTime)
    {
        stateMachine.Move(Vector3.zero, deltaTime);     // Continue to apply gravity
    }

    public override void Exit()
    {
        // Unsubscribe to transition events
        InputHandler.OnAny -= OnAnyKey;

        // Take out weapons again
        stateMachine.UnsheatheWeapons();
    }

    private void OnAnyKey()
    {
        stateMachine.TransitionStates(PlayerStates.Idle);
    }
}
