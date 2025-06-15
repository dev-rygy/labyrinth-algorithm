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
    [SerializeField] private Transform _cameraOffsetTransform;

    [Header("Debug")]
    [SerializeField] private bool _debug;

    private bool _cameraToggle = false;

    // Initial Camera Values
    private Vector3 _initCameraPivotTransformPos;
    private Quaternion _initCameraPivotTransformRot;
    private Vector3 _initCameraPivotTransformScale;
    private Vector3 _initCameraOffsetTransformPos;
    private Quaternion _initCameraOffsetTransformRot;
    private Vector3 _initCameraOffsetTransformScale;

    private void Start()
    {
        RegisterConsoleCommand();

        // Set Initial Camera Values
        _initCameraPivotTransformPos = _cameraPivotTransform.localPosition;
        _initCameraPivotTransformRot = _cameraPivotTransform.localRotation;
        _initCameraPivotTransformScale = _cameraPivotTransform.localScale;

        _initCameraOffsetTransformPos = _cameraOffsetTransform.localPosition;
        _initCameraOffsetTransformRot = _cameraOffsetTransform.localRotation;
        _initCameraOffsetTransformScale = _cameraOffsetTransform.localScale;
    }

    private void Update()
    {
        if (!_cameraToggle)
            return;

        Move();
        Look();
    }

    private void RegisterConsoleCommand()
    {
        ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "tfc",
            "Toggles Free Cam.",
            args =>
            {
                ToggleCamera();
                ConsoleUI.OnNewConsoleOutput("Free Camera Toggled");
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
        if (_debug) Debug.Log("Free Camera Enabled.");
        InputHandler.Instance.AssignPrevActionMap(InputMap.FreeCam);
    }

    // Update is called once per frame
    private void DisableCamera()
    {
        if (_debug) Debug.Log("Free Camera Disabled.");
        InputHandler.Instance.AssignPrevActionMap(InputMap.Player);

        // Reset camera back to it's original position
        ResetCamera();
    }

    private void ResetCamera()
    {
        _cameraPivotTransform.localPosition = _initCameraPivotTransformPos;
        _cameraPivotTransform.localRotation = _initCameraPivotTransformRot;
        _cameraPivotTransform.localScale = _initCameraPivotTransformScale;

        _cameraOffsetTransform.localPosition = _initCameraOffsetTransformPos;
        _cameraOffsetTransform.localRotation = _initCameraOffsetTransformRot;
        _cameraOffsetTransform.localScale = _initCameraOffsetTransformScale;
    }

    /// <summary>
    /// Move the player on the xy-plane using the WASD keys
    /// </summary>
    private void Move()
    {
        Vector3 movement = new Vector3(InputHandler.Instance.FreeCamMoveInput.x, 0, InputHandler.Instance.FreeCamMoveInput.y);
        movement = _cameraPivotTransform.right * movement.x + _cameraOffsetTransform.forward * movement.z;

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
        Vector2 lookInput = InputHandler.Instance.FreeCamLookInput;

        if (lookInput == Vector2.zero)
            return;

        // Handle X-Look
        // Rotate the player body and camera simultaniously
        Vector3 horizontalLook = new Vector3(0, lookInput.x * _cameraSensitivity * Time.deltaTime, 0);
        _cameraPivotTransform.Rotate(horizontalLook);

        // Handle Y-Look
        Vector3 verticalLook;
        if (!_invertYLook)                // Handle inverted Y-look if preferred
            verticalLook = new Vector3(-lookInput.y * _cameraSensitivity * Time.deltaTime, 0, 0);
        else
            verticalLook = new Vector3(lookInput.y * _cameraSensitivity * Time.deltaTime, 0, 0);

        // Clamp the Y-look
        if (_cameraOffsetTransform.localRotation.x < topCamAngleClamp && verticalLook.x < 0)          // If top bound is reached
            verticalLook = Vector3.zero;
        else if (_cameraOffsetTransform.localRotation.x > botCamAngleClamp && verticalLook.x > 0)     // If bot bound is reached
            verticalLook = Vector3.zero;
        else                                        // Rotate the camera vertically explicitly
            _cameraOffsetTransform.Rotate(verticalLook);
    }
}
