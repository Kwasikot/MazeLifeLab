#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ExperimentSwarmTrailAblationHarness))]
public class ExperimentSwarmTrailAblationHarnessEditor : Editor
{
    ExperimentSwarmTrailAblationHarness _harness;

    void OnEnable()
    {
        _harness = (ExperimentSwarmTrailAblationHarness)target;
        EditorApplication.update += OnEditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    void OnEditorUpdate()
    {
        if (!Application.isPlaying || _harness == null)
            return;

        if (_harness.IsRunningBatch)
            Repaint();
    }

    public override void OnInspectorGUI()
    {
        var harness = (ExperimentSwarmTrailAblationHarness)target;
        DrawDefaultInspector();

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("EXP-SWARM-005 Trail Ablation Panel", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play mode, then press Run All Tests. Live stats appear here and in the Game view OSD. " +
                "If the Game view OSD is clipped, set OSD Anchor to Top Right (default) or Bottom Right.",
                MessageType.Info);
        }

        EditorGUI.BeginDisabledGroup(!Application.isPlaying || harness.IsRunningBatch);
        if (GUILayout.Button("Run All Tests", GUILayout.Height(32f)))
        {
            harness.RunAllTests();
            EditorUtility.SetDirty(harness);
        }
        EditorGUI.EndDisabledGroup();

        if (Application.isPlaying && harness.IsRunningBatch)
        {
            EditorGUILayout.HelpBox("Batch running… stats update live below.", MessageType.Warning);
            if (GUILayout.Button("Stop Batch"))
                harness.StopBatch();
        }

        DrawLiveStatus(harness.LiveStatus, harness.IsRunningBatch);

        SwarmTrailAblationReport report = harness.LastReport;
        bool hasPartialData = harness.LastWithoutTrails.Count > 0 || harness.LastWithTrails.Count > 0;
        if (!report.isComplete && !hasPartialData && !harness.IsRunningBatch)
            return;

        EditorGUILayout.Space(8f);
        DrawVerdict(report);
        DrawConditionStats("Without Trails", report.withoutTrails, harness.LastWithoutTrails);
        DrawConditionStats("With Trails", report.withTrails, harness.LastWithTrails);
        DrawDeltas(report);
    }

    static void DrawLiveStatus(SwarmTrailAblationLiveStatus live, bool isRunning)
    {
        if (!isRunning && live.completedRuns <= 0)
            return;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Live Batch Status", EditorStyles.boldLabel);

        if (isRunning)
        {
            string condition = live.currentScentEnabled ? "WITH trails" : "WITHOUT trails";
            EditorGUILayout.LabelField("Current run", $"{live.currentRunIndex} / {live.totalRuns}");
            EditorGUILayout.LabelField("Seed / condition", $"{live.currentSeed}  |  {condition}");
            EditorGUILayout.LabelField("Completed runs", $"{live.completedRuns} / {live.totalRuns}");
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Current episode (live)", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Steps", live.liveSteps.ToString());
            EditorGUILayout.LabelField("Food returned", live.liveFoodReturned.ToString());
            EditorGUILayout.LabelField("Food discovered", live.liveFoodDiscovered.ToString());
            EditorGUILayout.LabelField("Foraging efficiency", live.liveForagingEfficiency.ToString("F6"));
            EditorGUILayout.LabelField("Revisit ratio", live.liveRevisitRatio.ToString("F3"));
            EditorGUILayout.LabelField("Scent deposits", live.liveScentDeposits.ToString());
            EditorGUILayout.LabelField("Scent-influenced steps", live.liveScentSteps.ToString());
        }

