using RyansLibrary.Console;
using RyansLibrary.Input;
using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _cameraSpeed;
    [SerializeField] private float _cameraSensitivity;
    [SerializeField] private bool _invertYLook;
    [SerializeField] private float botCamAngleClamp = 0.75f;      // Clamp the top angle of vertical look
    [SerializeField] private float topCamAngleClamp = -0.75f;     // Clamp the bot angle of vertical look

    [Header("Required References")]
    [SerializeField] private Transform _cameraPivotTransform;

    private bool _cameraToggle = false;

    private void Start()
    {
        RegisterConsoleCommand();
    }

    private void RegisterConsoleCommand()
    {
        ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "tfc",
            "Lists all available commands.",
            args =>
            {
                ToggleCamera();
                Debug.Log($"Console: Free Camera Command");
            }));
    }

    private void ToggleCamera()
    {
        _cameraToggle = !_cameraToggle;

        if (_cameraToggle)
            EnableCamera();
        else
            DisableCamera();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void EnableCamera()
    {
        InputHandler.Instance.ToggleFreeCameraInput(true);
        InputHandler.Instance.TogglePlayerInput(false);
        InputHandler.OnMove += Move;
        InputHandler.OnLook += Look;
    }

    // Update is called once per frame
    private void DisableCamera()
    {
        InputHandler.Instance.ToggleFreeCameraInput(false);
        InputHandler.Instance.TogglePlayerInput(true);
        InputHandler.OnMove -= Move;
        InputHandler.OnLook -= Look;

        // Reset camera back to it's original position
        _cameraPivotTransform.localPosition = Vector3.zero;
        _cameraPivotTransform.localRotation = Quaternion.identity;
        _cameraPivotTransform.localScale = Vector3.one;
    }

    /// <summary>
    /// Move the player on the xy-plane using the WASD keys
    /// </summary>
    private void Move()
    {
        Vector3 movement = new Vector3(InputHandler.Instance.MovementInput.x, 0, InputHandler.Instance.MovementInput.y);
        movement = transform.right * movement.x + transform.forward * movement.y;

        if (movement != Vector3.zero)
        {
            _cameraPivotTransform.position += movement * _cameraSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Look around by rotating the camera pivot 
    /// </summary>
    private void Look()
    {
        Vector2 lookInput = InputHandler.Instance.LookInput;

        if (lookInput == Vector2.zero)
            return;

        // Handle X-Look
        // Rotate the player body and camera simultaniously
        Vector3 horizontalLook = new Vector3(0, lookInput.x * _cameraSensitivity, 0);
        transform.Rotate(horizontalLook);

        // Handle Y-Look
        Vector3 verticalLook;
        if (_invertYLook)                // Handle inverted Y-look if preferred
            verticalLook = new Vector3(-lookInput.y * _cameraSensitivity, 0, 0);
        else
            verticalLook = new Vector3(lookInput.y * _cameraSensitivity, 0, 0);

        // Clamp the Y-look
        if (_cameraPivotTransform.localRotation.x < topCamAngleClamp && verticalLook.x < 0)          // If top bound is reached
            verticalLook = Vector3.zero;
        else if (_cameraPivotTransform.localRotation.x > botCamAngleClamp && verticalLook.x > 0)     // If bot bound is reached
            verticalLook = Vector3.zero;
        else                                        // Rotate the camera vertically explicitly
            _cameraPivotTransform.Rotate(verticalLook);
    }
}
