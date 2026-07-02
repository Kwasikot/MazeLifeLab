#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ExperimentSwarmCoordinationAblationHarness))]
public class ExperimentSwarmCoordinationAblationHarnessEditor : Editor
{
    ExperimentSwarmCoordinationAblationHarness _harness;

    void OnEnable()
    {
        _harness = (ExperimentSwarmCoordinationAblationHarness)target;
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
        var harness = (ExperimentSwarmCoordinationAblationHarness)target;
        DrawDefaultInspector();

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("EXP-SWARM-005b Coordination Ablation", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play mode, then Run All Tests. Compares None vs HiveBulletin vs LocalBroadcast " +
                "(and optional GlobalBroadcast oracle). Disable the legacy trail ablation harness on this object.",
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
            EditorGUILayout.HelpBox("Batch running…", MessageType.Warning);
            if (GUILayout.Button("Stop Batch"))
                harness.StopBatch();
        }

        DrawLiveStatus(harness.LiveStatus, harness.IsRunningBatch);

        SwarmFoodCoordinationAblationReport report = harness.LastReport;
        bool hasData = harness.LastNoneRuns.Count > 0 || harness.LastHiveRuns.Count > 0;
        if (!report.isComplete && !hasData && !harness.IsRunningBatch)
            return;

        EditorGUILayout.Space(8f);
        DrawVerdict(report);
        DrawConditionStats("None (baseline)", report.baselineNone, harness.LastNoneRuns);
        DrawConditionStats("Hive bulletin", report.hiveBulletin, harness.LastHiveRuns);
        DrawConditionStats("Local broadcast", report.localBroadcast, harness.LastLocalRuns);
        DrawConditionStats("Global oracle", report.globalBroadcast, harness.LastGlobalRuns);
    }

    static void DrawLiveStatus(SwarmCoordinationAblationLiveStatus live, bool isRunning)
    {
        if (!isRunning && live.completedRuns <= 0)
            return;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Live Batch Status", EditorStyles.boldLabel);

        if (isRunning)
        {
            EditorGUILayout.LabelField("Current run", $"{live.currentRunIndex} / {live.totalRuns}");
            EditorGUILayout.LabelField("Seed / mode", $"{live.currentSeed}  |  {live.currentMode.ToReportLabel()}");
            EditorGUILayout.LabelField("Food returned", live.liveFoodReturned.ToString());
            EditorGUILayout.LabelField("Coordination steps", live.liveCoordinationSteps.ToString());
            EditorGUILayout.LabelField("Signals emitted", live.liveSignalsEmitted.ToString());
            EditorGUILayout.LabelField("Efficiency", live.liveForagingEfficiency.ToString("F6"));
        }
    }

    static void DrawVerdict(SwarmFoodCoordinationAblationReport report)
    {
        if (!report.isComplete)
            return;

        MessageType messageType = report.verdict switch
        {
            SwarmFoodCoordinationVerdict.CoordinationWorking => MessageType.Info,
            SwarmFoodCoordinationVerdict.CoordinationNotWorking => MessageType.Warning,
            _ => MessageType.None
        };

        string title = report.verdict switch
        {
            SwarmFoodCoordinationVerdict.CoordinationWorking => "VERDICT: COORDINATION WORKING",
            SwarmFoodCoordinationVerdict.CoordinationNotWorking => "VERDICT: COORDINATION NOT WORKING",
            _ => "VERDICT: INCONCLUSIVE"
        };

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(report.verdictSummary, messageType);
        if (report.bestMode != SwarmFoodCoordinationMode.None)
        {
            EditorGUILayout.LabelField("Best mode", report.bestMode.ToReportLabel());
            EditorGUILayout.LabelField(
                "Primary metrics passed",
                $"{report.metricsPassed} / {report.metricsRequired}");
        }
    }

    static void DrawConditionStats(
        string label,
        SwarmFoodCoordinationConditionStats stats,
        System.Collections.Generic.IReadOnlyList<SwarmForagingEpisodeSnapshot> runs)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Runs", stats.runCount.ToString());
        EditorGUILayout.LabelField("Mean food returned", stats.meanFoodReturned.ToString("F1"));
        EditorGUILayout.LabelField("Mean efficiency", stats.meanForagingEfficiency.ToString("F6"));
        EditorGUILayout.LabelField("Mean coordination steps", stats.meanCoordinationInfluencedSteps.ToString("F1"));
        EditorGUILayout.LabelField("Mean signals emitted", stats.meanSignalsEmitted.ToString("F1"));

        if (runs != null && runs.Count > 0)
            EditorGUILayout.LabelField("Per-run food", FormatRunFood(runs));
    }

    static string FormatRunFood(System.Collections.Generic.IReadOnlyList<SwarmForagingEpisodeSnapshot> runs)
    {
        var parts = new System.Text.StringBuilder();
        for (int i = 0; i < runs.Count; i++)
        {
            if (i > 0)
                parts.Append(", ");

            parts.Append(runs[i].seed);
            parts.Append('=');
            parts.Append(runs[i].foodReturned);
        }

        return parts.ToString();
    }
}
#endif
