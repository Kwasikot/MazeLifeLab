using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class MazeGen : MonoBehaviour
{
    [SerializeField] MazeSeedConfig config = new MazeSeedConfig();
    [SerializeField] bool autoConfigureCamera = true;
    [SerializeField] bool autoAlignFloorPlane = true;

    MazeGenerator _generator;
    MazeWallVisualizer _wallVisualizer;
    Transform _wallsVisualRoot;

    public event Action OnMazeRegenerated;

    public MazeSeedConfig Config => config;
    public MazeGenerator Generator => _generator;
    public int Fingerprint => _generator != null ? _generator.Fingerprint : 0;
    public int VisibleWallCount => _generator != null ? _generator.VisibleWallCount : 0;
    public int VisitedCellCount => _generator != null ? _generator.VisitedCellCount : 0;
    public bool HasGeneratedMaze => _generator != null;

    void Awake()
    {
        ExperimentRunnerExclusivity.EnforceExclusiveRunners(gameObject);
        EnsureWallVisualizerRoot();
    }

    void Start()
    {
        Regenerate(notifyListeners: false);
    }

    [ContextMenu("Regenerate Maze")]
    public void Regenerate()
    {
        Regenerate(notifyListeners: true);
    }

    public void Regenerate(bool notifyListeners)
    {
        _generator = new MazeGenerator(config);
        _generator.Generate();

        int totalCells = config.mazeWidthCells * config.mazeHeightCells;
        Debug.Log(
            $"[MazeGen] carved {_generator.VisitedCellCount}/{totalCells} cells " +
            $"({config.mazeWidthCells}x{config.mazeHeightCells}, seed={config.mazeSeed}, " +
            $"visibleWalls={_generator.VisibleWallCount})");

        RebuildWallVisuals();
        AlignFloorPlane();
        AlignDirectionalLight();
        if (autoConfigureCamera)
            StartCoroutine(ConfigureCameraWhenReady());

        if (notifyListeners)
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
            _wallVisualizer.Rebuild(_generator.WallsMap, config.cellSize);
    }

    void AlignFloorPlane()
    {
        if (!autoAlignFloorPlane)
            return;

        GameObject plane = GameObject.Find("Plane");
        if (plane == null)
            return;

        float mazeWidth = config.mazeWidthCells * config.cellSize;
        float mazeHeight = config.mazeHeightCells * config.cellSize;
        float scale = Mathf.Max(mazeWidth, mazeHeight) / 10f;

        plane.transform.position = new Vector3(mazeWidth * 0.5f, 0f, mazeHeight * 0.5f);
        plane.transform.localScale = new Vector3(scale, 1f, scale);
    }

    IEnumerator ConfigureCameraWhenReady()
    {
        Experiment001CameraFollow.TryConfigureMainCamera(this, showFullMaze: true);
        yield return null;
        Experiment001CameraFollow.TryConfigureMainCamera(this, showFullMaze: true);
    }

    void AlignDirectionalLight()
    {
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            GameObject lightObject = GameObject.Find("Directional Light");
            if (lightObject != null)
                sun = lightObject.GetComponent<Light>();
        }

        if (sun == null)
            return;

        float mazeWidth = config.mazeWidthCells * config.cellSize;
        float mazeHeight = config.mazeHeightCells * config.cellSize;
        float halfExtent = Mathf.Max(mazeWidth, mazeHeight) * 0.5f;

        sun.transform.position = new Vector3(mazeWidth * 0.5f, halfExtent * 0.5f, mazeHeight * 0.5f);
    }
}
