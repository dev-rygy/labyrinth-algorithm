/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/10/2026
 * Last Modified:   06/10/2026 (Ryan)
 * Notes:           
*/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public static event Action OnTriggerActivated;

    [Header("General Settings")]
    [SerializeField] protected float _speed = 2f;
    [SerializeField] protected float _waypointPrecision = 0.1f;
    [SerializeField] protected float _waitTimeAtWaypoint = 2f;
    [SerializeField] protected Queue<Vector3> _waypoints;

    [Header("Trigger Settings")]
    [SerializeField] protected bool _needsTrigger = false;
    [SerializeField] protected Trigger MainTriggerObject;
    
    protected bool _isMoving = false;

    private void OnEnable()
    {
        if (_waypoints == null)
            _waypoints = new Queue<Vector3>();
    }

    protected virtual void Start()
    {
        if (_needsTrigger && MainTriggerObject == null)
        {
            Debug.LogError("Elevator is set to need a trigger, but no trigger object is assigned.");
            return;
        }
    }

    protected virtual void Update()
    {
        if (!_needsTrigger && !_isMoving)
        {
            MoveToNextWaypoint();
        }
    }

    protected virtual void MoveToNextWaypoint()
    {
        if (_waypoints.Count == 0)
        {
            Debug.LogWarning("No waypoints set for the elevator.");
            return;
        }

        if (_isMoving)
        {
            Debug.LogWarning("Elevator is already moving.");
            return;
        }

        // Move elevator
        StartCoroutine(MovePlatform());
    }

    protected virtual IEnumerator MovePlatform()
    {
        _isMoving = true;
        Vector3 currentWaypoint = _waypoints.Dequeue();

        while (Vector3.Distance(transform.position, currentWaypoint) > _waypointPrecision)
        {
            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, _speed * Time.deltaTime);

            yield return null;
        }

        yield return new WaitForSeconds(_waitTimeAtWaypoint);

        _isMoving = false;

        // Re-enqueue the waypoint for continuous movement
        _waypoints.Enqueue(currentWaypoint);
    }


}
