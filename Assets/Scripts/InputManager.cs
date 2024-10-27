using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2024
 * Last Modified:   10/26/2024 
 * Notes:           Input Handler
*/
namespace RyansLibrary.Input
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance;

        public static event System.Action OnMove;
        public static event System.Action OnJump;

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
            if (debug) Debug.Log("The Movement Input read was = " + MovementInput);
        }

        private void OnJumpInput(InputAction.CallbackContext context)
        {
            if (!context.started) 
                return;

            OnJump?.Invoke();
        }
    }
}
