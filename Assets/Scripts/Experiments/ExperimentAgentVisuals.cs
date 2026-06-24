using UnityEngine;
using UnityEngine.Rendering;

public static class ExperimentAgentVisuals
{
    static Shader _cachedUnlitShader;

    public static float ResolveAgentCubeScale(MazeGenerator generator, int agentCount)
    {
        float cellSize = generator.Config.cellSize;
        float fromCell = Mathf.Max(1.25f, cellSize * 0.55f);
        float crowd = agentCount <= 2 ? 1f : agentCount <= 4 ? 0.85f : 0.7f;
        return fromCell * crowd;
    }

    public static float ResolveAgentHeight(MazeGenerator generator, float configuredHeight)
    {
        return configuredHeight > 0f ? configuredHeight : generator.Config.cellSize * 0.45f;
    }

    public static float ResolveGoalScale(MazeGenerator generator, float configuredScale)
    {
        if (configuredScale > 0f)
            return configuredScale;

        if (generator != null)
            return generator.Config.cellSize * 1.6f;

        return 8f;
    }

    /// <summary>
    /// Small flying swarm spheres: scale with maze cell size so agents stay visible in full-maze top-down view.
    /// Set configuredScale to 0 to use auto sizing.
    /// </summary>
    public static float ResolveSwarmSphereScale(MazeGenerator generator, float configuredScale, int agentCount)
    {
        if (configuredScale > 0.1f)
            return configuredScale;

        float cellScale = generator != null
            ? Mathf.Max(1.4f, generator.Config.cellSize * 0.48f)
            : 1.8f;
        float crowd = agentCount >= 64 ? 0.9f : 1f;
        float autoScale = cellScale * crowd;

        if (configuredScale <= 0.1f)
            return autoScale;

        // Older swarm runners defaulted to 0.7, which is too small in full-maze top-down view.
        if (configuredScale < autoScale * 0.75f)
            return autoScale;

        return configuredScale;
    }

    public static void ApplyUnlitColor(Renderer renderer, Color color, int renderQueue = 2450)
    {
        if (renderer == null)
            return;

        renderer.material = CreateUnlitMaterial(color, renderQueue);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    public static Material CreateUnlitMaterial(Color color, int renderQueue = 2450)
    {
        Shader shader = ResolveUnlitShader();
        var material = new Material(shader);
        material.name = "ExperimentUnlit";
        material.renderQueue = renderQueue;
        material.SetColor("_Color", color);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        ConfigureOpaque(material);

        // Standard fallback can pick up green bounce from maze walls; force self-lit color.
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        return material;
    }

    static Shader ResolveUnlitShader()
    {
        if (_cachedUnlitShader != null)
            return _cachedUnlitShader;

        _cachedUnlitShader = Shader.Find("Unlit/Color");
        if (_cachedUnlitShader == null)
            _cachedUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (_cachedUnlitShader == null)
            _cachedUnlitShader = Shader.Find("Standard");

        return _cachedUnlitShader;
    }

    static void ConfigureOpaque(Material material)
    {
        material.SetOverrideTag("RenderType", "Opaque");
        material.SetInt("_SrcBlend", (int)BlendMode.One);
        material.SetInt("_DstBlend", (int)BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.SetInt("_Cull", (int)CullMode.Back);
    }
}
