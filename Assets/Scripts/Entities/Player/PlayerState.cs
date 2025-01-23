/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025 (Ryan)
 * Notes:           Player Base State, to be inherited by all player states
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RyansLibrary.StateMachine;
using UnityEngine.EventSystems;

/// <summary>
/// The base overall state class that all player states will inherit from.
/// contains useful methods the states can utalize. Also contains a reference
/// to the player's state machine
/// </summary>
public abstract class PlayerState : State
{
    // Reference to the overall player state machine that other states will continuously call for data and other references
    protected PlayerStateMachine stateMachine;

    // Constructor
    public PlayerState(PlayerStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }
}
