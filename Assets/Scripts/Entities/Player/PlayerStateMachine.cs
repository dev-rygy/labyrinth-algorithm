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
using UnityEngine.InputSystem.XR.Haptics;

/// <summary>
/// Player Controls Manager that stores references and data for the different states to 
/// use.
/// </summary>
public class PlayerStateMachine : StateMachine
{
    // TODO: Implement static states to save on memory

    [field: Header("References")]
    [field: SerializeField] public Transform PlayerCharacter { get; private set; }

    [field: Header("Movement")]
    [field: SerializeField] public float MovementSpeed { get; private set; } = 5;
    [field: SerializeField] public float GravityMultiplier { get; private set; } = -9.81f;
    [field: SerializeField] public float MoveRotationDampValue { get; private set; }

    [field: Header("Equipped")]
    [field: SerializeField] public Weapon PrimaryWeapon { get; private set; }
    [field: SerializeField] public Weapon SecondaryWeapon { get; private set; }

    // References
    public InputHandler Input { get; private set; } // reference to the input handler
    public CharacterController Controller { get; private set; } // reference to the player's controller
    public Animator Animator { get; private set; }  // reference to the player's animator
    public AnimationTimestamps AnimationTimestamps { get; private set; }

    // Can be used to check if the player is grounded
    public bool IsGrounded() => Controller.isGrounded;

    // Player Abilities
    public Ability ComboAttackPrimary { get; private set; }
    public Ability ComboAttackSecondary { get; private set; }
    public Ability PowerAttackPrimary { get; private set; }
    public Ability PowerAttackSecondary { get; private set; }

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
        AnimationTimestamps = PlayerCharacter.GetComponent<AnimationTimestamps>();      // The timestamp events for the player abilities

        // Transition to the first state
        SwitchState(new PlayerIdleState(this));

        // Equip Weapons
        EquipPrimaryWeapon(PrimaryWeapon);
        EquipSecondaryWeapon(SecondaryWeapon);
    }

    public void EquipPrimaryWeapon(Weapon weapon)
    {
        if (weapon == null)
            return;

        PrimaryWeapon = weapon;
        Ability ability = null;

        // Set Combo Attack Primary
        ability = weapon.GetAbility(AbilityType.ComboAttackPrimary);
        SetAbility(ability, AbilityType.ComboAttackPrimary);

        // Set Power Attack Primary
        ability = weapon.GetAbility(AbilityType.ComboAttackPrimary);
        SetAbility(ability, AbilityType.ComboAttackPrimary);

        // If there is a secondary weapon equipped then do not replace the secondary abilitites
        if (SecondaryWeapon != null)
            return;

        // Set Combo Attack Secondary
        ability = weapon.GetAbility(AbilityType.ComboAttackPrimary);
        SetAbility(ability, AbilityType.ComboAttackPrimary);

        // Set Power Attack Secondary
        ability = weapon.GetAbility(AbilityType.ComboAttackPrimary);
        SetAbility(ability, AbilityType.ComboAttackPrimary);
    }

    public void EquipSecondaryWeapon(Weapon weapon)
    {
        if (weapon == null)
            return;

        SecondaryWeapon = weapon;
        Ability ability = null;

        // Set Combo Attack Secondary
        ability = weapon.GetAbility(AbilityType.ComboAttackPrimary);
        SetAbility(ability, AbilityType.ComboAttackPrimary);

        // Set Power Attack Secondary
        ability = weapon.GetAbility(AbilityType.ComboAttackPrimary);
        SetAbility(ability, AbilityType.ComboAttackPrimary);
    }

    public void SetAbility(Ability ability, AbilityType type)
    {
        switch(type)
        {
            case AbilityType.ComboAttackPrimary:
                ComboAttackPrimary = ability;
                break;
            case AbilityType.ComboAttackSecondary:
                ComboAttackSecondary = ability;
                break;
            case AbilityType.PowerAttackPrimary:
                PowerAttackPrimary = ability;
                break;
            case AbilityType.PowerAttackSecondary:
                PowerAttackSecondary = ability;
                break;
            default:
                return;
        }
    }
}
