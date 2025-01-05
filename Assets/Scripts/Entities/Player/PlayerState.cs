/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025
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

    private float _verticalVelocity;

    // Constructor
    public PlayerState(PlayerStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    /// <summary>
    /// Move the player controller in any which way you please
    /// </summary>
    /// <param name="motion">Motion vector</param>
    /// <param name="deltaTime">Time per frame</param>
    protected void Move(Vector3 motion, float deltaTime)
    {
        stateMachine.Controller.Move(motion * deltaTime);
    }

    /// <summary>
    /// Apply gravity to the player controller
    /// </summary>
    /// <param name="deltaTime">Time per frame</param>
    protected void ApplyGravity(float deltaTime)
    {
        if (stateMachine.IsGrounded() && _verticalVelocity < 0.0f)
            _verticalVelocity = -1f;
        else
        {
            _verticalVelocity += stateMachine.GravityMultiplier * Time.deltaTime;
        }
        Move(new Vector3(0, _verticalVelocity, 0), deltaTime);
    }

    /// <summary>
    /// Face the player character towards the direction they are moving
    /// </summary>
    /// <param name="direction">The direction vector</param>
    /// <param name="deltaTime">Time per frame</param>
    protected void ApplyCharacterRotation(Vector3 direction, float deltaTime)
    {
        // have the player character look in the direction of movement
        // Quaternion.Lerp() changes between two quaternion values based on a delta time
        stateMachine.PlayerCharacter.rotation = Quaternion.Lerp(stateMachine.PlayerCharacter.rotation, 
            Quaternion.LookRotation(direction), 
            deltaTime * stateMachine.MoveRotationDampValue);
    }
}
