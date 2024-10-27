/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2024
 * Last Modified:   10/26/2024 
 * Notes:           Player Controller
*/
using UnityEngine;
using RyansLibrary.Input;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _gravityMultiplier = 3;
    [SerializeField] private float _jumpPower = 1f;

    private CharacterController _characterController;
    private InputManager _inputManager;

    private Vector3 _moveDirection;
    private float _gravity = -9.81f;
    private float _velocity;

    private bool IsGrounded() => _characterController.isGrounded;

    private void Start()
    {
        InputManager.OnJump += Jump;

        _inputManager = InputManager.Instance;
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        ApplyGravity();
        ApplyMovement();
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
    }

    private void ApplyMovement()
    {
        Vector3 input = _inputManager.MovementInput;
        _moveDirection.x = input.x;
        _moveDirection.z = input.y;
    }

    private void Jump()
    {
        if (!IsGrounded())
            return;

        _velocity += _jumpPower;
    }
}
