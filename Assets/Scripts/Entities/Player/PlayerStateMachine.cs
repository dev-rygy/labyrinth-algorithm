/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/07/2025
 * Notes:           The Player's State Machine
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RyansLibrary.StateMachine;
using RyansLibrary.Input;

/// <summary> Player Controls Manager that stores references and data for the different states to use. </summary>
public class PlayerStateMachine : StateMachine
{
    public static PlayerStateMachine Instance { get; private set; }

    // TODO: Implement static states to save on memory

    [field: Header("Required References")]
    [field: SerializeField] public Transform PlayerCharacter { get; private set; }
    [field: SerializeField] public Weapon UnarmedWeapon { get; private set; }

    [field: Header("Movement")]
    [field: SerializeField] public float MovementSpeed { get; private set; } = 5f;
    [field: SerializeField] public float MoveRotationDampValue { get; private set; }
    [field: SerializeField] public float ClimbSpeed { get; private set; } = 3f;
    [field: SerializeField] public float ClimbTriggerOffset { get; private set; } = 0.1f;
    [field: SerializeField] public float ClimbInteractDistance { get; private set; } = 0.4f;
    [field: SerializeField] [Range(0, 1)] private float _climbAngleRange = 0.85f;

    [field: Header("Equipped")]
    [field: SerializeField] public Weapon PrimaryWeapon { get; private set; }
    [field: SerializeField] public Weapon SecondaryWeapon { get; private set; }

    // References
    public InputHandler Input { get; private set; } // reference to the input handler
    public CharacterController Controller { get; private set; } // reference to the player's controller
    public Animator Animator { get; private set; }  // reference to the player's animator
    public AnimationTimestamps AnimationTimestamps { get; private set; }
    public ForceReciever ForceReciever { get; private set; }

    // Player Abilities
    public Ability ComboAttackPrimary { get; private set; }
    public Ability ComboAttackSecondary { get; private set; }
    public Ability PowerAttackPrimary { get; private set; }
    public Ability PowerAttackSecondary { get; private set; }

