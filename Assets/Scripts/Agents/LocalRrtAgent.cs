using System.Collections.Generic;
using UnityEngine;

public class LocalRrtAgent : MonoBehaviour
{
    [SerializeField] int sensorRadiusCells = 3;
    [SerializeField] int rrtIterationsPerStep = 128;
    [SerializeField] float rrtGoalBias = 0.2f;
    [SerializeField] int replanIntervalSteps = 4;
    [SerializeField] float agentHeight = 0.5f;
    [SerializeField] bool drawRrtTree = true;
    [SerializeField] bool drawPlannedPath = true;
    [SerializeField] Color treeEdgeColor = new Color(0.2f, 1f, 0.35f, 0.95f);
    [SerializeField] Color pathEdgeColor = new Color(0.2f, 0.85f, 1f, 1f);

    readonly MazeLocalDiscoveryMap _map = new MazeLocalDiscoveryMap();
    readonly List<Vector2Int> _plannedPath = new List<Vector2Int>();
    readonly List<Vector2Int> _pathQueue = new List<Vector2Int>();
    readonly List<LocalRrtEdge> _treeEdges = new List<LocalRrtEdge>();

    LocalRrtTreeVisualizer _visualizer;
    MazeGenerator _truth;
    System.Random _rng;
    bool _enabled;

    int _cellX;
    int _cellY;
    int _goalCellX;
    int _goalCellY;
    int _stepsSinceReplan;
    int _totalRrtIterations;
    int _totalRrtNodesCreated;
    int _collisionCount;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public int CollisionCount => _collisionCount;
    public float CoveragePercent => _map.CoveragePercent();
    public int RrtIterations => _totalRrtIterations;
    public int RrtNodesCreated => _totalRrtNodesCreated;
    public bool LastPlanFound { get; private set; }
    public string ObservabilityMode => "incremental_map";

    public void BeginEpisode(
        int mazeSeed,
        MazeGenerator truth,
        int goalCellX,
        int goalCellY,
        float height)
    {
        _truth = truth;
        _rng = new System.Random(mazeSeed + 120301);
        _goalCellX = goalCellX;
        _goalCellY = goalCellY;
        agentHeight = height;
        _collisionCount = 0;
        _totalRrtIterations = 0;
        _totalRrtNodesCreated = 0;
        _stepsSinceReplan = replanIntervalSteps;
        LastPlanFound = false;
        _enabled = true;

        EnsureVisualizer();

        _map.Reset(truth.Config.mazeWidthCells, truth.Config.mazeHeightCells);

        if (!truth.TryWorldToCell(transform.position, out _cellX, out _cellY))
        {
            _cellX = 0;
            _cellY = 0;
        }

        Sense();
        Replan();
        SnapToGrid();

        Debug.Log(
            $"[LocalRrt] start cell=({_cellX},{_cellY}) goal=({_goalCellX},{_goalCellY}) " +
            $"coverage={CoveragePercent:F1}% treeEdges={_treeEdges.Count} sensorRadius={sensorRadiusCells}");
    }

    public void EndEpisode()
    {
        _enabled = false;
        _truth = null;
        _rng = null;
        _pathQueue.Clear();
        _plannedPath.Clear();
        _treeEdges.Clear();
        _visualizer?.Clear();
    }

    public void ExecuteStep()
    {
        if (!_enabled || _truth == null || _rng == null)
            return;

        Sense();
        _stepsSinceReplan++;

        if (_stepsSinceReplan >= replanIntervalSteps || _pathQueue.Count == 0)
            Replan();

        if (_pathQueue.Count > 0)
        {
            Vector2Int next = _pathQueue[0];
            if (next.x == _cellX && next.y == _cellY)
            {
                _pathQueue.RemoveAt(0);
                if (_pathQueue.Count > 0)
                    next = _pathQueue[0];
                else
                    return;
            }

            if (TryMoveToCell(next.x, next.y))
                _pathQueue.RemoveAt(0);
            else
                _collisionCount++;
        }
        else if (_map.TryFindStepTowardFrontier(_cellX, _cellY, _goalCellX, _goalCellY, out int frontierDirX, out int frontierDirZ))
        {
            if (!TryMoveByDirection(frontierDirX, frontierDirZ))
                _collisionCount++;
        }
        else if (_map.TryFindGreedyGoalStep(_cellX, _cellY, _goalCellX, _goalCellY, out int goalDirX, out int goalDirZ))
        {
            if (!TryMoveByDirection(goalDirX, goalDirZ))
                _collisionCount++;
        }
        else if (_map.TryFindFrontierMove(_cellX, _cellY, _rng, out int dirX, out int dirZ))
        {
            if (!TryMoveByDirection(dirX, dirZ))
                _collisionCount++;
        }
    }

