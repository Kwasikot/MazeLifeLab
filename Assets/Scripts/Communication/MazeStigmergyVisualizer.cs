using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renders stigmergy trails. With per-agent layers, every agent's path stays visible even when
/// cells overlap (the shared field only keeps the last depositor for read bias).
/// </summary>
public class MazeStigmergyVisualizer : MonoBehaviour
{
    public struct AgentTrailLayer
    {
        public int AgentIndex;
        public IReadOnlyList<Vector2Int> Cells;
        public Color Color;
    }

    public struct ClaimLayer
    {
        public int AgentIndex;
        public Vector2Int Center;
        public int RadiusCells;
        public Color Color;
    }

    static readonly Color[] DefaultDepositorColors =
    {
        new Color(0.2f, 0.85f, 1f, 0.55f),
        new Color(1f, 0.35f, 0.85f, 0.55f),
        new Color(1f, 0.85f, 0.2f, 0.55f),
        new Color(0.55f, 1f, 0.35f, 0.55f),
        new Color(1f, 0.55f, 0.15f, 0.55f),
        new Color(0.75f, 0.55f, 1f, 0.55f),
        new Color(0.35f, 1f, 0.85f, 0.55f),
        new Color(1f, 0.45f, 0.45f, 0.55f),
        new Color(0.95f, 0.95f, 0.95f, 0.55f),
        new Color(0.6f, 0.4f, 0.2f, 0.55f)
    };

    [SerializeField] float tileY = 0.02f;
    [SerializeField] float perAgentLayerStepY = 0.004f;
    [SerializeField] float strengthAlphaScale = 0.12f;
    [SerializeField] float perAgentTrailAlpha = 0.42f;

    readonly List<GameObject> _layerObjects = new List<GameObject>();
    Material _sharedMaterial;

    public void Rebuild(MazeStigmergyField field, float cellSize, float maxCellStrength)
    {
        Rebuild(field, cellSize, maxCellStrength, null, null);
    }

    public void Rebuild(
        MazeStigmergyField field,
        float cellSize,
        float maxCellStrength,
        IReadOnlyList<AgentTrailLayer> agentTrails)
    {
        Rebuild(field, cellSize, maxCellStrength, agentTrails, null);
    }

    public void Rebuild(
        MazeStigmergyField field,
        float cellSize,
        float maxCellStrength,
        IReadOnlyList<AgentTrailLayer> agentTrails,
        IReadOnlyList<ClaimLayer> claimLayers)
    {
        ClearLayers();
        bool builtLayer = false;

        if (agentTrails != null && agentTrails.Count > 0)
        {
            for (int i = 0; i < agentTrails.Count; i++)
            {
                AgentTrailLayer layer = agentTrails[i];
                if (layer.Cells == null || layer.Cells.Count == 0)
                    continue;

                Color color = layer.Color;
                color.a = perAgentTrailAlpha;
                float layerY = tileY + layer.AgentIndex * perAgentLayerStepY;
                BuildLayerMesh(
                    $"StigmergyTrail_A{layer.AgentIndex}",
                    layer.Cells,
                    cellSize,
                    layerY,
                    color,
                    insetScale: 0.72f);
                builtLayer = true;
            }
        }

        if (claimLayers != null && claimLayers.Count > 0)
        {
            for (int i = 0; i < claimLayers.Count; i++)
            {
                ClaimLayer claim = claimLayers[i];
                if (claim.RadiusCells <= 0)
                    continue;

                Color color = claim.Color;
                color.a = 0.18f;
                BuildClaimMesh(
                    $"FrontierClaim_A{claim.AgentIndex}",
                    claim.Center,
                    claim.RadiusCells,
                    cellSize,
                    tileY + 0.12f + claim.AgentIndex * perAgentLayerStepY,
                    color);
                builtLayer = true;
            }
        }

        if (builtLayer)
            return;

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

                Color baseColor = DefaultDepositorColors[Mathf.Abs(depositor) % DefaultDepositorColors.Length];
                float alpha = Mathf.Clamp01(strength * strengthAlphaScale);
                if (alpha <= 0.02f)
                    continue;

                baseColor.a = alpha;
                AddCellQuad(x, y, cellSize, tileY, baseColor, 1f, vertices, colors, triangles);
            }
        }

        if (vertices.Count == 0)
            return;

        AttachMesh("StigmergyTiles", vertices, colors, triangles);
    }

    void BuildLayerMesh(
        string objectName,
        IReadOnlyList<Vector2Int> cells,
        float cellSize,
        float worldY,
        Color color,
        float insetScale)
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            AddCellQuad(cell.x, cell.y, cellSize, worldY, color, insetScale, vertices, colors, triangles);
        }

        if (vertices.Count == 0)
            return;

        AttachMesh(objectName, vertices, colors, triangles);
    }

    void BuildClaimMesh(
        string objectName,
        Vector2Int center,
        int radiusCells,
        float cellSize,
        float worldY,
        Color color)
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();

        for (int dz = -radiusCells; dz <= radiusCells; dz++)
        {
            for (int dx = -radiusCells; dx <= radiusCells; dx++)
            {
                int manhattan = Mathf.Abs(dx) + Mathf.Abs(dz);
                if (manhattan > radiusCells)
                    continue;

                int x = center.x + dx;
                int y = center.y + dz;
                if (x < 0 || y < 0)
                    continue;

                float falloff = 1f - (float)manhattan / Mathf.Max(1, radiusCells);
                Color cellColor = color;
                cellColor.a *= Mathf.Lerp(0.25f, 1f, falloff);
                AddCellQuad(x, y, cellSize, worldY, cellColor, 0.96f, vertices, colors, triangles);
            }
        }

        if (vertices.Count == 0)
            return;

        AttachMesh(objectName, vertices, colors, triangles);
    }

    void AttachMesh(string objectName, List<Vector3> vertices, List<Color> colors, List<int> triangles)
    {
        EnsureMaterial();
        var mesh = new Mesh { name = objectName };
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        var layerObject = new GameObject(objectName);
        layerObject.transform.SetParent(transform, false);

        var meshFilter = layerObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        var meshRenderer = layerObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = _sharedMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        _layerObjects.Add(layerObject);
    }

    void EnsureMaterial()
    {
        if (_sharedMaterial != null)
            return;

        _sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    void ClearLayers()
    {
        for (int i = 0; i < _layerObjects.Count; i++)
        {
            GameObject obj = _layerObjects[i];
            if (obj == null)
                continue;

            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        _layerObjects.Clear();
    }

    static void AddCellQuad(
        int cellX,
        int cellY,
        float cellSize,
        float worldY,
        Color color,
        float insetScale,
        List<Vector3> vertices,
        List<Color> colors,
        List<int> triangles)
    {
        float x0 = cellX * cellSize;
        float z0 = cellY * cellSize;
        float x1 = x0 + cellSize;
        float z1 = z0 + cellSize;
        float margin = cellSize * (1f - insetScale) * 0.5f;
        x0 += margin;
        z0 += margin;
        x1 -= margin;
        z1 -= margin;

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
        ClearLayers();
    }
}
