/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   02/12/2025 (Ryan)
 * Notes:           Finite State Machine for the Player
 *                  1.) States = { Idle, ComboPrimary, PowerPrimary, ComboSecondary, PowerSecondary, Charge, Cast, Fall, Land, Climb, Dash, DashAttack, Hit, Death, Emote}
 *                  2.) Alphabet = { button_south, button_east, button_west, button_north, left_stick, right_stick, right_shoulder, left_shoulder, right_trigger, 
 *                      left_trigger, interact, take_damage, IsGrounded() = true, IsGrounded() = false, left_stick + right_stick, left_shoulder + right_shoulder, emote }
 *                  3.) Start State = { Idle }
 *                  4.) Final States = { Death }
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RyansLibrary.StateMachine;
using RyansLibrary.Input;

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

/// <summary> Player Controls Manager that stores references and data for the different states to use. </summary>
public class PlayerStateMachine : StateMachine
{
    #region Variables
    public static PlayerStateMachine Instance { get; private set; }     // WARNING: This singleton may need to be removed later for networking reasons with multiple players

    // Static states to save on memory
    private PlayerIdleState _idleState;
    private PlayerComboPrimaryState _comboPrimState;
    private PlayerPowerPrimaryState _powerPrimState;
    private PlayerComboSecondaryState _comboSecState;
    private PlayerPowerSecondaryState _powerSecState;
    private PlayerClimbState _climbState;
    private PlayerFallState _fallState;
    // TODO: Implement the rest of the states below
    // private static PlayerChargeState _chargeState;
    // private static PlayerCastState _castState;
    // private static PlayerDashState _dashState;
    private PlayerEmoteState _emoteState;
    

    [field: Header("Required References")]
    [field: SerializeField] public Transform PlayerCharacter { get; private set; }      // The player model with the bones
    [field: SerializeField] public Weapon UnarmedWeapon { get; private set; }       // The abilities the player will have when they do not have a weapon equipped 
    [field: SerializeField] public AbilityUI AbilityUI { get; private set; }        // TODO: remove this reference, this is BAD practice!

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

    [field: Header("Attach Points")]                                                                // Attach points for objects that move with the player's rig
    [field: Tooltip("Primary weapon parent transform when equipped")]
    [field: SerializeField] public Transform PrimaryWeaponAttachPoint { get; private set; }
    [field: Tooltip("Secondary weapon parent transform when equipped")]
    [field: SerializeField] public Transform SecondaryWeaponAttachPoint { get; private set; }
    [field: Tooltip("Sword parent transform when sheathed")]
    [field: SerializeField] public Transform SwordSheatheAttachPoint { get; private set; }
    [field: Tooltip("Shield parent transform when sheathed")]
    [field: SerializeField] public Transform ShieldSheatheAttachPoint { get; private set; }

    // References Assigned In Code
    public InputHandler Input { get; private set; } // reference to the input handler
    public CharacterController Controller { get; private set; } // reference to the player's controller
    public Animator Animator { get; private set; }  // reference to the player's animator
    public AnimationTimestamps AnimationTimestamps { get; private set; }        // reference to player animator events
    public ForceReciever ForceReciever { get; private set; }        // reference to player physics
    public EntityHealth Health { get; private set; }        // Reference to player health

    // Player Abilities Assigned In Code
    public Ability ComboAttackPrimary { get; private set; }
    public Ability ComboAttackSecondary { get; private set; }
    public Ability PowerAttackPrimary { get; private set; }
    public Ability PowerAttackSecondary { get; private set; }
    #endregion

