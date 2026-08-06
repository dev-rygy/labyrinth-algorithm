/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   03/09/2025 (Ryan)
 * Notes:           Finite State Machine for the Player
 *                  1.) States = { Idle, ComboPrimary, PowerPrimary, ComboSecondary, PowerSecondary, Charge, Cast, Fall, Land, Climb, Dash, DashAttack, Hit, Death, Emote}
 *                  2.) Alphabet = { button_south, button_east, button_west, button_north, left_stick, right_stick, right_shoulder, left_shoulder, right_trigger, 
 *                      left_trigger, interact, take_damage, IsGrounded() = true, IsGrounded() = false, left_stick + right_stick, left_shoulder + right_shoulder, emote }
 *                  3.) Start State = { Idle }
 *                  4.) Final States = { Death }
*/
using RyansLibrary.Abilities;
using RyansLibrary.Console;
using RyansLibrary.Input;
using RyansLibrary.StateMachine;
using System;
using UnityEngine;

public enum PlayerStates
{
    Idle,
    ComboPrim,
    PowerPrim,
    ComboSec,
    PowerSec,
    Charge,
    Cast,
    Fall,
    Climb,
    Dash,
    Impact,
    Death,
    Emote
}

public enum AutoMoveDirections
{
    Forward,
    Backward,
    Left,
    Right,
    None
}

/// <summary> Player Controls Manager that stores references and data for the different states to use. </summary>
public class PlayerStateMachine : StateMachine
{
    #region Variables
    // Events
    public static event Action<PlayerStateMachine> OnPlayerSpawned;
    public event Action<Ability> OnAbilityChanged;      // Invokes when a new ability is slotted

    public static PlayerStateMachine Instance { get; private set; }     // WARNING: This singleton may need to be removed later for networking purposes

    // Static states to save on memory
    private PlayerIdleState _idleState;
    private PlayerComboPrimaryState _comboPrimState;
    private PlayerPowerPrimaryState _powerPrimState;
    private PlayerComboSecondaryState _comboSecState;
    private PlayerPowerSecondaryState _powerSecState;
    private PlayerClimbState _climbState;
    private PlayerFallState _fallState;
    private PlayerDashState _dashState;
    // TODO: Implement the rest of the states below
    // private static PlayerChargeState _chargeState;
    // private static PlayerCastState _castState;
    private PlayerEmoteState _emoteState;
    
    // Inspector Variables
    [field: Header("Required References")]
    [field: SerializeField] public Transform PlayerCharacter { get; private set; }      // The player model with the bones
    [field: SerializeField] public Weapon UnarmedWeapon { get; private set; }       // The abilities the player will have when they do not have a weapon equipped

    [field: Header("Movement")]
    [field: Tooltip("Default speed of the player.")]
    [field: SerializeField] public float MovementSpeed { get; private set; } = 5f;  // Default speed of the player
    [field: Tooltip("Added time it takes for the player to rotate between frames.")] 
    [field: SerializeField] public float MoveRotationDampValue { get; private set; }    // Added time it takes for the player to turn between frames
    [field: Tooltip("The speed of the player when climbing up a climbable object.")]
    [field: SerializeField] public float ClimbSpeed { get; private set; } = 3f; 
    [field: Tooltip("Offset origin of the raycast that detects climbable objects")]
    [field: SerializeField] public float ClimbTriggerOffset { get; private set; } = 0.1f;   // Offset origin of the raycast that detects climbable object
    [field: Tooltip("Max detection distance of the raycast that detects climbable objects")]
    [field: SerializeField] public float ClimbInteractDistance { get; private set; } = 0.4f;    // Max detection distance of the raycast that detects climbable objects
    [field: Tooltip("The acceptable angle between the player's forward vector and the climbable object's forward vector (should not need to go above 90 deg)")]
    [field: SerializeField] [Range(0, 1)] private float _climbInteractionAngle = 0.85f;

    [field: Header("Equipped")]
    [field: Tooltip("Player's primary weapon slot")]
    [field: SerializeField] public Weapon PrimaryWeapon { get; private set; }
    [field: Tooltip("Player's secondary weapon slot")]
    [field: SerializeField] public Weapon SecondaryWeapon { get; private set; }

    // Equipped Abilities
    [field: Header("Equipped Abilities")]
    [field: Tooltip("Ability equipped in the Combo Attack Primary Slot")]
    [field: SerializeField] public Ability ComboAttackPrimaryAbility { get; private set; }
    [field: Tooltip("Ability equipped in the Power Attack Primary Slot")]
    [field: SerializeField] public Ability PowerAttackPrimaryAbility { get; private set; }
    [field: Tooltip("Ability equipped in the Combo Attack Secondary Slot")]
    [field: SerializeField] public Ability ComboAttackSecondaryAbility { get; private set; }
    [field: Tooltip("Ability equipped in the Power Attack Secondary Slot")]
    [field: SerializeField] public Ability PowerAttackSecondaryAbility { get; private set; }
    [field: Tooltip("Ability equipped in the Dash Slot")]
    [field: SerializeField] public Ability DashAbility { get; set; }

