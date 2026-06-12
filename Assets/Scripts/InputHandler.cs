/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   06/12/2025 (Ryan)
 * Notes:           Input Handler
*/
using UnityEngine;
using UnityEngine.InputSystem;

public enum InputMap
{
    Player,
    FreeCam,
    Console
}

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
        public static event System.Action OnConsoleOpen;

        // Free Camera Input Events
        public static event System.Action OnFreeCamMove;
        public static event System.Action OnFreeCamLook;
        public static event System.Action<bool> OnFreeCamSprint;

        // Console Input Events
        public static event System.Action OnConsoleClose;
        public static event System.Action OnAutoComplete;
        public static event System.Action OnSubmit;
        public static event System.Action OnPageUp;
        public static event System.Action OnPageDown;
        public static event System.Action OnPrevious;
        public static event System.Action OnNext;

        [SerializeField] private bool _toggleConsole = true;
        [SerializeField] private bool _debug = false;

        // The player's Unnormalized Input
        public Vector2 MovementInput { get; private set; }
        // The player's Normalized Input
        public Vector2 MoveDirectionNormalized { get; private set; }
        // public Vector2 LookInput { get; private set; }   DEPRICATED
        // public bool JumpKeyPressed { get; private set; } DEPRICATED
        public bool IsHoldingPrimaryCombo { get; private set; }
        public bool IsHoldingSecondaryCombo { get; private set; }
        public bool IsHoldingPrimaryPower { get; private set; }
        public bool IsHoldingSecondaryPower { get; private set; }

        public Vector2 FreeCamMoveInput { get; private set; }
        public Vector2 FreeCamMoveInputNormalized { get; private set; }
        public Vector2 FreeCamLookInput { get; private set; }
        private PlayerControls _playerControls;

        private InputActionMap _playerInputActionMap;
        private InputActionMap _consoleInputActionMap;
        private InputActionMap _freeCamInputActionMap;

        private InputActionMap _activeActionMap;
        private InputActionMap _prevActionMap;

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

            // Assign input maps
            _playerInputActionMap = _playerControls.Player;
            _consoleInputActionMap = _playerControls.Console;
            _freeCamInputActionMap = _playerControls.FreeCamera;

            SubscribeToPlayerEvents();
            SubscribeToFreeCameraEvents();
            SubscribeToConsoleEvents();

            TogglePlayerInput(true);
            ToggleConsoleInput(false);
            ToggleUIInput(true);
        }

        private void SubscribeToPlayerEvents()
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

            // Open Console
            _playerControls.Player.OpenConsole.performed += _ => OnConsoleOpenInput();
        }

        private void SubscribeToFreeCameraEvents()
        {
            // Free Camera Events
            _playerControls.FreeCamera.Move.performed += context => OnFreeCamMovementInput(context);
            _playerControls.FreeCamera.Move.canceled += context => OnFreeCamMovementInput(context);

            _playerControls.FreeCamera.Look.performed += context => OnFreeCamLookInput(context);
            _playerControls.FreeCamera.Look.canceled += context => OnFreeCamLookInput(context);

            _playerControls.FreeCamera.Sprint.performed += _ => OnFreeCamSprint(true);
            _playerControls.FreeCamera.Sprint.canceled += _ => OnFreeCamSprint(false);

            _playerControls.FreeCamera.OpenConsole.performed += _ => OnConsoleOpenInput();
        }

        private void SubscribeToConsoleEvents()
        {
            if (!_toggleConsole)
                return;

            // Console Input
            _playerControls.Console.CloseConsole.performed += _ => OnConsoleCloseInput();
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
            ToggleConsoleInput(false);
            ToggleFreeCameraInput(false);
            ToggleUIInput(true);

            _activeActionMap = _playerInputActionMap;
            _prevActionMap = null;
        }

        private void OnDisable()
        {
            TogglePlayerInput(false);
            ToggleConsoleInput(false);
            ToggleFreeCameraInput(false);
            ToggleUIInput(true);

            _activeActionMap = null;
            _prevActionMap = null;
        }

        public void UnsubscribePlayerInputEvents()
        {
            OnAny = null;
            OnMove = null;
            // OnLook = null;
            // OnLookRight = null;
            // OnLookLeft = null;
            OnJump = null;
            OnInteract1 = null;
            OnInteract2 = null;
            OnComboPrimary = null;
            OnComboSecondary = null;
            OnPowerPrimary = null;
            OnPowerSecondary = null;
            OnDash = null;
            OnTwoHand = null;
            OnSheathe = null;
            OnEmote = null;
        }

        #region Player Controls
        public void SwitchActionMap(InputMap map)
        {
            _prevActionMap = _activeActionMap;

            _prevActionMap.Disable();       // Disable previous action map

            // Search for desired action map
            _activeActionMap = GetActionMap(map);

            _activeActionMap.Enable();      // Enable new action map
        }

        private void SwitchActionMap(InputActionMap map)
        {
            _prevActionMap = _activeActionMap;      // Store previous map

            _prevActionMap.Disable();       // Disable previous action map

            _activeActionMap = map;     // Store new map

            _activeActionMap.Enable();      // Enable new action map
        }

        public void AssignPrevActionMap(InputMap map)
        {
            // Search for desired action map
            _prevActionMap = GetActionMap(map);
        }

        // Switch Input Action Map
        private InputActionMap GetActionMap(InputMap map)
        {
            switch (map)
            {
                case InputMap.Player:
                    if (_debug) Debug.Log("Switched to Player Map");
                    return _playerInputActionMap;
                case InputMap.Console:
                    if (_debug) Debug.Log("Switched to Console Map");
                    return _consoleInputActionMap;
                case InputMap.FreeCam:
                    if (_debug) Debug.Log("Switched to FreeCamera Map");
                    return _freeCamInputActionMap;
                default:
                    Debug.LogError("Invalid Input Map");
                    return null;
            }
        }

        // Toggles 'Player' input actions
        public void TogglePlayerInput(bool toggle)
        {
            if (toggle)
                _playerControls.Player.Enable();
            else
                _playerControls.Player.Disable();
        }

        // Toggles 'Console' input actions
        public void ToggleConsoleInput(bool toggle)
        {
            if (toggle)
                _playerControls.Console.Enable();
            else
                _playerControls.Console.Disable();
        }

        // Toggles 'Free Camera' input actions
        public void ToggleFreeCameraInput(bool toggle)
        {
            if (toggle)
                _playerControls.FreeCamera.Enable();
            else
                _playerControls.FreeCamera.Disable();
        }

        // Toggles 'UI' input actions
        public void ToggleUIInput(bool toggle)
        {
            if (toggle)
                _playerControls.UI.Enable();
            else 
                _playerControls.UI.Disable();
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

        private void OnFreeCamMovementInput(InputAction.CallbackContext context)
        {
            // Read value from input and set the movementInput Vector to it
            FreeCamMoveInput = context.ReadValue<Vector2>();       // This is an unnormalized Input
            if (context.performed)
                FreeCamMoveInputNormalized = (context.ReadValue<Vector2>()).normalized;

            OnFreeCamMove?.Invoke();

            if (_debug) Debug.Log("The Free Cam Movement Input read was = " + FreeCamMoveInput);
            if (_debug) Debug.Log("The Free Cam Normalized Movement Input read was = " + FreeCamMoveInputNormalized);
        }

        private void OnFreeCamLookInput(InputAction.CallbackContext context)
        {
            // Read value from input and set the movementInput Vector to it
            FreeCamLookInput = context.ReadValue<Vector2>();
            OnFreeCamLook?.Invoke();

            if (_debug) Debug.Log("The Free Cam Look Input read was = " + FreeCamLookInput);
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

        private void OnConsoleOpenInput()
        {
            // Toggle Action Map
            SwitchActionMap(_consoleInputActionMap);

            // Open Console
            OnConsoleOpen?.Invoke();
        }

        private void OnConsoleCloseInput()
        {
            // Toggle Action Map
            SwitchActionMap(_prevActionMap);

            // Close Console
            OnConsoleClose?.Invoke();
        }

        /* Jump Input (DEPRICATED)
        private void OnJumpInput(InputAction.CallbackContext context)
        {
            if (!context.started)
                return;

            OnJump?.Invoke();
        }

        private void OnLookInput(InputAction.CallbackContext context)
        {
            // Read value from input and set the movementInput Vector to it
            LookInput = context.ReadValue<Vector2>();
            OnLook?.Invoke();

            if (_debug) Debug.Log("The Look Input read was = " + LookInput);
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
