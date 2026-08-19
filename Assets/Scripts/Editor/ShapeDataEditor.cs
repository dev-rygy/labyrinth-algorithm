/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/19/2026
 * Last Modified:   08/19/2026 (Ryan)
 * Notes:           Renders ShapeData's RoomCells as an orbitable cube preview in the
 *                  Inspector's preview pane, the same way Unity previews meshes/prefabs.
 *                  Cube color is keyed off each cell's CellState.
*/
#if UNITY_EDITOR
using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RyansLibrary.UnityEditor
{
    [CustomEditor(typeof(ShapeData))]
    public class ShapeDataEditor : Editor
    {
        private const float k_cellSize = 1f;
        private const float k_cellGap = 0.05f;
        private const float k_opacity = 0.1f;

        private static readonly Dictionary<CellState, Color> k_stateColors = new Dictionary<CellState, Color>
        {
            { CellState.Blueprint, new Color(0.1f, 0.55f, 1f, k_opacity) },      // neon blue
            { CellState.NoBlueprint, new Color(1f, 0.15f, 0.2f, k_opacity) },    // neon red
            { CellState.DontCare, new Color(0.8f, 0.8f, 0.8f, k_opacity) },      // light grey
        };

        private PreviewRenderUtility _previewUtility;
        private Mesh _cubeMesh;
        private Mesh _cubeOutlineMesh;
        private readonly Dictionary<CellState, Material> _stateMaterials = new Dictionary<CellState, Material>();
        private readonly Dictionary<CellState, Material> _stateOutlineMaterials = new Dictionary<CellState, Material>();
        private Vector2 _previewDir = new Vector2(120f, -20f);

        public override bool HasPreviewGUI() => true;

        public override void OnPreviewSettings()
        {
            GUILayout.Label($"{((ShapeData)target).RoomCells?.Count ?? 0} cells", EditorStyles.whiteLabel);
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            _previewDir = Drag2D(_previewDir, r);

            if (Event.current.type != EventType.Repaint)
                return;

            InitPreviewUtility();

            ShapeData shapeData = (ShapeData)target;
            if (shapeData.RoomCells == null || shapeData.RoomCells.Count == 0)
            {
                EditorGUI.DropShadowLabel(r, "No RoomCells to preview");
                return;
            }

            Bounds bounds = ComputeBounds(shapeData);

            _previewUtility.BeginPreview(r, background);

            Quaternion rotation = Quaternion.Euler(_previewDir.y, 0f, 0f) * Quaternion.Euler(0f, _previewDir.x, 0f);
            PositionCamera(bounds, rotation);
            DrawCells(shapeData);

            _previewUtility.Render();
            Texture resultRender = _previewUtility.EndPreview();
            GUI.DrawTexture(r, resultRender, ScaleMode.StretchToFill, false);
        }

        // Thumbnail used by AssetPreview wherever ShapeData is referenced (e.g. RoomShapeEntryDrawer's
        // inline preview) - same cube geometry as the interactive pane, just from a fixed angle.
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            ShapeData shapeData = target as ShapeData;
            if (shapeData == null || shapeData.RoomCells == null || shapeData.RoomCells.Count == 0)
                return null;

            InitPreviewUtility();

            Bounds bounds = ComputeBounds(shapeData);

            _previewUtility.BeginStaticPreview(new Rect(0, 0, width, height));

            PositionCamera(bounds, Quaternion.Euler(-20f, 120f, 0f));
            DrawCells(shapeData);

            _previewUtility.Render();
            return _previewUtility.EndStaticPreview();
        }

        private void PositionCamera(Bounds bounds, Quaternion rotation)
        {
            float radius = bounds.extents.magnitude;
            float distance = radius * 5f + 4f;

            _previewUtility.camera.orthographic = true;
            _previewUtility.camera.orthographicSize = radius * 1.15f + 0.25f;

            _previewUtility.camera.transform.position = bounds.center + rotation * (Vector3.back * distance);
            _previewUtility.camera.transform.LookAt(bounds.center);
            _previewUtility.camera.nearClipPlane = 0.01f;
            _previewUtility.camera.farClipPlane = distance * 2f + bounds.extents.magnitude * 2f;
        }

        private void DrawCells(ShapeData shapeData)
        {
            float cubeScale = k_cellSize - k_cellGap;
            foreach (ShapeCell cell in shapeData.RoomCells)
            {
                Matrix4x4 matrix = Matrix4x4.TRS((Vector3)cell.Position * k_cellSize, Quaternion.identity, Vector3.one * cubeScale);
                _previewUtility.DrawMesh(_cubeMesh, matrix, GetMaterial(cell.State, _stateMaterials), 0);
                _previewUtility.DrawMesh(_cubeOutlineMesh, matrix, GetMaterial(cell.State, _stateOutlineMaterials), 0);
            }
        }

        private void InitPreviewUtility()
        {
            if (_previewUtility != null)
                return;

            _previewUtility = new PreviewRenderUtility();
            _previewUtility.camera.farClipPlane = 100f;
            _previewUtility.camera.nearClipPlane = 0.01f;
            _previewUtility.lights[0].intensity = 1.2f;
            _previewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
            _previewUtility.lights[1].intensity = 0.6f;

            _cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            _cubeOutlineMesh = CreateCubeOutlineMesh();

            foreach (KeyValuePair<CellState, Color> entry in k_stateColors)
            {
                _stateMaterials[entry.Key] = CreateTransparentMaterial(entry.Value);
                // Outline reuses the same hue as the fill, just fully opaque, so it reads as a neon edge per state.
                Color outlineColor = entry.Value;
                outlineColor.a = 1f;
                _stateOutlineMaterials[entry.Key] = CreateTransparentMaterial(outlineColor);
            }
        }

        private static Material GetMaterial(CellState state, Dictionary<CellState, Material> materials)
        {
            if (materials.TryGetValue(state, out Material material))
                return material;

            return materials[CellState.DontCare];
        }

        // Cube outlines
        private static Mesh CreateCubeOutlineMesh()
        {
            Vector3[] vertices =
            {
                new Vector3(-0.5f, -0.5f, -0.5f), // 0
                new Vector3( 0.5f, -0.5f, -0.5f), // 1
                new Vector3( 0.5f, -0.5f,  0.5f), // 2
                new Vector3(-0.5f, -0.5f,  0.5f), // 3
                new Vector3(-0.5f,  0.5f, -0.5f), // 4
                new Vector3( 0.5f,  0.5f, -0.5f), // 5
                new Vector3( 0.5f,  0.5f,  0.5f), // 6
                new Vector3(-0.5f,  0.5f,  0.5f), // 7
            };

            int[] lineIndices =
            {
                0, 1, 1, 2, 2, 3, 3, 0, // bottom face
                4, 5, 5, 6, 6, 7, 7, 4, // top face
                0, 4, 1, 5, 2, 6, 3, 7, // verticals
            };

            Mesh mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices;
            mesh.SetIndices(lineIndices, MeshTopology.Lines, 0);

            return mesh;
        }

        // Set material properties
        private static Material CreateTransparentMaterial(Color color)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            Material material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            material.SetColor("_Color", color);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
            material.SetInt("_ZWrite", 0);

            return material;
        }

        private static Bounds ComputeBounds(ShapeData shapeData)
        {
            Bounds bounds = new Bounds((Vector3)shapeData.RoomCells[0].Position * k_cellSize, Vector3.one * k_cellSize);
            foreach (ShapeCell cell in shapeData.RoomCells)
                bounds.Encapsulate(new Bounds((Vector3)cell.Position * k_cellSize, Vector3.one * k_cellSize));

            return bounds;
        }

        // Standard click-drag-to-orbit handler, mirroring the interaction Unity's own mesh/prefab preview uses.
        private static Vector2 Drag2D(Vector2 scrollPosition, Rect position)
        {
            int controlId = GUIUtility.GetControlID("ShapeDataPreviewDrag".GetHashCode(), FocusType.Passive);
            Event evt = Event.current;

            switch (evt.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition) && evt.button <= 1)
                    {
                        GUIUtility.hotControl = controlId;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        scrollPosition -= evt.delta * (evt.shift ? 3f : 1f) / Mathf.Min(position.width, position.height) * 140f;
                        scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90f, 90f);
                        evt.Use();
                        GUI.changed = true;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
            }

            return scrollPosition;
        }

        public void OnDisable()
        {
            if (_previewUtility != null)
            {
                _previewUtility.Cleanup();
                _previewUtility = null;
            }

            foreach (Material material in _stateMaterials.Values)
                DestroyImmediate(material);
            _stateMaterials.Clear();

            foreach (Material material in _stateOutlineMaterials.Values)
                DestroyImmediate(material);
            _stateOutlineMaterials.Clear();

            if (_cubeOutlineMesh != null)
            {
                DestroyImmediate(_cubeOutlineMesh);
                _cubeOutlineMesh = null;
            }
        }
    }
}
#endif
