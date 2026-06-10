using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MazeWallVisualizer : MonoBehaviour
{
    [SerializeField] float lineWidth = 0.35f;
    [SerializeField] float wallY = 0.05f;
    [SerializeField] Color wallColor = Color.red;

    Mesh _mesh;
    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;

    void Awake()
    {
        EnsureMeshComponents();
    }

    void EnsureMeshComponents()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh { name = "MazeWallsMesh" };
        _meshFilter.sharedMesh = _mesh;

        var material = new Material(Shader.Find("Unlit/Color"));
        material.color = wallColor;
        _meshRenderer.sharedMaterial = material;
        _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _meshRenderer.receiveShadows = false;
    }

    public void Rebuild(IReadOnlyDictionary<string, MazeWall> walls)
    {
        if (_mesh == null)
            EnsureMeshComponents();

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        float halfWidth = lineWidth * 0.5f;

        foreach (KeyValuePair<string, MazeWall> entry in walls)
        {
            if (!entry.Value.bVisible)
                continue;

            AddWallQuad(
                entry.Value.A,
                entry.Value.B,
                halfWidth,
                wallY,
                vertices,
                triangles);
        }

        _mesh.Clear();
        if (vertices.Count == 0)
            return;

        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(triangles, 0);
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
    }

    static void AddWallQuad(
        Vector3 a,
        Vector3 b,
        float halfWidth,
        float y,
        List<Vector3> vertices,
        List<int> triangles)
    {
        Vector3 delta = b - a;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.0001f)
            return;

        Vector3 dir = delta.normalized;
        Vector3 perp = new Vector3(-dir.z, 0f, dir.x) * halfWidth;

        a.y = y;
        b.y = y;

        int start = vertices.Count;
        vertices.Add(a - perp);
        vertices.Add(a + perp);
        vertices.Add(b + perp);
        vertices.Add(b - perp);

        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }
}
