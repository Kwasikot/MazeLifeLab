#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Experiment004Runner))]
public class Experiment004RunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        var runner = (Experiment004Runner)target;

        if (GUILayout.Button("Begin Episode", GUILayout.Height(24)))
        {
            if (!runner.enabled)
                runner.enabled = true;
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
        EditorGUILayout.LabelField("Agents", runner.AgentCount.ToString());
        EditorGUILayout.LabelField("Running", runner.IsRunning.ToString());
        EditorGUILayout.LabelField("Steps", runner.Steps.ToString());
        EditorGUILayout.LabelField("Team Coverage %", runner.TeamCoveragePercent.ToString("F1"));
        EditorGUILayout.LabelField("Overlap %", runner.OverlapPercent.ToString("F1"));
        EditorGUILayout.LabelField("Success", runner.Success.ToString());
        EditorGUILayout.LabelField("Termination", runner.TerminationReason.ToString());
    }
}
#endif
