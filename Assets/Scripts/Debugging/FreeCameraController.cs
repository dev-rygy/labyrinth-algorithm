using RyansLibrary.Console;
using RyansLibrary.Input;
using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _cameraSpeed;
    [SerializeField] private float _cameraSprintSpeed;
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
    private float _currentCameraSpeed;
    private float _yaw;
    private float _pitch;

    private void OnEnable()
    {
        _currentCameraSpeed = _cameraSpeed;

        InputHandler.OnFreeCamSprint += ToggleSprint;
        EnableCamera();
    }

    private void OnDisable()
    {
        InputHandler.OnFreeCamSprint -= ToggleSprint;
        DisableCamera();
    }

    private void Update()
    {
        if (!_cameraToggle)
            return;

        Move();
        Look();
    }

    public void SetCameraTransform(Transform transform)
    {
        _cameraPivotTransform.position = transform.position;
        _cameraPivotTransform.rotation = transform.rotation;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void EnableCamera()
    {
        _cameraToggle = true;
        InputHandler.Instance.AssignPrevActionMap(InputMap.FreeCam);

        if (_debug) Debug.Log("Free Camera Enabled.");
    }

    // Update is called once per frame
    private void DisableCamera()
    {
        _cameraToggle = false;
        InputHandler.Instance.AssignPrevActionMap(InputMap.Player);

        if (_debug) Debug.Log("Free Camera Disabled.");
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
            _cameraPivotTransform.position += movement * _currentCameraSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Look around by rotating the camera pivot 
    /// </summary>
    private void Look()
    {
        Vector2 look = InputHandler.Instance.FreeCamLookInput;

        if (_invertYLook)
            look.y = -look.y;

        _yaw += look.x * _cameraSensitivity * Time.deltaTime;

        _pitch -= look.y * _cameraSensitivity * Time.deltaTime;

        _pitch = Mathf.Clamp(_pitch, botCamAngleClamp, topCamAngleClamp);

        Quaternion targetYaw =
            Quaternion.Euler(0f, _yaw, 0f);

        Quaternion targetPitch =
            Quaternion.Euler(_pitch, 0f, 0f);

        _cameraPivotTransform.rotation =
            Quaternion.Slerp(
                _cameraPivotTransform.rotation,
                targetYaw,
                15f * Time.deltaTime);

        _cameraOffsetTransform.localRotation =
            Quaternion.Slerp(
                _cameraOffsetTransform.localRotation,
                targetPitch,
                15f * Time.deltaTime);
    }

    private void ToggleSprint(bool toggle)
    {
        if (toggle)
            _currentCameraSpeed = _cameraSprintSpeed;
        else
            _currentCameraSpeed = _cameraSpeed;

        if (_debug) Debug.Log($"Free Camera: Camera Sprint = {toggle}");
    }

}
