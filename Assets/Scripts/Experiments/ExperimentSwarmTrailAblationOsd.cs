using System.Globalization;
using UnityEngine;

public enum SwarmTrailAblationOsdAnchor
{
    TopRight = 0,
    TopLeft = 1,
    BottomRight = 2,
    BottomLeft = 3
}

/// <summary>
/// On-screen stats overlay for trail ablation batch runs (Game view).
/// </summary>
[DisallowMultipleComponent]
public class ExperimentSwarmTrailAblationOsd : MonoBehaviour
{
    const float InnerPadding = 12f;
    const float PreferredWidth = 320f;
    const float LineHeight = 19f;

    ExperimentSwarmTrailAblationHarness _harness;
    GUIStyle _titleStyle;
    GUIStyle _labelStyle;
    Texture2D _background;
    bool _stylesReady;

    public void Bind(ExperimentSwarmTrailAblationHarness harness)
    {
        _harness = harness;
    }

    void OnGUI()
    {
        if (_harness == null || !_harness.ShowOnScreenDisplay)
            return;

        DrawPanel();
    }

    void DrawPanel()
    {
        EnsureStyles();

        SwarmTrailAblationLiveStatus live = _harness.LiveStatus;
        SwarmTrailAblationReport report = _harness.LastReport;
        bool showLive = live.isRunningBatch || _harness.IsRunningBatch;
        bool showFinal = report.isComplete;

        if (!showLive && !showFinal && _harness.LastWithoutTrails.Count == 0)
            return;

        int lineCount = 1;
        if (showLive)
            lineCount += 12;
        if (showFinal)
            lineCount += 5;

        float panelHeight = InnerPadding * 2f + lineCount * LineHeight;
        Rect panel = ComputePanelRect(panelHeight, _harness.OsdAnchor, _harness.OsdScreenMargin);

        GUI.DrawTexture(panel, _background, ScaleMode.StretchToFill);

        float x = panel.x + InnerPadding;
        float y = panel.y + InnerPadding;
        float width = panel.width - InnerPadding * 2f;

        y = DrawLabel(x, y, width, "EXP-SWARM-005 Trail Ablation", _titleStyle);

        if (showLive)
        {
            string condition = live.currentScentEnabled ? "WITH trails" : "WITHOUT trails";
            y = DrawLabel(x, y, width, $"Run {live.currentRunIndex}/{live.totalRuns}", _labelStyle);
            y = DrawLabel(x, y, width, $"Seed {live.currentSeed} | {condition}", _labelStyle);
            y = DrawLabel(x, y, width, $"Completed {live.completedRuns}/{live.totalRuns}", _labelStyle);
            y += 4f;
            y = DrawLabel(x, y, width, "Current episode", _titleStyle);
            y = DrawLabel(x, y, width, $"Steps: {live.liveSteps}", _labelStyle);
            y = DrawLabel(x, y, width, $"Food returned: {live.liveFoodReturned}", _labelStyle);
            y = DrawLabel(x, y, width, $"Food discovered: {live.liveFoodDiscovered}", _labelStyle);
            y = DrawLabel(
                x,
                y,
                width,
                $"Efficiency: {live.liveForagingEfficiency.ToString("F6", CultureInfo.InvariantCulture)}",
                _labelStyle);
            y = DrawLabel(
                x,
                y,
                width,
                $"Revisit: {live.liveRevisitRatio.ToString("F3", CultureInfo.InvariantCulture)}",
                _labelStyle);
            y = DrawLabel(x, y, width, $"Scent deposits: {live.liveScentDeposits}", _labelStyle);
            y = DrawLabel(x, y, width, $"Scent steps: {live.liveScentSteps}", _labelStyle);
            y += 4f;
            y = DrawLabel(x, y, width, "Batch averages so far", _titleStyle);
            y = DrawConditionBlock(x, y, width, "Without trails", live.partialWithoutTrails);
            y = DrawConditionBlock(x, y, width, "With trails", live.partialWithTrails);
        }

        if (showFinal)
        {
            y += 6f;
            y = DrawLabel(x, y, width, FormatVerdictTitle(report.verdict), _titleStyle);
            y = DrawWrappedLabel(x, y, width, report.verdictSummary, _labelStyle);
            y = DrawLabel(
                x,
                y,
                width,
                $"Metrics passed: {report.metricsPassed}/{report.metricsRequired}",
                _labelStyle);
            y = DrawLabel(
                x,
                y,
                width,
                $"Food returned: {FormatDelta(report.foodReturnedDeltaPercent)}",
                _labelStyle);
            y = DrawLabel(
                x,
                y,
                width,
                $"Efficiency: {FormatDelta(report.foragingEfficiencyDeltaPercent)}",
                _labelStyle);
        }
    }

