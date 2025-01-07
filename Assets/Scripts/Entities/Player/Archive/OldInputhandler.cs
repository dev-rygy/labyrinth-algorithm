/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2024
 * Last Modified:   01/06/2024 
 * Notes:           Old Input Handler (DEPRICATED)
*/

using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RyansLibrary.Input
{
    public class OldInputhandler : MonoBehaviour
    {
        public static OldInputhandler Instance { get; private set; }

        // Input Events
        public static event System.Action OnMove;
        public static event System.Action OnLook;
        public static event System.Action OnLookRight;
        public static event System.Action OnLookLeft;
        public static event System.Action OnJump;
        public static event System.Action OnInteract1;
        public static event System.Action OnInteract2;

        [SerializeField] private bool debug = false;

        public Vector2 MovementInput { get; private set; }
        public Vector3 LookInput { get; private set; }
        public bool JumpKeyPressed { get; private set; }

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

            // Control handling
            if (_playerControls == null)
            {
                _playerControls = new PlayerControls();
            }

            _playerControls.Player.Move.performed += context => OnMovementInput(context);
            _playerControls.Player.Move.canceled += context => OnMovementInput(context);

            _playerControls.Player.Look.performed += context => OnLookInput(context);
            _playerControls.Player.Look.canceled += context => OnLookInput(context);
            //_playerControls.Player.LookRight.started += context => OnLookRightInput(context);
            //_playerControls.Player.LookLeft.started += context => OnLookLeftInput(context);

            _playerControls.Player.Jump.started += context => OnJumpInput(context);


            _playerControls.Player.Interact1.started += context => OnInteract1Input(context);
            _playerControls.Player.Interact2.started += context => OnInteract2Input(context);
        }

        private void OnEnable()
        {
            _playerControls.Player.Enable();
        }

        private void OnDisable()
        {
            _playerControls.Player.Disable();
        }

        private void OnMovementInput(InputAction.CallbackContext context)
        {
            // Read value from input and set the movementInput Vector to it
            MovementInput = context.ReadValue<Vector2>();

            OnMove?.Invoke();

            if (debug) Debug.Log("The Movement Input read was = " + MovementInput);
        }

        private void OnLookInput(InputAction.CallbackContext context)
        {
            // Read value from input and set the movementInput Vector to it
            LookInput = context.ReadValue<Vector2>();

            OnLook?.Invoke();

            if (debug) Debug.Log("The Look Input read was = " + LookInput);
        }

        /*
        private void OnLookRightInput(InputAction.CallbackContext context)
        {
            if (!context.started)
                return;

            OnLookRight?.Invoke();

            if (debug) Debug.Log("Player pressed look right button");
        }

        private void OnLookLeftInput(InputAction.CallbackContext context)
        {
            if (!context.started)
                return;

            OnLookLeft?.Invoke();

            if (debug) Debug.Log("Player pressed look left button");
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
            if (debug) Debug.Log("Player has interacted");
        }

        private void OnInteract2Input(InputAction.CallbackContext context)
        {
            if (!context.started)
                return;

            OnInteract2?.Invoke();
            if (debug) Debug.Log("Player has interacted");
        }
    }
}
