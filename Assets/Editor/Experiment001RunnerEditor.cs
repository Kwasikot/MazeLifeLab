#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Experiment001Runner))]
public class Experiment001RunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        var runner = (Experiment001Runner)target;

        if (GUILayout.Button("Begin Episode", GUILayout.Height(24)))
        {
            runner.BeginEpisode();
            EditorUtility.SetDirty(runner);
        }

        if (GUILayout.Button("Reset Episode", GUILayout.Height(24)))
        {
            runner.ResetEpisode();
            EditorUtility.SetDirty(runner);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Running", runner.IsRunning.ToString());
        EditorGUILayout.LabelField("Steps", runner.Steps.ToString());
        EditorGUILayout.LabelField("Success", runner.Success.ToString());
        EditorGUILayout.LabelField("Termination", runner.TerminationReason.ToString());
    }
}
#endif
