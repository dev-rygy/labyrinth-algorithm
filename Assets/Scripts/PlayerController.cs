/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2024
 * Last Modified:   12/18/2024 
 * Notes:           Player Controller
*/
using UnityEngine;
using RyansLibrary.Input;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Values")]
    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _lookAngleIncrement = 90f;
    [SerializeField] private float _gravityMultiplier = 3;
    [SerializeField] private float _jumpPower = 1f;

    [Header("Player Model")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _playerCharacter;
    private CharacterController _characterController;
    private InputHandler _inputManager;

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
        InputHandler.OnJump += Jump;
        InputHandler.OnInteract1 += Interact;
        InputHandler.OnLookRight += () => transform.Rotate(Vector3.down, _lookAngleIncrement);
        InputHandler.OnLookLeft += () => transform.Rotate(Vector3.down, -(_lookAngleIncrement));


        _inputManager = InputHandler.Instance;
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        ApplyGravity();
        ApplyMovement();
        ApplyLook();
        Move();
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

        if (_moveDirection.x != 0f || _moveDirection.z != 0f)
            _animator.SetBool("isRunning", true);
        else
            _animator.SetBool("isRunning", false);
    }

    private void ApplyMovement()
    {
        Vector3 input = new Vector3(_inputManager.MovementInput.x, 0, _inputManager.MovementInput.y);
        
        // Transform input to be relative to the camera's orientation
        input = transform.TransformDirection(input);
        input.y = 0;        // Ensure that the movement is strictly horizontal

        _moveDirection.x = input.x;
        _moveDirection.z = input.z;
    }

    private void ApplyLook()
    {
        // Ensure there is some horizontal movement when changing the direction of the model
        if (_moveDirection.x != 0 || _moveDirection.z != 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(_moveDirection.x, 0, _moveDirection.z));
            _playerCharacter.rotation = targetRotation;
        }
    }

    private void Jump()
    {
        if (!IsGrounded())
            return;

        _velocity += _jumpPower;
    }

    private void Interact()
    {
        // TODO: Interaction Logic
    }
}
