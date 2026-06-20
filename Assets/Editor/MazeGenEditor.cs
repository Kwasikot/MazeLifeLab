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
            int totalCells = mazeGen.Config.mazeWidthCells * mazeGen.Config.mazeHeightCells;
            EditorGUILayout.LabelField(
                "Cells Carved",
                $"{mazeGen.VisitedCellCount} / {totalCells} " +
                $"({mazeGen.Config.mazeWidthCells}×{mazeGen.Config.mazeHeightCells})");
            EditorGUILayout.LabelField("Fingerprint", mazeGen.Fingerprint.ToString());
            EditorGUILayout.LabelField("Visible Walls", mazeGen.VisibleWallCount.ToString());

            if (mazeGen.VisitedCellCount < totalCells)
            {
                EditorGUILayout.HelpBox(
                    $"Only {mazeGen.VisitedCellCount} of {totalCells} cells were carved. " +
                    "The maze will appear as a small patch on a large grid.",
                    MessageType.Warning);
            }
        }
    }
}
#endif
