/*
 * Created By:      Ryan Carpenter
 * Date Created:    12/26/2024
 * Last Modified:   12/26/2024 
 * Notes:           Spawn Pad Used for spawning a variety of different objects 
 *                      and entities.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PadType
{
    chest
}

public class SpawnPad : MonoBehaviour
{
    // What type of object the pad will spawn
    [SerializeField] public PadType Type;

    // The object it is to spawn
    [SerializeField] private GameObject _spawnObject;

    [Header("Debug")]
    [SerializeField] private bool _debug;
    [SerializeField] private Vector3 _gizmoArea;

    /// <summary>
    /// Spawn the object from the inspector. If no object is in the inspector then do not spawn anything.
    /// </summary>
    public void SpawnObject()
    {
        if (_spawnObject == null)
        {
            Debug.LogWarning("[SpawnPad] Spawn Pad has no valid object to spawn.");
            return;
        }

        Instantiate(_spawnObject, transform.position, Quaternion.identity, transform);
    }

    /// <summary>
    /// Spawn the object from the inspector. If no object is in the inspector then do not spawn anything.
    /// </summary>
    /// <param name="spawnObject">The object to spawn.</param>
    public void SpawnObject(GameObject spawnObject)
    {
        if (spawnObject == null)
        {
            Debug.LogWarning("[SpawnPad] Spawn Pad was passed in an object of type null.");
            return;
        }

        Instantiate(spawnObject, transform.position, Quaternion.identity, transform);
    }

    private void OnDrawGizmos()
    {
        if (!_debug)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position - new Vector3(0f, -0.5f, 0f), _gizmoArea);
    }
}
