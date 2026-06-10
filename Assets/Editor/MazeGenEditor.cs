#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazeGen))]
public class MazeGenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        var mazeGen = (MazeGen)target;

        if (GUILayout.Button("Regenerate Maze", GUILayout.Height(28)))
        {
            mazeGen.Regenerate();
            EditorUtility.SetDirty(mazeGen);
            SceneView.RepaintAll();
        }

        if (mazeGen.HasGeneratedMaze)
        {
            EditorGUILayout.LabelField("Fingerprint", mazeGen.Fingerprint.ToString());
            EditorGUILayout.LabelField("Visible Walls", mazeGen.VisibleWallCount.ToString());
        }
    }
}
#endif
