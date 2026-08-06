/*
 * Created By:      Ryan Carpenter
 * Date Created:    12/26/2024
 * Last Modified:   12/26/2024 
 * Notes:           Spawn Pad Used for spawning a variety of different objects 
 *                      and entities.
*/
using RyansLibrary;
using System.Collections.Generic;
using UnityEngine;

public enum PadType
{
    chest,
    enemy
}

public class SpawnPad : MonoBehaviour
{

    // What type of object the pad will spawn
    [SerializeField] public PadType Type;

    // The object it is to spawn
    [SerializeField] private List<ProbabilityEntry<GameObject>> _spawnEntries;

    [Header("Debug")]
    [SerializeField] private bool _debugGizmos = false;
    [SerializeField] private Vector3 _gizmoArea;
    [SerializeField] private Vector3 _gizmoOffset; 
    [SerializeField] private Color _gizmoColor;

    private void Start()
    {
        _spawnEntries = new List<ProbabilityEntry<GameObject>>();
    }

    /// <summary>
    /// Spawn the object from the inspector. If no object is in the inspector then do not spawn anything.
    /// </summary>
    public void SpawnObject()
    {
        if (_spawnEntries.Count <= 0)
        {
            Debug.LogWarning("Spawn Pad has no valid object to spawn.");
            return;
        }

        ProbabilityEntry<GameObject> choosenObject = Probability<GameObject>.ChooseRandomFromWeights(_spawnEntries);

        Instantiate(choosenObject.Object, transform.position, Quaternion.identity, transform);
    }

    /// <summary>
    /// Spawn the object from the inspector. If no object is in the inspector then do not spawn anything.
    /// </summary>
    /// <param name="spawnObject">The object to spawn.</param>
    public void SpawnObject(GameObject spawnObject)
    {
        if (spawnObject == null)
        {
            Debug.LogWarning("Spawn Pad was passed in an object of type null.");
            return;
        }

        Instantiate(spawnObject, transform.position, Quaternion.identity, transform);
    }

    private void OnDrawGizmos()
    {
        if (!_debugGizmos)
            return;

        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireCube(transform.position + _gizmoOffset, _gizmoArea);
    }
}
