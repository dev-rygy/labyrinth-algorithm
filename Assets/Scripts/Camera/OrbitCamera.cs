using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Orbit")]
    [SerializeField] private Transform _target;
    [SerializeField] private float _radius = 10f;
    [SerializeField] private float _height = 5f;
    [SerializeField] private float _orbitSpeed = 20f;
    [SerializeField] private bool _showAngle = false;

    private float _angle;

    private void Update()
    {
        if (_target == null)
            return;

        _angle += _orbitSpeed * Time.deltaTime;

        float radians = _angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * _radius,
            _height,
            Mathf.Sin(radians) * _radius);

        transform.position = _target.position + offset;
        transform.LookAt(_target);

        if (_angle >= 360f)
        {
            _angle = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_target == null)
            return;

        Gizmos.color = Color.cyan;

        const int segments = 64;

        Vector3 previous = _target.position + new Vector3(_radius, _height, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;

            Vector3 next = _target.position + new Vector3(
                Mathf.Cos(angle) * _radius,
                _height,
                Mathf.Sin(angle) * _radius);

            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    private void OnGUI()
    {
        if (!_showAngle)
            return;

        // Displays custom text inside the box area
        GUI.Label(new Rect(1830, 1050, 230, 30), $"Angle: {_angle:F2}");
    }
}
