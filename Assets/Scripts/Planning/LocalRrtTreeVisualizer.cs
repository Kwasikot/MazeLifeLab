using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LocalRrtTreeVisualizer : MonoBehaviour
{
    [SerializeField] float lineWidth = 0.2f;
    [SerializeField] float lineY = 0.3f;

    Mesh _treeMesh;
    Mesh _pathMesh;
    MeshFilter _treeFilter;
    MeshRenderer _treeRenderer;
    MeshFilter _pathFilter;
    MeshRenderer _pathRenderer;
    Transform _pathRoot;

    void Awake()
    {
        EnsureComponents();
    }

    void EnsureComponents()
    {
        transform.SetParent(null, true);
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        _treeFilter = GetComponent<MeshFilter>();
        _treeRenderer = GetComponent<MeshRenderer>();
        _treeMesh = new Mesh { name = "LocalRrtTreeMesh" };
        _treeFilter.sharedMesh = _treeMesh;
        _treeRenderer.sharedMaterial = CreateUnlitMaterial(new Color(0.2f, 1f, 0.35f, 0.95f));
        DisableShadows(_treeRenderer);

        _pathRoot = transform.Find("LocalRrtPathVisual");
        if (_pathRoot == null)
        {
            var pathObject = new GameObject("LocalRrtPathVisual");
            pathObject.transform.SetParent(transform, false);
            _pathRoot = pathObject.transform;
        }

        _pathFilter = _pathRoot.GetComponent<MeshFilter>();
        if (_pathFilter == null)
            _pathFilter = _pathRoot.gameObject.AddComponent<MeshFilter>();

        _pathRenderer = _pathRoot.GetComponent<MeshRenderer>();
        if (_pathRenderer == null)
            _pathRenderer = _pathRoot.gameObject.AddComponent<MeshRenderer>();

        _pathMesh = new Mesh { name = "LocalRrtPathMesh" };
        _pathFilter.sharedMesh = _pathMesh;
        _pathRenderer.sharedMaterial = CreateUnlitMaterial(new Color(0.2f, 0.85f, 1f, 1f));
        DisableShadows(_pathRenderer);
    }

    public void Clear()
    {
        EnsureComponents();
        _treeMesh.Clear();
        _pathMesh.Clear();
    }

    public void Rebuild(
        MazeGenerator generator,
        IReadOnlyList<LocalRrtEdge> treeEdges,
        IReadOnlyList<Vector2Int> path,
        bool showTree,
        bool showPath,
        Color treeColor,
        Color pathColor)
    {
        EnsureComponents();
        _treeRenderer.sharedMaterial.color = treeColor;
        _pathRenderer.sharedMaterial.color = pathColor;

        RebuildTreeMesh(generator, treeEdges, showTree, lineY);
        RebuildPathMesh(generator, path, showPath, lineY + 0.08f);
    }

    void RebuildTreeMesh(MazeGenerator generator, IReadOnlyList<LocalRrtEdge> edges, bool showTree, float y)
    {
        _treeMesh.Clear();
        if (!showTree || generator == null || edges == null || edges.Count == 0)
            return;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        float halfWidth = lineWidth * 0.5f;

        foreach (LocalRrtEdge edge in edges)
        {
            if (!IsValidCell(generator, edge.From.x, edge.From.y) ||
                !IsValidCell(generator, edge.To.x, edge.To.y))
            {
                continue;
            }

            Vector3 a = generator.GetCellCenterWorld(edge.From.x, edge.From.y);
            Vector3 b = generator.GetCellCenterWorld(edge.To.x, edge.To.y);
            AddLineQuad(a, b, halfWidth, y, vertices, triangles);
        }

        if (vertices.Count == 0)
            return;

        _treeMesh.SetVertices(vertices);
        _treeMesh.SetTriangles(triangles, 0);
        _treeMesh.RecalculateBounds();
        _treeMesh.RecalculateNormals();
    }

    void RebuildPathMesh(MazeGenerator generator, IReadOnlyList<Vector2Int> path, bool showPath, float y)
    {
        _pathMesh.Clear();
        if (!showPath || generator == null || path == null || path.Count < 2)
            return;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        float halfWidth = lineWidth * 0.65f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2Int from = path[i];
            Vector2Int to = path[i + 1];
            if (!IsValidCell(generator, from.x, from.y) || !IsValidCell(generator, to.x, to.y))
                continue;

            Vector3 a = generator.GetCellCenterWorld(from.x, from.y);
            Vector3 b = generator.GetCellCenterWorld(to.x, to.y);
            AddLineQuad(a, b, halfWidth, y, vertices, triangles);
        }

        if (vertices.Count == 0)
            return;

        _pathMesh.SetVertices(vertices);
        _pathMesh.SetTriangles(triangles, 0);
        _pathMesh.RecalculateBounds();
        _pathMesh.RecalculateNormals();
    }

    static bool IsValidCell(MazeGenerator generator, int cellX, int cellY)
    {
        return generator != null && generator.IsCellInBounds(cellX, cellY);
    }

    static void AddLineQuad(
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

    static Material CreateUnlitMaterial(Color color)
    {
        var material = new Material(Shader.Find("Unlit/Color"));
        material.color = color;
        return material;
    }

    static void DisableShadows(Renderer renderer)
    {
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
