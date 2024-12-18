/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2024
 * Last Modified:   12/17/2024 
 * Notes:           Player Controller
*/
using UnityEngine;
using RyansLibrary.Input;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Values")]
    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _lookSpeed = 1f;
    [SerializeField] private float _gravityMultiplier = 3;
    [SerializeField] private float _jumpPower = 1f;

    [Header("References")]
    [SerializeField] private GameObject _cameraPivot;

    private CharacterController _characterController;
    private InputManager _inputManager;

    private Vector3 _lookDirection;
    private Vector3 _moveDirection;
    private float _gravity = -9.81f;
    private float _velocity;

    [SerializeField] private bool debug = false;

    private bool IsGrounded() => _characterController.isGrounded;

    private void Awake()
    {
        // Handle Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InputManager.OnJump += Jump;
        InputManager.OnInteract1 += Interact;
        InputManager.OnLookRight += LookRight;
        InputManager.OnLookLeft += LookLeft;


        _inputManager = InputManager.Instance;
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        ApplyGravity();
        ApplyMovement();
        //ApplyLook();
        Move();
        //Look();
    }
    
    private void ApplyGravity()
    {
        if (IsGrounded() && _velocity < 0.0f)
            _velocity = -1f;
        else
        {
            _velocity += _gravity * _gravityMultiplier * Time.deltaTime;
        }
        _moveDirection.y = _velocity;
    }

    private void Move()
    {
        // Move Player
        _characterController.Move(_moveDirection * _speed * Time.deltaTime);
    }

    private void Look()
    {
        if (_cameraPivot == null)
        {
            Debug.LogError("Player Camera pivot is not assigned.");
            return;
        }

        // Change the perspective of the camera
        //float angle = _lookDirection.x * _lookSpeed * Time.deltaTime;
        float angle = 90 * _lookDirection.x;
        _cameraPivot.transform.Rotate(Vector3.up, angle);

        if (debug) Debug.Log("Look angle change = " + angle);
    }

    private void LookRight()
    {
        float angle = 90;
        _cameraPivot.transform.Rotate(Vector3.down, angle);
    }

    private void LookLeft()
    {
        float angle = -90;
        _cameraPivot.transform.Rotate(Vector3.down, angle);
    }

    private void ApplyMovement()
    {
        Vector3 input = new Vector3(_inputManager.MovementInput.x, 0, _inputManager.MovementInput.y);
        
        // Transform input to be relative to the camera's orientation
        input = _cameraPivot.transform.TransformDirection(input);
        input.y = 0;        // Ensure that the movement is strictly horizontal

        _moveDirection.x = input.x;
        _moveDirection.z = input.z;
    }

    private void ApplyLook()
    {
        Vector3 input = _inputManager.LookInput;
        _lookDirection.x = input.x;
        _lookDirection.y = input.y;
    }

    private void Jump()
    {
        if (!IsGrounded())
            return;

        _velocity += _jumpPower;
    }

    private void Interact()
    {

    }
}