        if (live.partialWithoutTrails.runCount > 0 || live.partialWithTrails.runCount > 0)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Running averages", EditorStyles.miniBoldLabel);
            DrawMiniStats("Without trails", live.partialWithoutTrails);
            DrawMiniStats("With trails", live.partialWithTrails);
        }
    }

    static void DrawMiniStats(string label, SwarmTrailAblationConditionStats stats)
    {
        if (stats.runCount <= 0)
        {
            EditorGUILayout.LabelField(label, "(no runs yet)");
            return;
        }

        EditorGUILayout.LabelField(
            label,
            $"{stats.runCount} runs | food={stats.meanFoodReturned:F1} | eff={stats.meanForagingEfficiency:F6} | revisit={stats.meanRevisitRatio:F3}");
    }

    static void DrawVerdict(SwarmTrailAblationReport report)
    {
        if (!report.isComplete)
            return;

        MessageType messageType = report.verdict switch
        {
            SwarmTrailAblationVerdict.TrailsWorking => MessageType.Info,
            SwarmTrailAblationVerdict.TrailsNotWorking => MessageType.Warning,
            _ => MessageType.None
        };

        string title = report.verdict switch
        {
            SwarmTrailAblationVerdict.TrailsWorking => "VERDICT: TRAILS WORKING",
            SwarmTrailAblationVerdict.TrailsNotWorking => "VERDICT: TRAILS NOT WORKING",
            _ => "VERDICT: INCONCLUSIVE"
        };

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(report.verdictSummary, messageType);
        EditorGUILayout.LabelField(
            "Primary metrics passed",
            $"{report.metricsPassed} / {report.metricsRequired}");
    }

    static void DrawConditionStats(
        string label,
        SwarmTrailAblationConditionStats stats,
        System.Collections.Generic.IReadOnlyList<SwarmForagingEpisodeSnapshot> runs)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Runs", stats.runCount.ToString());
        EditorGUILayout.LabelField("Mean food returned", stats.meanFoodReturned.ToString("F1"));
        EditorGUILayout.LabelField("Mean foraging efficiency", stats.meanForagingEfficiency.ToString("F6"));
        EditorGUILayout.LabelField(
            "Mean time to first food",
            stats.meanTimeToFirstFood >= 0f ? stats.meanTimeToFirstFood.ToString("F1") : "n/a");
        EditorGUILayout.LabelField("Mean revisit ratio", stats.meanRevisitRatio.ToString("F3"));
        EditorGUILayout.LabelField("Mean scent deposits", stats.meanScentDeposits.ToString("F1"));
        EditorGUILayout.LabelField("Mean scent-influenced steps", stats.meanScentInfluencedSteps.ToString("F1"));

        if (runs != null && runs.Count > 0)
        {
            EditorGUILayout.LabelField("Per-run food returned", FormatRunValues(runs, r => r.foodReturned));
            EditorGUILayout.LabelField("Per-run efficiency", FormatRunValues(runs, r => r.foragingEfficiency, "F6"));
        }
    }

    static void DrawDeltas(SwarmTrailAblationReport report)
    {
        if (!report.isComplete)
            return;

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("With Trails vs Without (%)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Food returned", FormatDelta(report.foodReturnedDeltaPercent));
        EditorGUILayout.LabelField("Foraging efficiency", FormatDelta(report.foragingEfficiencyDeltaPercent));
        EditorGUILayout.LabelField("Time to first food", FormatDelta(report.timeToFirstFoodDeltaPercent));
        EditorGUILayout.LabelField("Revisit ratio", FormatDelta(report.revisitRatioDeltaPercent, lowerIsBetter: true));
    }

    static string FormatDelta(float delta, bool lowerIsBetter = false)
    {
        string suffix = lowerIsBetter ? " (lower is better)" : string.Empty;
        return $"{delta:+0.0;-0.0;0.0}%{suffix}";
    }

    static string FormatRunValues(
        System.Collections.Generic.IReadOnlyList<SwarmForagingEpisodeSnapshot> runs,
        System.Func<SwarmForagingEpisodeSnapshot, float> selector,
        string format = "F0")
    {
        if (runs == null || runs.Count == 0)
            return string.Empty;

        var parts = new System.Text.StringBuilder();
        for (int i = 0; i < runs.Count; i++)
        {
            if (i > 0)
                parts.Append(", ");

            parts.Append(runs[i].seed);
            parts.Append('=');
            parts.Append(selector(runs[i]).ToString(format, System.Globalization.CultureInfo.InvariantCulture));
        }

        return parts.ToString();
    }
}
#endif
