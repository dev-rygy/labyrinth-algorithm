/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/22/2025 (Ryan)
 * Notes:           Applies forces to an object
*/

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;

/// <summary>
/// Returns the movement when the forces are applied to an object. Essetially
/// makes the object have a rigidbody when they are not using Unity's physics system
/// </summary>
public class ForceReciever : MonoBehaviour
{
    [field: Header("Falling and Grounded")]
    [field: SerializeField] public float GravityMultiplier { get; private set; } = -9.81f;
    [field: SerializeField] public float StaticVerticalVelocity { get; private set; } = -2f;
    [field: SerializeField] public bool HasGravity { get; set; } = true;
    [field: SerializeField] public bool EnableGroundCheck = true;

    [SerializeField] private float _groundRayFanAngleX = 45;
    [SerializeField] private float _groundRayCount = 5;
    [SerializeField] private float _groundFanCheckDistance = 0.1f;
    [SerializeField] private float _groundDownCheckDistance = 0.025f;
    [SerializeField] private float _terminalVelocity = -50f;       // Highest velocity the player can reach

    [SerializeField] private bool _debug;
    [SerializeField] private bool _log = false;

    // Individual velocities of the object
    public float VelocityX { get; private set; }
    public float VelocityY { get; private set; }
    public float VelocityZ { get; private set; }

    private Vector3 _impact;
    private Vector3 dampingVelocity;
    private float drag;

    public Vector3 Movement => _impact + Vector3.up * VelocityY;
    //public bool IsGrounded() => _characterController.isGrounded;  // Character controllers ground check

    public void Update()
    {
        if (_log) Debug.Log("IsGrounded: " + IsGrounded());

        if (_log) Debug.Log("ForceReciever Movement: " + Movement);

        // Reduce any forces applied to the player a small bit every second
        _impact = Vector3.SmoothDamp(_impact, Vector3.zero, ref dampingVelocity, drag);

        if (_log) Debug.Log("ForceReciever Impact: " + _impact);

        if (_log) Debug.Log("Velocity Y: " + VelocityY);

        // Handle gravity below
        if (!HasGravity)
        {
            VelocityY = 0;
            return;
        }

        // Conditionally Handle Gravity; IsGrounded is unique to humanoid entities with a CharacterController
        if (IsGrounded() && VelocityY < 0.0f)
            VelocityY = StaticVerticalVelocity;                         // Does not have gravity
        else
        {
            if (VelocityY > -(_terminalVelocity))   // If terminal velocity has not been reached
                VelocityY += GravityMultiplier * Time.deltaTime;        // Has gravity
            else
                if (_log) Debug.Log("Terminal Velocity Reached");
        }
    }

    public void AddForce(Vector3 force, float drag = 0.3f)
    {
        this.drag = drag;
        _impact += force;
    }
    
    public bool IsGrounded()
    {
        if (!EnableGroundCheck) return false;

        Vector3 rayOrigin = transform.position;
        RaycastHit hit;

        if (_debug) Debug.DrawRay(rayOrigin, Vector3.down * _groundDownCheckDistance, Color.red);
        // Check the first raycast
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, _groundDownCheckDistance))
            return true;

        float groundRayFanAngleY = 0;
        float segmentedFanAngle = 360 / _groundRayCount;

        // Check all fan raycasts 
        for (int i = 0; i < _groundRayCount; i++)
        {
            Vector3 rayDirection = Quaternion.Euler(_groundRayFanAngleX, groundRayFanAngleY, 0) * Vector3.down;
            rayDirection.Normalize();

            if (_debug) Debug.DrawRay(rayOrigin, rayDirection * _groundFanCheckDistance, Color.red);

            if (Physics.Raycast(rayOrigin, rayDirection, out hit, _groundFanCheckDistance))
                return true;

            groundRayFanAngleY += segmentedFanAngle;
        }
        
        return false;
    }
}