    private void Awake()
    {
        // Handle singleton
        if (Instance && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        if (PlayerCharacter == null)
        {
            Debug.LogError("Player State Machine Error: The Player Character object is missing.");
            return;
        }

        // Hook up references
        Input = InputHandler.Instance;                              // An input handler must be in the player's scene
        Controller = GetComponent<CharacterController>();           // The player must have a character controller
        Animator = PlayerCharacter.GetComponent<Animator>();        // The animator is on the "Player Character" child object
        AnimationTimestamps = PlayerCharacter.GetComponent<AnimationTimestamps>();      // The timestamp events for the player abilities
        ForceReciever = GetComponent<ForceReciever>();              // The player must have a force reciever to interact with gravity

        // Kick off the player's state machine
        // Transition to the first state
        SwitchState(new PlayerIdleState(this));

        // Equip Weapons
        EquipPrimaryWeapon(PrimaryWeapon);          // Equip the primary weapon that is referenced in the inspector off rip
        EquipSecondaryWeapon(SecondaryWeapon);      // Equip the secondary weapon that is referenced in the inspector off rip
    }

    /// <summary>
    /// Equips the abilities of the weapon that is passed into the function.
    /// If the weapon does not fill a ability space then the space will be null
    /// </summary>
    /// <param name="weapon">The weapon to be equipped.</param>
    public void EquipPrimaryWeapon(Weapon weapon)
    {
        // If the weapon passed in was null then equip the unarmed weapon
        if (weapon == null)
            weapon = UnarmedWeapon;

        PrimaryWeapon = weapon;

        Ability ability = null;

        // Set Combo Attack Primary
        ability = weapon.GetAbility(AbilityType.ComboAttackPrimary, this);
        SetAbility(ability, AbilityType.ComboAttackPrimary);

        // Set Power Attack Primary
        ability = weapon.GetAbility(AbilityType.PowerAttackPrimary, this);
        SetAbility(ability, AbilityType.PowerAttackPrimary);

        // If there is a secondary weapon equipped then do not replace the secondary abilitites
        if (SecondaryWeapon != null)
            return;

        // Set Combo Attack Secondary
        ability = weapon.GetAbility(AbilityType.ComboAttackSecondary, this);
        SetAbility(ability, AbilityType.ComboAttackSecondary);

        // Set Power Attack Secondary
        ability = weapon.GetAbility(AbilityType.PowerAttackSecondary, this);
        SetAbility(ability, AbilityType.PowerAttackSecondary);
    }

    /// <summary>
    /// Equips the abilities of the weapon that is passed into the function.
    /// If the weapon does not fill a ability space then the space will be null
    /// </summary>
    /// <param name="weapon">The weapon to be equipped.</param>
    public void EquipSecondaryWeapon(Weapon weapon)
    {
        // If the weapon passed in was null then equip the unarmed weapon
        if (weapon == null)
            weapon = UnarmedWeapon;

        PrimaryWeapon = weapon;

        SecondaryWeapon = weapon;
        Ability ability = null;

        // Set Combo Attack Secondary
        ability = weapon.GetAbility(AbilityType.ComboAttackSecondary, this);
        SetAbility(ability, AbilityType.ComboAttackSecondary);

        // Set Power Attack Secondary
        ability = weapon.GetAbility(AbilityType.PowerAttackSecondary, this);
        SetAbility(ability, AbilityType.PowerAttackSecondary);
    }

    /// <summary> Sets up an ability and assigns it to a slot. </summary>
    /// <param name="ability">Ability reference</param>
    /// <param name="type">Ability slot</param>
    public void SetAbility(Ability ability, AbilityType type)
    {
        switch(type)
        {
            case AbilityType.ComboAttackPrimary:
                ComboAttackPrimary = ability;
                if (DebugStateMachine) Debug.Log("Combo Attack Primary set to " + ability);
                break;
            case AbilityType.ComboAttackSecondary:
                ComboAttackSecondary = ability;
                if (DebugStateMachine) Debug.Log("Combo Attack Secondary set to " + ability);
                break;
            case AbilityType.PowerAttackPrimary:
                PowerAttackPrimary = ability;
                if (DebugStateMachine) Debug.Log("Power Attack Primary set to " + ability);
                break;
            case AbilityType.PowerAttackSecondary:
                PowerAttackSecondary = ability;
                if (DebugStateMachine) Debug.Log("Power Attack Secondary set to " + ability);
                break;
            default:
                return;
        }
    }

    /// <summary> Move the player controller in any which way please. Also apply gravity. </summary>
    /// <param name="motion">Motion vector</param>
    /// <param name="deltaTime">Time per frame</param>
    public void Move(Vector3 motion, float deltaTime)
    {
        // Handle Movement
        Controller.Move((motion + ForceReciever.Movement) * deltaTime);
    }

    public bool CanClimb()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * ClimbTriggerOffset;
        Vector3 moveDirection = PlayerCharacter.transform.forward;

        // If the raycast does not hit an object with a collider; can not climb
        if (!Physics.Raycast(rayOrigin, moveDirection, out RaycastHit raycastHit, ClimbInteractDistance))
            return false;

        // If the player is not facing towards the object; can not climb
        if ((Vector3.Dot(PlayerCharacter.transform.forward, raycastHit.transform.forward)) < _climbAngleRange)
            return false;

        // If the player is facing an object of type "Climbable"; can climb
        return raycastHit.transform.gameObject.CompareTag("Climbable");
    }

    /// <summary> Face the player character towards the direction they are moving </summary>
    /// <param name="direction">The direction the player must face; Best if it is a normalized vector.</param>
    /// <param name="deltaTime">Time per frame</param>
    public void ApplyCharacterRotation(Vector3 direction, float deltaTime)
    {
        // have the player character look in the direction of movement
        // Quaternion.Lerp() changes between two quaternion values based on a delta time
        PlayerCharacter.rotation = Quaternion.Lerp(PlayerCharacter.rotation,
            Quaternion.LookRotation(direction),
            deltaTime * MoveRotationDampValue);
    }
}
