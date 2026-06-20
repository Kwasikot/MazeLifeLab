using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renders maze walls as chunked meshes. Uses spatial tiles so large mazes (100x100+)
/// render across the full world extent, not only near the origin.
/// </summary>
public class MazeWallVisualizer : MonoBehaviour
{
    const int CellsPerTile = 20;

    [SerializeField] float wallY = 0.05f;
    [SerializeField] Color wallColor = Color.red;

    readonly Dictionary<long, TileBuilder> _tiles = new Dictionary<long, TileBuilder>();
    readonly List<GameObject> _chunkObjects = new List<GameObject>();
    Material _sharedMaterial;

    sealed class TileBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<int> Triangles = new List<int>();
    }

    void Awake()
    {
        RemoveLegacyRendererComponents();
        EnsureMaterial();
    }

    void RemoveLegacyRendererComponents()
    {
        var collider = GetComponent<MeshCollider>();
        if (collider != null)
            DestroyImmediate(collider);

        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
            DestroyImmediate(meshFilter);

        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            DestroyImmediate(meshRenderer);
    }

    void EnsureMaterial()
    {
        if (_sharedMaterial != null)
            return;

        _sharedMaterial = new Material(Shader.Find("Unlit/Color"));
        _sharedMaterial.color = wallColor;
    }

    public void Rebuild(IReadOnlyDictionary<string, MazeWall> walls, float cellSize = 5f)
    {
        EnsureMaterial();
        ClearChunks();
        _tiles.Clear();

        if (walls == null || walls.Count == 0)
            return;

        float halfWidth = Mathf.Max(0.35f, cellSize * 0.06f) * 0.5f;
        int visibleWalls = 0;

        foreach (KeyValuePair<string, MazeWall> entry in walls)
        {
            if (!entry.Value.bVisible)
                continue;

            visibleWalls++;
            Vector3 mid = (entry.Value.A + entry.Value.B) * 0.5f;
            int cellX = Mathf.FloorToInt(mid.x / cellSize);
            int cellY = Mathf.FloorToInt(mid.z / cellSize);
            int tileX = FloorDiv(cellX, CellsPerTile);
            int tileY = FloorDiv(cellY, CellsPerTile);

            TileBuilder tile = GetOrCreateTile(tileX, tileY);
            AddWallQuad(entry.Value.A, entry.Value.B, halfWidth, wallY, tile.Vertices, tile.Triangles);
        }

        int totalVertices = 0;
        foreach (KeyValuePair<long, TileBuilder> entry in _tiles)
        {
            totalVertices += BuildTileMeshes(entry.Value);
        }

        Debug.Log(
            $"[MazeWallVisualizer] walls={visibleWalls} vertices={totalVertices} " +
            $"tiles={_tiles.Count} chunks={_chunkObjects.Count} cellSize={cellSize}");
    }

    int BuildTileMeshes(TileBuilder tile)
    {
        return CreateChunkMesh(tile.Vertices, tile.Triangles);
    }

    int CreateChunkMesh(List<Vector3> vertices, List<int> triangles)
    {
        if (vertices.Count == 0 || triangles.Count == 0)
            return 0;

        var mesh = new Mesh { name = "MazeWallsChunk" };
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        var chunkObject = new GameObject($"MazeWallsChunk_{_chunkObjects.Count}");
        chunkObject.transform.SetParent(transform, false);
        chunkObject.transform.localPosition = Vector3.zero;
        chunkObject.transform.localRotation = Quaternion.identity;
        chunkObject.transform.localScale = Vector3.one;

        var meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        var meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = _sharedMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.allowOcclusionWhenDynamic = false;

        _chunkObjects.Add(chunkObject);
        return vertices.Count;
    }

    TileBuilder GetOrCreateTile(int tileX, int tileY)
    {
        long key = TileKey(tileX, tileY);
        if (!_tiles.TryGetValue(key, out TileBuilder tile))
        {
            tile = new TileBuilder();
            _tiles[key] = tile;
        }

        return tile;
    }

    void ClearChunks()
    {
        for (int i = 0; i < _chunkObjects.Count; i++)
        {
            if (_chunkObjects[i] == null)
                continue;

            if (Application.isPlaying)
                Destroy(_chunkObjects[i]);
            else
                DestroyImmediate(_chunkObjects[i]);
        }

        _chunkObjects.Clear();
    }

    static int FloorDiv(int value, int divisor)
    {
        if (value >= 0)
            return value / divisor;

        return (value - divisor + 1) / divisor;
    }

    static long TileKey(int tileX, int tileY)
    {
        return ((long)tileX << 32) | (uint)tileY;
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
