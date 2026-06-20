using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renders non-zero stigmergy cells as colored floor tiles (one color per depositor agent).
/// </summary>
public class MazeStigmergyVisualizer : MonoBehaviour
{
    static readonly Color[] DepositorColors =
    {
        new Color(0.2f, 0.85f, 1f, 0.55f),
        new Color(1f, 0.35f, 0.85f, 0.55f),
        new Color(1f, 0.85f, 0.2f, 0.55f),
        new Color(0.55f, 1f, 0.35f, 0.55f),
        new Color(1f, 0.55f, 0.15f, 0.55f),
        new Color(0.75f, 0.55f, 1f, 0.55f),
        new Color(0.35f, 1f, 0.85f, 0.55f),
        new Color(1f, 0.45f, 0.45f, 0.55f)
    };

    [SerializeField] float tileY = 0.02f;
    [SerializeField] float strengthAlphaScale = 0.12f;

    GameObject _chunkObject;
    Material _sharedMaterial;

    public void Rebuild(MazeStigmergyField field, float cellSize, float maxCellStrength)
    {
        ClearChunk();

        if (field == null || maxCellStrength <= 0f)
            return;

        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();

        for (int y = 0; y < field.Height; y++)
        {
            for (int x = 0; x < field.Width; x++)
            {
                if (!field.TryGetCellSignal(x, y, out float strength, out int depositor))
                    continue;

                Color baseColor = DepositorColors[Mathf.Abs(depositor) % DepositorColors.Length];
                float alpha = Mathf.Clamp01(strength * strengthAlphaScale);
                if (alpha <= 0.02f)
                    continue;

                baseColor.a = alpha;
                AddCellQuad(x, y, cellSize, tileY, baseColor, vertices, colors, triangles);
            }
        }

        if (vertices.Count == 0)
            return;

        EnsureMaterial();
        var mesh = new Mesh { name = "StigmergyTiles" };
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        _chunkObject = new GameObject("StigmergyTiles");
        _chunkObject.transform.SetParent(transform, false);

        var meshFilter = _chunkObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        var meshRenderer = _chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = _sharedMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    void EnsureMaterial()
    {
        if (_sharedMaterial != null)
            return;

        _sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    void ClearChunk()
    {
        if (_chunkObject == null)
            return;

        if (Application.isPlaying)
            Destroy(_chunkObject);
        else
            DestroyImmediate(_chunkObject);

        _chunkObject = null;
    }

    static void AddCellQuad(
        int cellX,
        int cellY,
        float cellSize,
        float worldY,
        Color color,
        List<Vector3> vertices,
        List<Color> colors,
        List<int> triangles)
    {
        float x0 = cellX * cellSize;
        float z0 = cellY * cellSize;
        float x1 = x0 + cellSize;
        float z1 = z0 + cellSize;
        float inset = cellSize * 0.08f;
        x0 += inset;
        z0 += inset;
        x1 -= inset;
        z1 -= inset;

        int start = vertices.Count;
        vertices.Add(new Vector3(x0, worldY, z0));
        vertices.Add(new Vector3(x1, worldY, z0));
        vertices.Add(new Vector3(x1, worldY, z1));
        vertices.Add(new Vector3(x0, worldY, z1));

        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);

        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }

    void OnDestroy()
    {
        ClearChunk();
    }
}
