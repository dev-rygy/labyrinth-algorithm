/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/10/2026
 * Last Modified:   06/10/2026 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using UnityEngine;

public class Elevator : MovingPlatform
{
    [Header("Elevator Settings")]
    [SerializeField] private bool _useRaycast = false;
    [SerializeField] private Vector3 _raycastOffset;
    [SerializeField] private bool _useOrigin = false;
    [SerializeField] private Vector3 _baseTriggerOffset;
    [SerializeField] private Vector3 _endTriggerOffset;
    [Header("Chain Settings")]
    [SerializeField] private GameObject _chainObject;
    [SerializeField] private GameObject _chainParent;
    [SerializeField] private float _spawnChainAtDistInterval = 5f;
    [SerializeField] private float _chainLinkLength = 10f;
    [SerializeField] private Vector3 _chainSpawnStartPoint;
    [Header("Debug")]
    [SerializeField] private bool _debug;

    private float _prevElevatorPositionY;
    private Stack<GameObject> _chainLinkObjects;
    private Vector3 _currentSpawnChainPoint;

    protected override void Start()
    {
        base.Start();

        if (_useRaycast)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + _raycastOffset, Vector3.down, out hit))
            {
                Vector3 adjHit = hit.point + Vector3.up * 0.2f; // Slightly above the hit point to prevent clipping
                _waypoints.Enqueue(adjHit);
                _endRecallWaypoint = adjHit;

                _endRecallTriggerObject.gameObject.transform.position = _endRecallWaypoint + _endTriggerOffset;
                _endRecallTriggerObject.transform.parent = null;
            }
        }
        if (_useOrigin)
        {
            _waypoints.Enqueue(transform.position);
            _baseRecallWaypoint = transform.position;

            _baseRecallTriggerObject.gameObject.transform.position = _baseRecallWaypoint + _baseTriggerOffset;
            _baseRecallTriggerObject.transform.parent = null;
        }

        _chainLinkObjects = new Stack<GameObject>();
        _currentSpawnChainPoint = _chainSpawnStartPoint;
        _prevElevatorPositionY = transform.position.y;
    }

    protected override void Update()
    {
        base.Update();

        // Chain spawning/despawning logic
        if (_chainObject == null || _chainParent == null)
        {
            Debug.LogWarning("No chain object/parent assigned for the elevator.");
            return;
        }

        //SpawnChain();
        //DespawnChain();
    }

    private void SpawnChain()
    {
        // If the elevator has moved down enough to require a new chain link, spawn one
        // Position >= Previous Position of Chain Spawn - Distance to Spawn New Chain Link
        if (transform.position.y <= (_prevElevatorPositionY - _spawnChainAtDistInterval))
        {
            GameObject chainLink = Instantiate(_chainObject, _chainParent.transform);
            chainLink.transform.localPosition = _currentSpawnChainPoint;
            _currentSpawnChainPoint += _chainLinkLength * Vector3.up; // Move the spawn point down for the next link
            
            _prevElevatorPositionY = Mathf.Ceil(transform.position.y);
            _chainLinkObjects.Push(chainLink);
        }
    }

    private void DespawnChain()
    {
        if (_chainLinkObjects.Count > 0 && (transform.localPosition.y > -0.5f))
        {
            GameObject chainLink = _chainLinkObjects.Pop();
            Destroy(chainLink);
        }
        else if (transform.position.y >= (_prevElevatorPositionY + _spawnChainAtDistInterval))
        {
            _prevElevatorPositionY = Mathf.Floor(transform.position.y);
            _currentSpawnChainPoint = _chainSpawnStartPoint;

            GameObject chainLink = _chainLinkObjects.Pop();
            Destroy(chainLink);
        }
    }

    private void OnDrawGizmos()
    {
        if (_useRaycast && _debug)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + _raycastOffset, transform.position + _raycastOffset + Vector3.down * 100f);
        }
    }
}
