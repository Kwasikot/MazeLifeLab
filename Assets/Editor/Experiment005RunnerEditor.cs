#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Experiment005Runner))]
public class Experiment005RunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var runner = (Experiment005Runner)target;
        var host = runner.gameObject;

        DrawRunnerConflictWarnings(host, runner);

        DrawDefaultInspector();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Configured Agents", runner.ConfiguredAgentCount.ToString());
        EditorGUILayout.LabelField("Spawned Agents (runtime)", runner.AgentCount.ToString());

        if (Application.isPlaying && runner.ConfiguredAgentCount != runner.AgentCount)
        {
            EditorGUILayout.HelpBox(
                "Configured count does not match spawned agents. Click Respawn Agents below " +
                "(or stop Play, save scene, and press Play again).",
                MessageType.Warning);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Respawn Agents (Begin Episode)", GUILayout.Height(28)))
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
        EditorGUILayout.LabelField("Communication", runner.CommunicationMode.ToString());
        EditorGUILayout.LabelField("Running", runner.IsRunning.ToString());
        EditorGUILayout.LabelField("Steps", runner.Steps.ToString());
        EditorGUILayout.LabelField("Team Coverage %", runner.TeamCoveragePercent.ToString("F1"));
        EditorGUILayout.LabelField("Overlap %", runner.OverlapPercent.ToString("F1"));
        EditorGUILayout.LabelField("Success", runner.Success.ToString());
        EditorGUILayout.LabelField("Termination", runner.TerminationReason.ToString());
    }

    static void DrawRunnerConflictWarnings(GameObject host, Experiment005Runner runner)
    {
        if (!runner.enabled)
        {
            EditorGUILayout.HelpBox(
                "Experiment 005 Runner is DISABLED — this component will not spawn agents. " +
                "Enable the checkbox at the top of this component.",
                MessageType.Error);
        }

        var runner006 = host.GetComponent<Experiment006Runner>();
        if (runner006 != null && runner006.enabled)
        {
            EditorGUILayout.HelpBox(
                "Experiment 006 Runner is also ENABLED. At Play, EXP-006 wins and EXP-005 is ignored. " +
                "Disable Experiment 006 Runner to use EXP-005 with your agent count here.",
                MessageType.Error);
        }

        var runner004 = host.GetComponent<Experiment004Runner>();
        if (runner004 != null && runner004.enabled && !runner006.enabled)
        {
            EditorGUILayout.HelpBox(
                "Experiment 004 Runner is enabled. Disable it so only EXP-005 runs.",
                MessageType.Warning);
        }
    }
}
#endif
