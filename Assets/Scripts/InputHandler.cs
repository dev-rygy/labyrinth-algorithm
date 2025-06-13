/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025 (Ryan)
 * Notes:           Input Handler
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RyansLibrary.Input
{
    public class InputHandler : MonoBehaviour
    {
        public static InputHandler Instance { get; private set; }

        // Player Input Events
        public static event System.Action OnAny;
        public static event System.Action OnMove;
        // public static event System.Action OnLook;        DEPRICATED
        // public static event System.Action OnLookRight;   DEPRICATED
        // public static event System.Action OnLookLeft;    DEPRICATED
        public static event System.Action OnJump;
        public static event System.Action OnInteract1;
        public static event System.Action OnInteract2;
        public static event System.Action OnComboPrimary;
        public static event System.Action OnComboSecondary;
        public static event System.Action OnPowerPrimary;
        public static event System.Action OnPowerSecondary;
        public static event System.Action OnDash;
        public static event System.Action OnTwoHand;
        public static event System.Action OnSheathe;
        public static event System.Action OnEmote;

        // Console Input Events
        public static event System.Action OnToggleConsole;
        public static event System.Action OnAutoComplete;
        public static event System.Action OnSubmit;
        public static event System.Action OnPageUp;
        public static event System.Action OnPageDown;
        public static event System.Action OnPrevious;
        public static event System.Action OnNext;

        [SerializeField] private bool _debug = false;

        // The player's Unnormalized Input
        public Vector2 MovementInput { get; private set; }
        // The player's Normalized Input
        public Vector2 MoveDirectionNormalized { get; private set; }
        // public Vector3 LookInput { get; private set; }   DEPRICATED
        // public bool JumpKeyPressed { get; private set; } DEPRICATED
        public bool IsHoldingPrimaryCombo { get; private set; }
        public bool IsHoldingSecondaryCombo { get; private set; }
        public bool IsHoldingPrimaryPower { get; private set; }
        public bool IsHoldingSecondaryPower { get; private set; }

        private PlayerControls _playerControls;

        private void Awake()
        {
            // Handle Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize
            if (_playerControls == null)
                _playerControls = new PlayerControls();

            SubscribeToInputEvents();
            TogglePlayerInput(true);
        }

        private void SubscribeToInputEvents()
        {
            // Movement Input Events
            _playerControls.Player.Move.performed += context => OnMovementInput(context);
            _playerControls.Player.Move.canceled += context => OnMovementInput(context);

            // Look Input Events (DEPRICATED)
            // _playerControls.Player.Look.performed += context => OnLookInput(context);
            // _playerControls.Player.Look.canceled += context => OnLookInput(context);
            // _playerControls.Player.LookRight.started += context => OnLookRightInput(context);
            // _playerControls.Player.LookLeft.started += context => OnLookLeftInput(context);

            // Jump Input Events
            _playerControls.Player.Jump.started += _ => OnJump?.Invoke();

            // Player Interaction Events
            _playerControls.Player.Interact1.started += _ => OnInteract1?.Invoke();
            _playerControls.Player.Interact2.started += _ => OnInteract2?.Invoke();

            _playerControls.Player.ComboAttackPrimary.performed += context => OnPrimaryComboInput(context);
            _playerControls.Player.ComboAttackPrimary.canceled += context => OnPrimaryComboInput(context);
            _playerControls.Player.ComboAttackSecondary.performed += context => OnSecondaryComboInput(context);
            _playerControls.Player.ComboAttackSecondary.canceled += context => OnSecondaryComboInput(context);
            _playerControls.Player.PowerAttackPrimary.performed += context => OnPrimaryPowerInput(context);
            _playerControls.Player.PowerAttackPrimary.canceled += context => OnPrimaryPowerInput(context);
            _playerControls.Player.PowerAttackSecondary.performed += context => OnSecondaryPowerInput(context);
            _playerControls.Player.PowerAttackSecondary.canceled += context => OnSecondaryPowerInput(context);

            _playerControls.Player.Dash.performed += _ => OnDash?.Invoke();
            _playerControls.Player.TwoHand.performed += _ => OnTwoHand?.Invoke();
            _playerControls.Player.Sheathe.performed += _ => OnSheathe?.Invoke();
            _playerControls.Player.Emote.performed += _ => OnEmote?.Invoke();

            // The "Any" action; All inputs that can cancel an interaction; EX. Any input cancels Emoting
            _playerControls.Player.Move.performed += _ => OnAny?.Invoke();
            _playerControls.Player.ComboAttackPrimary.performed += _ => OnAny?.Invoke();
            _playerControls.Player.ComboAttackSecondary.performed += _ => OnAny?.Invoke();
            _playerControls.Player.PowerAttackPrimary.performed += _ => OnAny?.Invoke();
            _playerControls.Player.PowerAttackSecondary.performed += _ => OnAny?.Invoke();
            _playerControls.Player.Dash.performed += _ => OnAny?.Invoke();
            _playerControls.Player.TwoHand.performed += _ => OnAny?.Invoke();
            _playerControls.Player.Sheathe.performed += _ => OnAny?.Invoke();

            // Console Input
            _playerControls.Console.ToggleConsole.performed += _ => OnToggleConsole?.Invoke();
            _playerControls.Console.AutoComplete.performed += _ => OnAutoComplete?.Invoke();
            _playerControls.Console.Submit.performed += _ => OnSubmit?.Invoke();
            _playerControls.Console.PageUp.performed += _ => OnPageUp?.Invoke();
            _playerControls.Console.PageDown.performed += _ => OnPageDown?.Invoke();
            _playerControls.Console.Previous.performed += _ => OnPrevious?.Invoke();
            _playerControls.Console.Next.performed += _ => OnNext?.Invoke();
        }

        private void OnEnable()
        {
            TogglePlayerInput(true);
        }

        private void OnDisable()
        {
            TogglePlayerInput(false);
        }

        private void OnDestroy()
        {
            TogglePlayerInput(true);
        }

        #region Player Controls
        // Toggles 'Player' input actions
        private void TogglePlayerInput(bool toggle)
        {
            if (toggle)
                _playerControls.Player.Enable();
            else
                _playerControls.Player.Disable();
        }

        // Toggles 'Console' input actions
        private void ToggleConsoleInput(bool toggle)
        {
            if (toggle)
                _playerControls.Console.Enable();
            else
                _playerControls.Console.Disable();
        }

        private void OnMovementInput(InputAction.CallbackContext context)
        {
            // Read value from input and set the movementInput Vector to it
            MovementInput = context.ReadValue<Vector2>();       // This is an unnormalized Input
            if (context.performed)
                MoveDirectionNormalized = (context.ReadValue<Vector2>()).normalized;

            OnMove?.Invoke();

            if (_debug) Debug.Log("The Movement Input read was = " + MovementInput);
            if (_debug) Debug.Log("The Normalized Movement Input read was = " + MoveDirectionNormalized);
        }

        private void OnPrimaryComboInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnComboPrimary?.Invoke();
                IsHoldingPrimaryCombo = true;
            }
            if (context.canceled)
                IsHoldingPrimaryCombo = false;
        }

        private void OnSecondaryComboInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnComboSecondary?.Invoke();
                IsHoldingSecondaryCombo = true;
            }
            if (context.canceled)
                IsHoldingSecondaryCombo = false;
        }

        private void OnPrimaryPowerInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnPowerPrimary?.Invoke();
                IsHoldingPrimaryPower = true;
            }
            if (context.canceled)
                IsHoldingPrimaryPower = false;
        }

        private void OnSecondaryPowerInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnPowerSecondary?.Invoke();
                IsHoldingSecondaryPower = true;
            }
            if (context.canceled)
                IsHoldingSecondaryPower = false;
        }

        /* Jump Input (DEPRICATED)
        private void OnJumpInput(InputAction.CallbackContext context)
        {
            if (!context.started)
                return;

            OnJump?.Invoke();
        }
        */

        /*  Look Input (DEPRICATED)
        private void OnLookInput(InputAction.CallbackContext context)
        {
            // Read value from input and set the movementInput Vector to it
            LookInput = context.ReadValue<Vector2>();
            OnLook?.Invoke();

            if (_debug) Debug.Log("The Look Input read was = " + LookInput);
        }

        private void OnLookRightInput(InputAction.CallbackContext context)
        {
            if (!context.started)
                return;

            OnLookRight?.Invoke();

            if (_debug) Debug.Log("Player pressed look right button");
        }

        private void OnLookLeftInput(InputAction.CallbackContext context)
        {
            if (!context.started)
                return;

            OnLookLeft?.Invoke();

            if (_debug) Debug.Log("Player pressed look left button");
        }
        */

        /*      Interact Input Functions (DEPRICATED)
        private void OnInteract1Input(InputAction.CallbackContext context)
        {
            if (!context.started)
                return;

            OnInteract1?.Invoke();
            if (_debug) Debug.Log("Player has interacted");
        }

        private void OnInteract2Input(InputAction.CallbackContext context)
        {
            if (!context.started)
                return;

            OnInteract2?.Invoke();
            if (_debug) Debug.Log("Player has interacted");
        }
        */
        #endregion
    }
}
