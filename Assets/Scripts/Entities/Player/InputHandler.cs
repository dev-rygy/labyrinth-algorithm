/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025
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

        // Input Events
        public static event System.Action OnMove;
        // public static event System.Action OnLook;        DEPRICATED
        // public static event System.Action OnLookRight;   DEPRICATED
        // public static event System.Action OnLookLeft;    DEPRICATED 
        public static event System.Action OnJump;
        public static event System.Action OnInteract1;
        public static event System.Action OnInteract2;

        [SerializeField] private bool _debug = false;
        public Vector2 MovementInput { get; private set; }
        // public Vector3 LookInput { get; private set; }   DEPRICATED
        // public bool JumpKeyPressed { get; private set; } DEPRICATED

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
            {
                _playerControls = new PlayerControls();
            }

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
            _playerControls.Player.Jump.started += context => OnJumpInput(context);

            // Player Interaction Events
            _playerControls.Player.Interact1.started += context => OnInteract1Input(context);
            _playerControls.Player.Interact2.started += context => OnInteract2Input(context);
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

        private void TogglePlayerInput(bool toggle)
        {
            if (toggle)
                _playerControls.Player.Enable();
            else
                _playerControls.Player.Disable();
        }

        private void OnMovementInput(InputAction.CallbackContext context)
        {
            // Read value from input and set the movementInput Vector to it
            MovementInput = context.ReadValue<Vector2>();
            OnMove?.Invoke();

            if (_debug) Debug.Log("The Movement Input read was = " + MovementInput);
        }

        /*  Look input (DEPRICATED)
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

        private void OnJumpInput(InputAction.CallbackContext context)
        {
            if (!context.started)
                return;

            OnJump?.Invoke();
        }

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
    }
}
