/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/10/2026
 * Last Modified:   06/10/2026 (Ryan)
 * Notes:           
*/
using UnityEngine;

public class Elevator : MovingPlatform
{
    [Header("Elevator Settings")]
    [SerializeField] private bool useRaycast = false;
    [SerializeField] private Vector3 _raycastOffset;
    [SerializeField] private bool useOrigin = false;

    protected override void Start()
    {
        base.Start();

        if (useRaycast)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + _raycastOffset, Vector3.down, out hit))
            {
                Vector3 adjHit = hit.point + Vector3.up * 0.2f; // Slightly above the hit point to prevent clipping
                _waypoints.Enqueue(adjHit);
            }
        }
        if (useOrigin)
        {
            _waypoints.Enqueue(transform.position);
        }
    }

    private void OnDrawGizmos()
    {
        if (useRaycast)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + _raycastOffset, transform.position + _raycastOffset + Vector3.down * 100f);
        }
    }
}