    public static Rect ComputePanelRect(float preferredHeight, SwarmTrailAblationOsdAnchor anchor, float margin)
    {
        float viewWidth = Mathf.Max(1f, Screen.width);
        float viewHeight = Mathf.Max(1f, Screen.height);
        margin = Mathf.Max(8f, margin);

        float maxWidth = Mathf.Max(200f, viewWidth - margin * 2f);
        float width = Mathf.Min(PreferredWidth, maxWidth);
        float maxHeight = Mathf.Max(140f, viewHeight - margin * 2f);
        float height = Mathf.Min(preferredHeight, maxHeight);

        float x;
        float y;
        switch (anchor)
        {
            case SwarmTrailAblationOsdAnchor.TopLeft:
                x = margin;
                y = margin;
                break;
            case SwarmTrailAblationOsdAnchor.BottomLeft:
                x = margin;
                y = viewHeight - height - margin;
                break;
            case SwarmTrailAblationOsdAnchor.BottomRight:
                x = viewWidth - width - margin;
                y = viewHeight - height - margin;
                break;
            default:
                x = viewWidth - width - margin;
                y = margin;
                break;
        }

        x = Mathf.Clamp(x, margin, Mathf.Max(margin, viewWidth - width - margin));
        y = Mathf.Clamp(y, margin, Mathf.Max(margin, viewHeight - height - margin));
        return new Rect(x, y, width, height);
    }

    float DrawLabel(float x, float y, float width, string text, GUIStyle style)
    {
        GUI.Label(new Rect(x, y, width, LineHeight), text, style);
        return y + LineHeight;
    }

    float DrawWrappedLabel(float x, float y, float width, string text, GUIStyle style)
    {
        float height = style.CalcHeight(new GUIContent(text), width);
        GUI.Label(new Rect(x, y, width, height), text, style);
        return y + height + 2f;
    }

    float DrawConditionBlock(float x, float y, float width, string title, SwarmTrailAblationConditionStats stats)
    {
        if (stats.runCount <= 0)
            return DrawLabel(x, y, width, $"{title}: no runs yet", _labelStyle);

        return DrawLabel(
            x,
            y,
            width,
            $"{title} ({stats.runCount}): food {stats.meanFoodReturned.ToString("F1", CultureInfo.InvariantCulture)}, " +
            $"eff {stats.meanForagingEfficiency.ToString("F6", CultureInfo.InvariantCulture)}",
            _labelStyle);
    }

    static string FormatVerdictTitle(SwarmTrailAblationVerdict verdict)
    {
        return verdict switch
        {
            SwarmTrailAblationVerdict.TrailsWorking => "VERDICT: TRAILS WORKING",
            SwarmTrailAblationVerdict.TrailsNotWorking => "VERDICT: TRAILS NOT WORKING",
            _ => "VERDICT: INCONCLUSIVE"
        };
    }

    static string FormatDelta(float delta)
    {
        return $"{delta:+0.0;-0.0;0.0}%";
    }

    void EnsureStyles()
    {
        if (_stylesReady)
            return;

        _background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.85f));
        _titleStyle = BuildTextStyle(new Color(1f, 0.92f, 0.45f), FontStyle.Bold, 13);
        _labelStyle = BuildTextStyle(Color.white, FontStyle.Normal, 12);
        _stylesReady = true;
    }

    static GUIStyle BuildTextStyle(Color color, FontStyle fontStyle, int fontSize)
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = fontStyle,
            wordWrap = true,
            clipping = TextClipping.Clip,
            alignment = TextAnchor.UpperLeft,
            border = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            normal = { textColor = color }
        };
    }

    static Texture2D MakeTexture(int width, int height, Color color)
    {
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        var texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