    [field: Header("Attach Points")]                                                                // Attach points for objects that move with the player's rig
    [field: Tooltip("Primary weapon parent transform when equipped")]
    [field: SerializeField] public Transform PrimaryWeaponAttachPoint { get; private set; }
    [field: Tooltip("Secondary weapon parent transform when equipped")]
    [field: SerializeField] public Transform SecondaryWeaponAttachPoint { get; private set; }
    [field: Tooltip("Sword parent transform when sheathed")]
    [field: SerializeField] public Transform SwordSheatheAttachPoint { get; private set; }
    [field: Tooltip("Shield parent transform when sheathed")]
    [field: SerializeField] public Transform ShieldSheatheAttachPoint { get; private set; }

    [field: Header("Hitboxes")]
    [field: SerializeField] public Hitbox hitbox { get; private set; }

    // References Assigned In Code
    public InputHandler Input { get; private set; } // reference to the input handler
    public CharacterController Controller { get; private set; } // reference to the player's controller
    public Animator Animator { get; private set; }  // reference to the player's animator
    public AnimationTimestamps AnimationTimestamps { get; private set; }        // reference to player animator events
    public ForceReceiver ForceReciever { get; private set; }        // reference to player physics
    public EntityHealth Health { get; private set; }        // Reference to player health

    public AutoMoveDirections AutoMoveDirection { get; private set; } = AutoMoveDirections.None;
    #endregion

    #region Mono
    private void Awake()
    {
        // Handle singleton; if instance has a reference and the reference is not this object
        if (Instance != null)
        {
            Debug.LogWarning("Player State Machine Warning: Another instance of PlayerStateMachine already exists. Deleting Object...");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;

        // Assign references on object
        Controller = GetComponent<CharacterController>();           // The player must have a character controller
        Animator = PlayerCharacter.GetComponent<Animator>();        // The animator is on the "Player Character" child object
        AnimationTimestamps = PlayerCharacter.GetComponent<AnimationTimestamps>();      // The timestamp events for the player abilities
        ForceReciever = GetComponent<ForceReceiver>();              // The player must have a force reciever to interact with gravity
        Health = GetComponent<EntityHealth>();                      // Player's health behavior is shared with all entities

        OnPlayerSpawned?.Invoke(this);
    }

    void Start()
    {
        // Check required references
        if (PlayerCharacter == null)
        {
            Debug.LogError("Player State Machine Error: The Player Character object is missing.");
            return;
        }
        if (UnarmedWeapon == null)
        {
            Debug.LogError("Player State Machine Error: Unarmed Weapon is missing.");
        }

        // Input Singleton ref.
        Input = InputHandler.Instance;                              // An input handler must be in the player's scene

        RegisterConsoleCommands();

        // Kick off the player's state machine
        // Transition to the first state
        TransitionStates(PlayerStates.Idle);

        // DISABLED FOR DEMO
        // Equip Primary and Secondary Weapons off rip if assigned in the inspector
        //EquipPrimaryWeapon(PrimaryWeapon);
        //EquipSecondaryWeapon(SecondaryWeapon);

        //SetAbility(DashAbility);
    }

    private void OnEnable()
    {
        if (Health == null)
        {
            Debug.LogError("Player State Machine Error: Player's health was null.");
            return;
        }

        // Subscribe to State Events
        Health.OnTakeDamage += HandleTakeDamage;
        Health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (Health == null)
            return;

        UnregisterConsoleCommands();

        // Unsubscribe to State Events
        Health.OnTakeDamage -= HandleTakeDamage;
        Health.OnDeath -= HandleDeath;
    }

    private void OnDestroy()
    {
        // Release player from input
        Input.UnsubscribePlayerInputEvents();

        if (Health == null)
            return;

        UnregisterConsoleCommands();

        // Unsubscribe to State Events
        Health.OnTakeDamage -= HandleTakeDamage;
        Health.OnDeath -= HandleDeath;
    }
    #endregion

    #region State Machine Helpers
    /// <summary>
    /// Transition function for better readability and better handling of static states.
    /// </summary>
    /// <param name="state">The state to transition to.</param>
    public void TransitionStates(PlayerStates state)
    {
        TransitionStates(GetState(state));
    }

    private PlayerState GetState(PlayerStates state)
    {
        switch(state)
        {
            case PlayerStates.Idle: return _idleState ??= new PlayerIdleState(this);
            case PlayerStates.ComboPrim: return _comboPrimState ??= new PlayerComboPrimaryState(this);
            case PlayerStates.PowerPrim: return _powerPrimState ??= new PlayerPowerPrimaryState(this);
            case PlayerStates.ComboSec: return _comboSecState ??= new PlayerComboSecondaryState(this);
            case PlayerStates.PowerSec: return _powerSecState ??= new PlayerPowerSecondaryState(this);
            case PlayerStates.Charge: throw new ArgumentOutOfRangeException(nameof(state), $"Unhandled state: {state}");        // TODO: Implement
            case PlayerStates.Cast: throw new ArgumentOutOfRangeException(nameof(state), $"Unhandled state: {state}");        // TODO: Implement
            case PlayerStates.Fall: return _fallState ??= new PlayerFallState(this);
            case PlayerStates.Climb: return _climbState ??= new PlayerClimbState(this);
            case PlayerStates.Dash: return _dashState ??= new PlayerDashState(this);
            case PlayerStates.Impact: return new PlayerImpactState(this);
            case PlayerStates.Death: return new PlayerDeathState(this);
            case PlayerStates.Emote: return _emoteState ??= new PlayerEmoteState(this);
            default: throw new ArgumentOutOfRangeException(nameof(state), $"Unhandled state: {state}");
        }
    }

    // Subscriber function to switch to impact state whenever the player get's hit
    private void HandleTakeDamage()
    {
        TransitionStates(PlayerStates.Impact);
    }

    // Subscriber function to switch to death state when the player's health reaches 0
    private void HandleDeath()
    {
        TransitionStates(PlayerStates.Death);
    }
    #endregion

    #region Movement
    /// <summary> Move the player controller in any which way please. Also apply gravity. </summary>
    /// <param name="motion">Motion vector</param>
    /// <param name="deltaTime">Time per frame</param>
    public void Move(Vector3 motion, float deltaTime)
    {
        // Handle Movement
        Controller.Move((motion + ForceReciever.Movement) * deltaTime);
    }

    /// <summary>
    /// Applies gravity and other forces that surround the player without giving them movement
    /// </summary>
    /// <param name="deltaTime">Time per frame</param>
    public void ApplyAmbientForces(float deltaTime)
    {
        Move(Vector3.zero, deltaTime);
    }

    public bool CanClimb()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * ClimbTriggerOffset;
        Vector3 moveDirection = PlayerCharacter.transform.forward;

        if (DebugStateMachine) Debug.DrawRay(rayOrigin, moveDirection, Color.green);

        // If the raycast does not hit an object with a collider; can not climb
        if (!Physics.Raycast(rayOrigin, moveDirection, out RaycastHit raycastHit, ClimbInteractDistance))
            return false;

        // If the player is not facing towards the object; can not climb
        if ((Vector3.Dot(PlayerCharacter.transform.forward, raycastHit.transform.forward)) < _climbInteractionAngle)
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
        PlayerCharacter.rotation = Quaternion.Lerp(PlayerCharacter.rotation, Quaternion.LookRotation(direction), deltaTime * MoveRotationDampValue);
    }
    #endregion

