/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2024
 * Last Modified:   12/17/2024 
 * Notes:           Input Handler
*/

using UnityEngine;
using UnityEngine.InputSystem;

namespace RyansLibrary.Input
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public static event System.Action OnMove;
        public static event System.Action OnJump;
        public static event System.Action OnInteract1;
        public static event System.Action OnInteract2;

        [SerializeField] private bool debug = false;

        public Vector2 MovementInput { get; private set; }
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

            _playerControls.PlayerMovement.Move.performed += context => OnMovementInput(context);
            _playerControls.PlayerMovement.Move.canceled += context => OnMovementInput(context);

            _playerControls.PlayerMovement.Jump.started += context => OnJumpInput(context);


            _playerControls.PlayerMovement.Interact1.started += context => OnInteract1Input(context);
            _playerControls.PlayerMovement.Interact2.started += context => OnInteract2Input(context);
        }

        private void OnEnable()
        {
            _playerControls.PlayerMovement.Enable();
        }

        private void OnDisable()
        {
            _playerControls.PlayerMovement.Disable();
        }

        private void OnMovementInput(InputAction.CallbackContext context)
        {
            // Read value from input and set the movementInput Vector to it
            MovementInput = context.ReadValue<Vector2>();

            OnMove?.Invoke();

            if (debug) Debug.Log("The Movement Input read was = " + MovementInput);
        }

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