    void EnsureVisualizer()
    {
        if (_visualizer != null)
            return;

        var legacyOnAgent = GetComponent<LocalRrtTreeVisualizer>();
        if (legacyOnAgent != null)
            Destroy(legacyOnAgent);

        GameObject existing = GameObject.Find("MazeRrtVisual");
        if (existing != null)
            _visualizer = existing.GetComponent<LocalRrtTreeVisualizer>();

        if (_visualizer != null)
            return;

        var visualObject = new GameObject("MazeRrtVisual");
        visualObject.transform.SetParent(null);
        visualObject.transform.position = Vector3.zero;
        visualObject.transform.rotation = Quaternion.identity;
        visualObject.transform.localScale = Vector3.one;
        _visualizer = visualObject.AddComponent<LocalRrtTreeVisualizer>();
    }

    void Sense()
    {
        _map.Sense(_truth, transform.position, sensorRadiusCells);
    }

    void Replan()
    {
        _stepsSinceReplan = 0;
        _plannedPath.Clear();
        _pathQueue.Clear();

        _totalRrtIterations += rrtIterationsPerStep;

        LastPlanFound = LocalRrtPlanner.TryFindPath(
            _map,
            _cellX,
            _cellY,
            _goalCellX,
            _goalCellY,
            rrtIterationsPerStep,
            rrtGoalBias,
            _rng,
            out List<Vector2Int> path,
            out List<LocalRrtEdge> treeEdges,
            out int nodesCreated);

        _treeEdges.Clear();
        if (treeEdges != null)
            _treeEdges.AddRange(treeEdges);

        _totalRrtNodesCreated = Mathf.Max(_totalRrtNodesCreated, nodesCreated);

        if (LastPlanFound && path != null && path.Count > 1)
        {
            _plannedPath.AddRange(path);
            for (int i = 1; i < path.Count; i++)
                _pathQueue.Add(path[i]);
        }

        RefreshVisualization();
    }

    void RefreshVisualization()
    {
        if (_visualizer == null || _truth == null)
            return;

        _visualizer.Rebuild(
            _truth,
            _treeEdges,
            _plannedPath,
            drawRrtTree,
            drawPlannedPath,
            treeEdgeColor,
            pathEdgeColor);
    }

    bool TryMoveToCell(int targetX, int targetY)
    {
        int dx = Mathf.Clamp(targetX - _cellX, -1, 1);
        int dz = Mathf.Clamp(targetY - _cellY, -1, 1);
        if (dx == 0 && dz == 0)
            return true;

        return TryMoveByDirection(dx, dz);
    }

    bool TryMoveByDirection(int dirX, int dirZ)
    {
        if (!_truth.IsPassageOpen(_cellX, _cellY, dirX, dirZ))
            return false;

        int nextX = _cellX + dirX;
        int nextY = _cellY + dirZ;
        if (!_truth.IsCellInBounds(nextX, nextY))
            return false;

        _cellX = nextX;
        _cellY = nextY;
        SnapToGrid();
        return true;
    }

    void SnapToGrid()
    {
        Vector3 center = _truth.GetCellCenterWorld(_cellX, _cellY);
        center.y = agentHeight;
        transform.position = center;

        if (_pathQueue.Count > 0)
        {
            Vector3 look = _truth.GetCellCenterWorld(_pathQueue[0].x, _pathQueue[0].y) - center;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(look.normalized);
        }
    }
}
