/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/10/2026
 * Last Modified:   06/10/2026 (Ryan)
 * Notes:           
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] protected float _speed = 2f;
    [SerializeField] protected float _waypointPrecision = 0.15f;
    [SerializeField] protected float _waitTimeAtWaypoint = 2f;
    [SerializeField] protected Queue<Vector3> _waypoints;

    [Header("Trigger Settings")]
    [SerializeField] protected bool _needsTrigger = false;
    [SerializeField] protected Trigger _mainTriggerObject;
    [SerializeField] protected Trigger _baseRecallTriggerObject;
    [SerializeField] protected Trigger _endRecallTriggerObject;

    [Header("Debugger")]
    [SerializeField] private bool _debugLogs = false;

    protected bool CheckDistance(Vector3 waypoint) => Vector3.Distance(transform.position, waypoint) <= _waypointPrecision;

    protected Vector3 _baseRecallWaypoint = Vector3.zero;
    protected Vector3 _endRecallWaypoint = Vector3.zero;

    protected bool _isMoving = false;

    private void OnEnable()
    {
        if (_waypoints == null)
            _waypoints = new Queue<Vector3>();
    }

    protected virtual void Start()
    {
        if (_needsTrigger && _mainTriggerObject == null)
        {
            Debug.LogError("Platform is set to need a trigger, but no trigger object is assigned.");
            return;
        }
        if (_mainTriggerObject != null)
        {
            _mainTriggerObject.OnTriggerActivated += MoveToNextWaypoint;
            if (_debugLogs) Debug.Log("Subscribed to trigger events for platform.");
        }

        if (_baseRecallTriggerObject != null)
            _baseRecallTriggerObject.OnTriggerActivated += MoveToBase;

        if (_endRecallTriggerObject != null)
            _endRecallTriggerObject.OnTriggerActivated += MoveToEnd;
    }

    private void OnDisable()
    {
        _mainTriggerObject.OnTriggerActivated -= MoveToNextWaypoint;

        if (_baseRecallTriggerObject != null)
            _baseRecallTriggerObject.OnTriggerActivated -= MoveToBase;

        if (_endRecallTriggerObject != null)
            _endRecallTriggerObject.OnTriggerActivated -= MoveToEnd;
    }

    private void OnDestroy()
    {
        _mainTriggerObject.OnTriggerActivated -= MoveToNextWaypoint;

        if (_baseRecallTriggerObject != null)
            _baseRecallTriggerObject.OnTriggerActivated -= MoveToBase;

        if (_endRecallTriggerObject != null)
            _endRecallTriggerObject.OnTriggerActivated -= MoveToEnd;
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
        if (_debugLogs) Debug.Log("Attempting to move platform.");
        if (_waypoints.Count == 0)
        {
            Debug.LogWarning("No waypoints set for the platform.");
            return;
        }

        if (_isMoving)
        {
            Debug.LogWarning("Platform is already moving.");
            return;
        }

        // Move elevator
        StartCoroutine(MovePlatform());
    }

    protected virtual IEnumerator MovePlatform()
    {
        if (_debugLogs) Debug.Log("Platform moving.");
        _isMoving = true;
        Vector3 currentWaypoint = _waypoints.Dequeue();

        while (!CheckDistance(currentWaypoint))
        {
            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, _speed * Time.deltaTime);

            yield return null;
        }

        yield return new WaitForSeconds(_waitTimeAtWaypoint);

        _isMoving = false;

        // Re-enqueue the waypoint for continuous movement
        _waypoints.Enqueue(currentWaypoint);
        if (_debugLogs) Debug.Log("Platform stopped moving.");
    }

    protected virtual void MoveToEnd()
    {
        if (CheckDistance(_baseRecallWaypoint))
            MoveToNextWaypoint();
    }

    protected virtual void MoveToBase()
    {
        if (CheckDistance(_endRecallWaypoint))
            MoveToNextWaypoint();
    }
}
