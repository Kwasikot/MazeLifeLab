using UnityEngine;
using UnityEngine.Rendering;

public static class ExperimentAgentVisuals
{
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

    public static void ApplyUnlitColor(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var material = new Material(shader);
        material.color = color;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
