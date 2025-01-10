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
    [SerializeField] public bool hasGravity;

    public float VelocityX { get; private set; }
    public float VelocityY { get; private set; }
    public float VelocityZ { get; private set; }

    private Vector3 _impact;
    private Vector3 dampingVelocity;
    private float drag;

    public Vector3 Movement => _impact + Vector3.up * VelocityY;

    public void Update()
    {
        if (hasGravity)
            VelocityY += GravityMultiplier * Time.deltaTime;

        // Reduce any forces applied to the player a small bit every second
        _impact = Vector3.SmoothDamp(_impact, Vector3.zero, ref dampingVelocity, drag);
    }

    public void AddForce(Vector3 force, float drag = 0.3f)
    {
        this.drag = drag;
        _impact += force;
    }
}