    #region Mono
    private void Awake()
    {
        // Handle singleton
        if (Instance && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        // Assign references on object
        Controller = GetComponent<CharacterController>();           // The player must have a character controller
        Animator = PlayerCharacter.GetComponent<Animator>();        // The animator is on the "Player Character" child object
        AnimationTimestamps = PlayerCharacter.GetComponent<AnimationTimestamps>();      // The timestamp events for the player abilities
        ForceReciever = GetComponent<ForceReciever>();              // The player must have a force reciever to interact with gravity
        Health = GetComponent<EntityHealth>();                      // Player's health behavior is shared with all entities
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

        // Input Singleton
        Input = InputHandler.Instance;                              // An input handler must be in the player's scene

        // Kick off the player's state machine
        // Transition to the first state
        TransitionStates(PlayerStates.Idle);

        // Equip Primary and Secondary Weapons off rip if assigned in the inspector
        EquipPrimaryWeapon(PrimaryWeapon);
        EquipSecondaryWeapon(SecondaryWeapon);  
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

        // Unsubscribe to State Events
        Health.OnTakeDamage -= HandleTakeDamage;
        Health.OnDeath -= HandleDeath;
    }
    #endregion

    #region State Machine Helpers
    /// <summary>
    /// Overloaded transition function for better readability and better handling of static states.
    /// </summary>
    /// <param name="state">The state to transition to.</param>
    public void TransitionStates(PlayerStates state)
    {
        switch (state)
        {
            case PlayerStates.Idle:
                if (_idleState == null)
                    _idleState = new PlayerIdleState(this);
                TransitionStates(_idleState);
                break;
            case PlayerStates.ComboPrim:
                if (_comboPrimState == null)
                    _comboPrimState = new PlayerComboPrimaryState(this);
                TransitionStates(_comboPrimState);
                break;
            case PlayerStates.PowerPrim:
                if (_powerPrimState == null)
                    _powerPrimState = new PlayerPowerPrimaryState(this);
                TransitionStates(_powerPrimState);
                break;
            case PlayerStates.ComboSec:
                if (_comboSecState == null)
                    _comboSecState = new PlayerComboSecondaryState(this);
                TransitionStates(_comboSecState);
                break;
            case PlayerStates.PowerSec:
                if (_powerSecState == null)
                    _powerSecState = new PlayerPowerSecondaryState(this);
                TransitionStates(_powerSecState);
                break;
            case PlayerStates.Charge:
                // TODO: Implement
                break;
            case PlayerStates.Cast:
                // TODO: Implement
                break;
            case PlayerStates.Fall:
                if (_fallState == null)
                    _fallState = new PlayerFallState(this);
                TransitionStates(_fallState);
                break;
            case PlayerStates.Climb:
                if (_climbState == null)
                    _climbState = new PlayerClimbState(this);
                TransitionStates(_climbState);
                break;
            case PlayerStates.Dash:
                // TODO: Implement
                break;
            case PlayerStates.Impact:
                TransitionStates(new PlayerImpactState(this));
                break;
            case PlayerStates.Death:
                TransitionStates(new PlayerDeathState(this));
                break;
            case PlayerStates.Emote:
                if (_emoteState == null)
                    _emoteState = new PlayerEmoteState(this);
                TransitionStates(_emoteState);
                break;
            default:
                Debug.LogError("Player State Machine Error: State Call " + state + " does not exist.");
                break;
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

        SecondaryWeapon = weapon;
        Ability ability = null;

        // Set Combo Attack Secondary
        ability = weapon.GetAbility(AbilityType.ComboAttackSecondary, this);
        SetAbility(ability, AbilityType.ComboAttackSecondary);

        // Set Power Attack Secondary
        ability = weapon.GetAbility(AbilityType.PowerAttackSecondary, this);
        SetAbility(ability, AbilityType.PowerAttackSecondary);
    }

    /// <summary> Sets up an ability and assigns it to a slot.</summary>
    /// <param name="ability">Ability reference</param>
    /// <param name="type">Ability slot</param>
    public void SetAbility(Ability ability, AbilityType type)
    {
        switch (type)
        {
            case AbilityType.ComboAttackPrimary:
                ComboAttackPrimary = ability;
                AbilityUI.AssignPrimaryComboAbility(ability);
                if (DebugStateMachine) Debug.Log("Combo Attack Primary set to " + ability);
                break;
            case AbilityType.ComboAttackSecondary:
                ComboAttackSecondary = ability;
                AbilityUI.AssignSecondaryComboAbility(ability);
                if (DebugStateMachine) Debug.Log("Combo Attack Secondary set to " + ability);
                break;
            case AbilityType.PowerAttackPrimary:
                PowerAttackPrimary = ability;
                AbilityUI.AssignPrimaryPowerAbility(ability);
                if (DebugStateMachine) Debug.Log("Power Attack Primary set to " + ability);
                break;
            case AbilityType.PowerAttackSecondary:
                PowerAttackSecondary = ability;
                AbilityUI.AssignSecondaryPowerAbility(ability);
                if (DebugStateMachine) Debug.Log("Power Attack Secondary set to " + ability);
                break;
            default:
                return;
        }
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
}
