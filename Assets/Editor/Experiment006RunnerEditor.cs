#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Experiment006Runner))]
public class Experiment006RunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var runner = (Experiment006Runner)target;
        var host = runner.gameObject;

        DrawRunnerConflictWarnings(host, runner);

        DrawDefaultInspector();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Configured Agents", runner.ConfiguredAgentCount.ToString());
        EditorGUILayout.LabelField("Spawned Agents (runtime)", runner.AgentCount.ToString());

        if (Application.isPlaying && runner.ConfiguredAgentCount != runner.AgentCount)
        {
            EditorGUILayout.HelpBox(
                "Configured count does not match spawned agents. Click Begin Episode below " +
                "(or stop Play, save scene, and press Play again).",
                MessageType.Warning);
        }

        if (runner.ConfiguredAgentCount > 4)
        {
            EditorGUILayout.HelpBox(
                "10-agent Swarm RRT on a large maze is CPU-heavy. Enable Draw Local Rrt Tree For First Agent Only " +
                "(or keep agent count ≤ 4 for debugging). RRT iterations and replan interval scale down automatically.",
                MessageType.Info);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Begin Episode", GUILayout.Height(28)))
        {
            if (!runner.enabled)
                runner.enabled = true;

            ExperimentRunnerExclusivity.EnforceExclusiveRunners(host);
            runner.BeginEpisode();
            EditorUtility.SetDirty(runner);
        }

        if (GUILayout.Button("Reset Episode", GUILayout.Height(24)))
        {
            runner.ResetEpisode();
            EditorUtility.SetDirty(runner);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Algorithm", runner.Algorithm.ToString());
        EditorGUILayout.LabelField("Swarm Mode", runner.SwarmMode.ToString());
        EditorGUILayout.LabelField("Running", runner.IsRunning.ToString());
        EditorGUILayout.LabelField("Steps", runner.Steps.ToString());
        EditorGUILayout.LabelField("Team Coverage %", runner.TeamCoveragePercent.ToString("F1"));
        EditorGUILayout.LabelField("Overlap %", runner.OverlapPercent.ToString("F1"));
        EditorGUILayout.LabelField("Success", runner.Success.ToString());
        EditorGUILayout.LabelField("Termination", runner.TerminationReason.ToString());
    }

    static void DrawRunnerConflictWarnings(GameObject host, Experiment006Runner runner)
    {
        if (!runner.enabled)
        {
            EditorGUILayout.HelpBox(
                "Experiment 006 Runner is DISABLED — this component will not spawn agents. " +
                "Enable the checkbox at the top of this component.",
                MessageType.Error);
        }

        var runner005 = host.GetComponent<Experiment005Runner>();
        if (runner005 != null && runner005.enabled)
        {
            EditorGUILayout.HelpBox(
                "Experiment 005 Runner is also ENABLED. At Play, EXP-006 wins and EXP-005 is ignored. " +
                "Disable Experiment 005 Runner to avoid editing the wrong agent count.",
                MessageType.Error);
        }

        var runner004 = host.GetComponent<Experiment004Runner>();
        if (runner004 != null && runner004.enabled && !runner005.enabled)
        {
            EditorGUILayout.HelpBox(
                "Experiment 004 Runner is enabled. Disable it so only EXP-006 runs.",
                MessageType.Warning);
        }
    }
}
#endif
