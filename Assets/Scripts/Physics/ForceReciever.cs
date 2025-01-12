using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Returns the movement when the forces are applied to an object. Essetially
/// makes the object have a rigidbody when they are not using Unity's physics system
/// </summary>
public class ForceReciever : MonoBehaviour
{
    [field: SerializeField] public float GravityMultiplier { get; private set; } = -9.81f;
    [field: SerializeField] public bool HasGravity { get; set; } = true;

    [SerializeField] private bool _debug = false;

    public float VelocityX { get; private set; }
    public float VelocityY { get; private set; }
    public float VelocityZ { get; private set; }

    private CharacterController _characterController;
    // private bool _hasGravity;
    private Vector3 _impact;
    private Vector3 dampingVelocity;
    private float drag;

    public Vector3 Movement => _impact + Vector3.up * VelocityY;
    public bool IsGrounded() => _characterController.isGrounded;

    public void Start()
    {
        // if (HasGravity) _hasGravity = true;

        _characterController = GetComponent<CharacterController>();
    }

    public void Update()
    {
        if (_debug) Debug.Log("IsGrounded: " + IsGrounded());

        if (_debug) Debug.Log("ForceReciever Movement: " + Movement);

        // Reduce any forces applied to the player a small bit every second
        _impact = Vector3.SmoothDamp(_impact, Vector3.zero, ref dampingVelocity, drag);

        if (_debug) Debug.Log("ForceReciever Impact: " + _impact);

        if (_debug) Debug.Log("Velocity Y: " + VelocityY);

        // Handle gravity below
        if (!HasGravity)
            return;

        // Conditionally Handle Gravity; IsGrounded is unique to humanoid entities with a CharacterController
        if (_characterController != null)
        {
            if (IsGrounded() && VelocityY < 0.0f)
                VelocityY = 0f;                         // Does not have gravity
            else
                VelocityY += GravityMultiplier * Time.deltaTime;        // Has gravity 
        }
        else
            VelocityY += GravityMultiplier * Time.deltaTime; // Has gravity
    }

    public void AddForce(Vector3 force, float drag = 0.3f)
    {
        this.drag = drag;
        _impact += force;
    }
}
