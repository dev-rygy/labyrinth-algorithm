using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Orbit")]
    [SerializeField] private Transform target;
    [SerializeField] private float radius = 10f;
    [SerializeField] private float height = 5f;
    [SerializeField] private float orbitSpeed = 20f;

    private float _angle;

    private void Update()
    {
        if (target == null)
            return;

        _angle += orbitSpeed * Time.deltaTime;

        float radians = _angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * radius,
            height,
            Mathf.Sin(radians) * radius);

        transform.position = target.position + offset;
        transform.LookAt(target);
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null)
            return;

        Gizmos.color = Color.cyan;

        const int segments = 64;

        Vector3 previous = target.position + new Vector3(radius, height, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;

            Vector3 next = target.position + new Vector3(
                Mathf.Cos(angle) * radius,
                height,
                Mathf.Sin(angle) * radius);

            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
