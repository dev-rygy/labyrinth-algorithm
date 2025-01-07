/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025
 * Notes:           The Player's State Machine
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RyansLibrary.StateMachine;
using RyansLibrary.Input;
using UnityEngine.InputSystem.XR.Haptics;

/// <summary>
/// Player Controls Manager that stores references and data for the different states to 
/// use.
/// </summary>
public class PlayerStateMachine : StateMachine
{
    // TODO: Implement static states to save on memory

    [field: SerializeField] public Transform PlayerCharacter { get; private set; }
    [field: SerializeField] public float MovementSpeed { get; private set; } = 5;
    [field: SerializeField] public float GravityMultiplier { get; private set; } = -9.81f;
    [field: SerializeField] public float MoveRotationDampValue { get; private set; }
    public InputHandler Input { get; private set; } // reference to the input handler
    public CharacterController Controller { get; private set; } // reference to the player's controller
    public Animator Animator { get; private set; }  // reference to the player's animator

    // Can be used to check if the player is grounded
    public bool IsGrounded() => Controller.isGrounded;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerCharacter == null)
        {
            Debug.LogError("Player State Machine Error: The player character object is missing.");
            return;
        }

        Input = InputHandler.Instance;
        Controller = GetComponent<CharacterController>();
        Animator = PlayerCharacter.GetComponent<Animator>();        // The animator is on the "Player Character" child object

        // Transition to the first state
        SwitchState(new PlayerIdleState(this)); 
    }
}
