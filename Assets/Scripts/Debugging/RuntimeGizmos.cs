/*
 * RuntimeGizmos.cs
 *
 * Lightweight GL-based line/quad drawer that renders in Player builds, not just the Editor.
 * Hooked into URP via RenderPipelineManager.endCameraRendering (Camera.onPostRender, which
 * Gizmos/Debug.DrawLine effectively rely on, is never invoked by SRP cameras).
 *
 * USAGE: call RuntimeGizmos.Line / WireCube / Cube every frame (e.g. from Update), the same
 * way you'd call Gizmos.DrawLine / DrawWireCube / DrawCube from OnDrawGizmos. The queue is
 * captured once per frame and drawn to every camera that renders that frame, then cleared.
 *
 * IMPORTANT - BUILD STRIPPING GOTCHA:
 * Shader.Find("Hidden/Internal-Colored") can return null in an actual Player build if Unity's
 * shader stripper decides nothing references it. If lines show up in the Editor but not in a
 * build, this is almost certainly why. Fix: Edit > Project Settings > Graphics > Always
 * Included Shaders, and add "Hidden/Internal-Colored" to the list.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RyansLibrary.Labyrinth
{
    public static class RuntimeGizmos
    {
        public static bool Enabled = true;

        private struct LineEntry
        {
            public Vector3 A, B;
            public Color Color;
        }

        private struct QuadEntry
        {
            public Vector3 A, B, C, D;
            public Color Color;
        }

        // Filled by Line()/WireCube()/Cube() calls during the current frame
        private static List<LineEntry> _pendingLines = new();
        private static List<QuadEntry> _pendingQuads = new();

        // Snapshot drawn to every camera that renders during this frame
        private static List<LineEntry> _drawLines = new();
        private static List<QuadEntry> _drawQuads = new();

        private static int _snapshotFrame = -1;
        private static Material _material;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        public static void Line(Vector3 a, Vector3 b, Color color)
        {
            if (!Enabled) return;
            _pendingLines.Add(new LineEntry { A = a, B = b, Color = color });
        }

        public static void WireCube(Vector3 center, Vector3 size, Color color)
        {
            if (!Enabled) return;

            Vector3[] c = CubeCorners(center, size);
            int[,] edges = { { 0, 1 }, { 0, 2 }, { 0, 4 }, { 1, 3 }, { 1, 5 }, { 2, 3 },
                              { 2, 6 }, { 3, 7 }, { 4, 5 }, { 4, 6 }, { 5, 7 }, { 6, 7 } };

            for (int i = 0; i < edges.GetLength(0); i++)
                Line(c[edges[i, 0]], c[edges[i, 1]], color);
        }

        public static void Cube(Vector3 center, Vector3 size, Color color)
        {
            if (!Enabled) return;

            Vector3[] c = CubeCorners(center, size);
            int[,] faces = { { 0, 1, 3, 2 }, { 4, 6, 7, 5 }, { 0, 4, 5, 1 },
                              { 2, 3, 7, 6 }, { 0, 2, 6, 4 }, { 1, 5, 7, 3 } };

            for (int i = 0; i < faces.GetLength(0); i++)
            {
                _pendingQuads.Add(new QuadEntry
                {
                    A = c[faces[i, 0]],
                    B = c[faces[i, 1]],
                    C = c[faces[i, 2]],
                    D = c[faces[i, 3]],
                    Color = color
                });
            }
        }

        private static Vector3[] CubeCorners(Vector3 center, Vector3 size)
        {
            Vector3 ext = size * 0.5f;
            Vector3[] c = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                c[i] = center + new Vector3(
                    (i & 1) == 0 ? -ext.x : ext.x,
                    (i & 2) == 0 ? -ext.y : ext.y,
                    (i & 4) == 0 ? -ext.z : ext.z);
            }
            return c;
        }

        private static void EnsureMaterial()
        {
            if (_material != null)
                return;

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                Debug.LogError("Could not find shader 'Hidden/Internal-Colored'. " +
                    "Add it to Project Settings > Graphics > Always Included Shaders so it survives build stripping.");
                return;
            }

            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _material.SetInt("_ZWrite", 0);
        }

        private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // Skip preview/reflection cameras (asset thumbnails, reflection probes, etc.)
            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
                return;

            // Take a snapshot of this frame's queued geometry exactly once, so every camera
            // that renders this frame (main camera, scene view camera, a minimap camera, etc.)
            // draws the same data instead of racing to drain the pending lists.
            if (_snapshotFrame != Time.frameCount)
            {
                (_drawLines, _pendingLines) = (_pendingLines, _drawLines);
                (_drawQuads, _pendingQuads) = (_pendingQuads, _drawQuads);
                _pendingLines.Clear();
                _pendingQuads.Clear();
                _snapshotFrame = Time.frameCount;
            }

            if (_drawLines.Count == 0 && _drawQuads.Count == 0)
                return;

            EnsureMaterial();
            if (_material == null)
                return;

            _material.SetPass(0);

            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;

            GL.Begin(GL.LINES);
            foreach (LineEntry l in _drawLines)
            {
                GL.Color(l.Color);
                GL.Vertex(l.A);
                GL.Vertex(l.B);
            }
            GL.End();

            GL.Begin(GL.QUADS);
            foreach (QuadEntry q in _drawQuads)
            {
                GL.Color(q.Color);
                GL.Vertex(q.A);
                GL.Vertex(q.B);
                GL.Vertex(q.C);
                GL.Vertex(q.D);
            }
            GL.End();

            GL.PopMatrix();
        }
    }
}
