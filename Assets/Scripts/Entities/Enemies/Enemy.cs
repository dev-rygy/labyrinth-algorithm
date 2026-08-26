/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/09/2026
 * Last Modified:   08/09/2026 (Ryan)
 *                  This script is simply just controls the animator and movement of a simple enemy. 
 *                  It is not meant to be a full AI system, just eye candy for demo.
*/
using RyansLibrary.Physics;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private bool _canMove = true;
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _pauseIntervalMin = 1f;
    [SerializeField] private float _pauseIntervalMax = 3f;
    [SerializeField] private float _randomDirectionChangeInterval = 2f;
    [SerializeField] private Transform _characterModel;
    [SerializeField] private float _moveRotationDampValue;

    private CharacterController _controller;
    private ForceReceiver _forceReciever;
    private Animator _animator;

    private Vector3 _randomDirection;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _forceReciever = GetComponent<ForceReceiver>();
        _animator = GetComponentInChildren<Animator>();

        StartCoroutine(ChooseRandomDirectionCo());
    }

    private void Update()
    {
        // Apply ambient forces
        ApplyAmbientForces(Time.deltaTime);

        if (_canMove)
        {
            // Move the enemy in the random direction
            Move(_randomDirection * 2f, Time.deltaTime);
        }
    }

    /// <summary> Move the player controller in any which way please. Also apply gravity. </summary>
    /// <param name="motion">Motion vector</param>
    /// <param name="deltaTime">Time per frame</param>
    public void Move(Vector3 motion, float deltaTime)
    {
        // Handle Movement
        _controller.Move((motion * _moveSpeed + _forceReciever.Movement) * deltaTime);

        if (motion != Vector3.zero)
        {
            ApplyCharacterRotation(motion, deltaTime);
        }
    }

    /// <summary>
    /// Applies gravity and other forces that surround the player without giving them movement
    /// </summary>
    /// <param name="deltaTime">Time per frame</param>
    public void ApplyAmbientForces(float deltaTime)
    {
        Move(Vector3.zero, deltaTime);
    }

    private IEnumerator ChooseRandomDirectionCo()
    {
        while (true)
        {
            // If the enemy is not allowed to move, wait until the next frame and check again
            if (!_canMove)
            {
                yield return null;
                continue;
            }

            // Stop moving for a random interval, then choose a new random direction to move in
            _animator.SetBool("IsMoving", false);
            _randomDirection = Vector3.zero;
            yield return new WaitForSeconds(Random.Range(_pauseIntervalMin, _pauseIntervalMax));

            // Start moving in a random direction for a set interval, then choose a new random direction to move in
            _animator.SetBool("IsMoving", true);
            _randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            yield return new WaitForSeconds(_randomDirectionChangeInterval);
        }
    }

    /// <summary> Face the player character towards the direction they are moving </summary>
    /// <param name="direction">The direction the player must face; Best if it is a normalized vector.</param>
    /// <param name="deltaTime">Time per frame</param>
    public void ApplyCharacterRotation(Vector3 direction, float deltaTime)
    {
        // have the player character look in the direction of movement
        // Quaternion.Lerp() changes between two quaternion values based on a delta time
        _characterModel.rotation = Quaternion.Lerp(_characterModel.rotation, Quaternion.LookRotation(direction), deltaTime * _moveRotationDampValue);
    }
}