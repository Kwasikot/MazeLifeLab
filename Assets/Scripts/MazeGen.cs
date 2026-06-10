using System;
using UnityEngine;

public class MazeGen : MonoBehaviour
{
    [SerializeField] MazeSeedConfig config = new MazeSeedConfig();

    MazeGenerator _generator;
    MazeWallVisualizer _wallVisualizer;
    Transform _wallsVisualRoot;

    public event Action OnMazeRegenerated;

    public MazeSeedConfig Config => config;
    public MazeGenerator Generator => _generator;
    public int Fingerprint => _generator != null ? _generator.Fingerprint : 0;
    public int VisibleWallCount => _generator != null ? _generator.VisibleWallCount : 0;
    public bool HasGeneratedMaze => _generator != null;

    void Awake()
    {
        EnsureWallVisualizerRoot();
    }

    void Start()
    {
        Regenerate();
    }

    [ContextMenu("Regenerate Maze")]
    public void Regenerate()
    {
        _generator = new MazeGenerator(config);
        _generator.Generate();
        RebuildWallVisuals();

        Debug.Log(
            $"[MazeGen] EXP-001 seed={config.mazeSeed} " +
            $"fingerprint={_generator.Fingerprint} visibleWalls={_generator.VisibleWallCount}");

        OnMazeRegenerated?.Invoke();
    }

    void EnsureWallVisualizerRoot()
    {
        if (_wallsVisualRoot != null)
            return;

        var existing = GameObject.Find("MazeWallsVisual");
        if (existing != null)
        {
            _wallsVisualRoot = existing.transform;
            _wallVisualizer = _wallsVisualRoot.GetComponent<MazeWallVisualizer>();
            if (_wallVisualizer == null)
                _wallVisualizer = _wallsVisualRoot.gameObject.AddComponent<MazeWallVisualizer>();
            return;
        }

        var visualObject = new GameObject("MazeWallsVisual");
        visualObject.transform.SetParent(null);
        visualObject.transform.position = Vector3.zero;
        visualObject.transform.rotation = Quaternion.identity;
        visualObject.transform.localScale = Vector3.one;
        _wallsVisualRoot = visualObject.transform;
        _wallVisualizer = visualObject.AddComponent<MazeWallVisualizer>();
    }

    void RebuildWallVisuals()
    {
        EnsureWallVisualizerRoot();

        if (_wallVisualizer != null && _generator != null)
            _wallVisualizer.Rebuild(_generator.WallsMap);
    }

    void OnDrawGizmos()
    {
        if (_generator == null)
            return;

        Gizmos.color = Color.red;
        foreach (var kv in _generator.WallsMap)
        {
            if (kv.Value.bVisible)
                Gizmos.DrawLine(kv.Value.A, kv.Value.B);
        }
    }
}