    #region Equippables
    /// <summary>
    /// Equips the abilities of the weapon that is passed into the function.
    /// If the weapon does not fill a ability space then the space will be null
    /// </summary>
    /// <param name="weapon">The weapon to be equipped.</param>
    public void EquipPrimaryWeapon(Weapon weapon)
    {
        // If the weapon passed in was null then equip the unarmed weapon's abilities
        if (weapon == null)
            weapon = UnarmedWeapon;

        PrimaryWeapon = weapon;
        Ability ability = null;

        // Set Combo Attack Primary
        ability = weapon.GetAbility(AbilityType.ComboAttackPrimary);
        SetAbility(ability);

        // Set Power Attack Primary
        ability = weapon.GetAbility(AbilityType.PowerAttackPrimary);
        SetAbility(ability);

        // If there is a secondary weapon equipped then do not replace the secondary abilitites
        if (SecondaryWeapon != null)
            return;

        // Set Combo Attack Secondary
        ability = weapon.GetAbility(AbilityType.ComboAttackSecondary);
        SetAbility(ability);

        // Set Power Attack Secondary
        ability = weapon.GetAbility(AbilityType.PowerAttackSecondary);
        SetAbility(ability);
    }

    /// <summary>
    /// Equips the abilities of the weapon that is passed into the function.
    /// If the weapon does not fill a ability space then the space will be null
    /// </summary>
    /// <param name="weapon">The weapon to be equipped.</param>
    public void EquipSecondaryWeapon(Weapon weapon)
    {
        // If the weapon passed in was null then equip the unarmed weapon's abilities
        if (weapon == null)
            weapon = UnarmedWeapon;

        SecondaryWeapon = weapon;
        Ability ability = null;

        // Set Combo Attack Secondary
        ability = weapon.GetAbility(AbilityType.ComboAttackSecondary);
        SetAbility(ability);

        // Set Power Attack Secondary
        ability = weapon.GetAbility(AbilityType.PowerAttackSecondary);
        SetAbility(ability);
    }

