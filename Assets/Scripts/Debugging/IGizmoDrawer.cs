/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/30/2026
 * Last Modified:   06/30/2026 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public interface IGizmoDrawer
    {
        void SetColor(Color color);
        void Line(Vector3 a, Vector3 b);
        void WireCube(Vector3 center, Vector3 size);
        void Cube(Vector3 center, Vector3 size);
    }

    /// <summary>
    /// Wraps UnityEngine.Gizmos. Only valid to call from inside OnDrawGizmos/OnDrawGizmosSelected.
    /// </summary>
    public class EditorGizmoDrawer : IGizmoDrawer
    {
        public void SetColor(Color color) => Gizmos.color = color;
        public void Line(Vector3 a, Vector3 b) => Gizmos.DrawLine(a, b);
        public void WireCube(Vector3 center, Vector3 size) => Gizmos.DrawWireCube(center, size);
        public void Cube(Vector3 center, Vector3 size) => Gizmos.DrawCube(center, size);
    }

    /// <summary>
    /// Wraps RuntimeGizmos. Safe to call from anywhere, any time, including Player builds.
    /// </summary>
    public class RuntimeGizmoDrawer : IGizmoDrawer
    {
        private Color _color = Color.white;

        public void SetColor(Color color) => _color = color;
        public void Line(Vector3 a, Vector3 b) => RuntimeGizmos.Line(a, b, _color);
        public void WireCube(Vector3 center, Vector3 size) => RuntimeGizmos.WireCube(center, size, _color);
        public void Cube(Vector3 center, Vector3 size) => RuntimeGizmos.Cube(center, size, _color);
    }
}