    /// <summary> Sets up an ability and assigns it to a slot.</summary>
    /// <param name="ability">Ability reference</param>
    /// <param name="type">Ability slot</param>
    public void SetAbility(Ability ability)
    {
        if (ability == null)
        {
            Debug.LogWarning("Player State Machine Warning: Ability could not be set; null.");
            return;
        }

        // Reset cooldown to prevent errors
        ability.ResetCooldown();

        switch (ability.Type)
        {
            case AbilityType.ComboAttackPrimary:
                ComboAttackPrimaryAbility = ability;
                if (DebugStateMachine) Debug.Log("Combo Attack Primary Ability set to " + ability);
                break;
            case AbilityType.ComboAttackSecondary:
                ComboAttackSecondaryAbility = ability;
                if (DebugStateMachine) Debug.Log("Combo Attack Secondary Ability set to " + ability);
                break;
            case AbilityType.PowerAttackPrimary:
                PowerAttackPrimaryAbility = ability;
                if (DebugStateMachine) Debug.Log("Power Attack Primary Ability set to " + ability);
                break;
            case AbilityType.PowerAttackSecondary:
                PowerAttackSecondaryAbility = ability;
                if (DebugStateMachine) Debug.Log("Power Attack Secondary Ability set to " + ability);
                break;
            case AbilityType.Dash:
                DashAbility = ability;
                if (DebugStateMachine) Debug.Log("Dash Ability set to " + ability);
                break;
            default:
                Debug.LogError("Player State Machine Error: Ability has invalid type.");
                return;
        }

        OnAbilityChanged?.Invoke(ability);       // Update UI
    }

    public void SheatheWeapons()        // Swap the attach points of the weapons to the sheathe points
    {
        SheathePrimaryWeapon();
        SheatheSecondaryWeapon();
    }

    public void SheathePrimaryWeapon()      // Helper function for SheatheWeapons()
    {
        AttachObject(PrimaryWeapon.gameObject, SwordSheatheAttachPoint);
    }

    public void SheatheSecondaryWeapon()    // Helper function for SheatheWeapons()
    {
        AttachObject(SecondaryWeapon.gameObject, ShieldSheatheAttachPoint);
    }

    public void UnsheatheWeapons()      // Swap the attach points of the weapons to the active points
    {
        UnsheathePrimaryWeapon();
        UnsheatheSecondaryWeapon();
    }

    public void UnsheathePrimaryWeapon()    // Helper function for SheatheWeapons()
    {
        AttachObject(PrimaryWeapon.gameObject, PrimaryWeaponAttachPoint);
    }

    public void UnsheatheSecondaryWeapon()      // Helper function for SheatheWeapons()
    {
        AttachObject(SecondaryWeapon.gameObject, SecondaryWeaponAttachPoint);
    }

    /// <summary>
    /// Attaches/parents an object to a new transform; used for weapon and armor attach points
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="attachpoint"></param>
    private void AttachObject(GameObject obj, Transform attachpoint)
    {
        if (obj == null)
            return;

        obj.transform.parent = attachpoint.transform;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }
    #endregion

    #region Console Commands
    private void RegisterConsoleCommands()
    {
        // Map generator step command - Step the map generator by a desired amount of operations.
        ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "player.automove",
            "Will auto move the player in a certain direction.",
            args =>
            {

                if (args.Length < 1)        // No step amount given; default to stepping 1 operation
                {
                    Debug.LogWarning("No argument provided. Please specify a direction or 'none'.");
                    return;
                }
                string direction = args[0].ToLower().ToString();         // Step amount given and is valid
                
                if (direction == "none")
                {
                    Debug.Log("Player auto-move disabled.");
                    AutoMoveDirection = AutoMoveDirections.None;
                    return;
                }
                else if (direction == "forward")
                {
                    // Move the player forward
                    AutoMoveDirection = AutoMoveDirections.Forward;
                }
                else if (direction == "backward")
                {
                    // Move the player backward
                    AutoMoveDirection = AutoMoveDirections.Backward;
                }
                else if (direction == "left")
                {
                    // Move the player left
                    AutoMoveDirection = AutoMoveDirections.Left;
                }
                else if (direction == "right")
                {
                    // Move the player right
                    AutoMoveDirection = AutoMoveDirections.Right;
                }
                else
                {
                    Debug.LogWarning($"Invalid argument '{args[0]}'. Please specify a direction or 'none'.");
                    return;
                }
                Debug.Log($"Player auto move set to {direction}");
            }, true));
    }

    private void UnregisterConsoleCommands()
    {
        ConsoleUI.CommandRegistry.UnregisterCommand("player.automove");
    }
    #endregion
}