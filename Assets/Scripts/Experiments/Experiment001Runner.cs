using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Experiment001Runner : MonoBehaviour
{
    [SerializeField] MazeGen mazeGen;
    [SerializeField] Experiment001Algorithm algorithm = Experiment001Algorithm.RandomWalk;
    [SerializeField] MazeCellIndex startCell = new MazeCellIndex(0, 0);
    [SerializeField] MazeCellIndex goalCell = new MazeCellIndex(19, 19);
    [SerializeField] int maxSteps = 5000;
    [SerializeField] float goalRadius = 2f;
    [SerializeField] float goalVisualScale = 8f;
    [SerializeField] float agentHeight = 0.5f;
    [SerializeField] Transform agent;
    [SerializeField] Transform goalMarker;
    [SerializeField] bool autoStartOnPlay = true;
    [SerializeField] bool enableMetricsLogging = true;
    [SerializeField] string metricsCsvRelativePath = "results/experiment_001_single_agent.csv";

    ManualAgentController _manualController;
    RandomWalkAgent _randomWalkAgent;
    WallFollowerAgent _wallFollowerAgent;
    LocalRrtAgent _localRrtAgent;
    Vector3 _goalWorldPosition;
    Vector3 _lastAgentPosition;
    int _steps;
    int _collisions;
    float _pathLength;
    float _coveragePercent;
    bool _isRunning;
    MetricsLogger _metricsLogger;
    readonly HashSet<int> _visitedCellKeys = new HashSet<int>();
    bool _ignoreMazeRegenerated;
    Coroutine _cameraSetupCoroutine;

    public Experiment001Algorithm Algorithm => algorithm;
    public int Steps => _steps;
    public int Collisions => _collisions;
    public float PathLength => _pathLength;
    public float CoveragePercent => _coveragePercent;
    public bool IsRunning => _isRunning;
    public bool Success { get; private set; }
    public EpisodeTerminationReason TerminationReason { get; private set; } = EpisodeTerminationReason.None;

    void Awake()
    {
        if (mazeGen == null)
            mazeGen = GetComponent<MazeGen>();

        _metricsLogger = new MetricsLogger(metricsCsvRelativePath, enableMetricsLogging);
    }

    void OnEnable()
    {
        if (mazeGen != null)
            mazeGen.OnMazeRegenerated += HandleMazeRegenerated;
    }

    void OnDisable()
    {
        if (mazeGen != null)
            mazeGen.OnMazeRegenerated -= HandleMazeRegenerated;
    }

    void Start()
    {
        if (autoStartOnPlay && !_isRunning)
            BeginEpisode();
    }

    void FixedUpdate()
    {
        if (!_isRunning || agent == null)
            return;

        ExecuteAgentStep();
        TrackMovementMetrics();

        _steps++;

        if (HorizontalDistance(agent.position, _goalWorldPosition) <= goalRadius)
        {
            EndEpisode(EpisodeTerminationReason.Success, success: true);
            return;
        }

        if (_steps >= maxSteps)
            EndEpisode(EpisodeTerminationReason.Timeout, success: false);
    }

    void ExecuteAgentStep()
    {
        switch (algorithm)
        {
            case Experiment001Algorithm.RandomWalk:
                _randomWalkAgent.ExecuteStep();
                break;
            case Experiment001Algorithm.WallFollowerRight:
            case Experiment001Algorithm.WallFollowerLeft:
                _wallFollowerAgent.ExecuteStep();
                break;
            case Experiment001Algorithm.LocalRrt:
                _localRrtAgent.ExecuteStep();
                break;
        }
    }

    void TrackMovementMetrics()
    {
        Vector3 current = agent.position;
        _pathLength += HorizontalDistance(_lastAgentPosition, current);
        _lastAgentPosition = current;
        TrackVisitedCell();

        if (algorithm == Experiment001Algorithm.RandomWalk && _randomWalkAgent != null)
            _collisions = _randomWalkAgent.CollisionCount;
        else if (IsWallFollowerAlgorithm() && _wallFollowerAgent != null)
            _collisions = _wallFollowerAgent.CollisionCount;
        else if (algorithm == Experiment001Algorithm.LocalRrt && _localRrtAgent != null)
            _collisions = _localRrtAgent.CollisionCount;
    }

    void TrackVisitedCell()
    {
        if (agent == null || mazeGen == null || !mazeGen.HasGeneratedMaze)
            return;

        MazeGenerator generator = mazeGen.Generator;
        if (!generator.TryWorldToCell(agent.position, out int cellX, out int cellY))
            return;

        _visitedCellKeys.Add(CellKey(cellX, cellY));
        int totalCells = generator.Config.mazeWidthCells * generator.Config.mazeHeightCells;
        _coveragePercent = totalCells > 0
            ? 100f * _visitedCellKeys.Count / totalCells
            : 0f;
    }

    static int CellKey(int cellX, int cellY)
    {
        return cellX * 1000 + cellY;
    }

    bool IsWallFollowerAlgorithm()
    {
        return algorithm == Experiment001Algorithm.WallFollowerRight ||
               algorithm == Experiment001Algorithm.WallFollowerLeft;
    }

    bool IsAutonomousAlgorithm()
    {
        return algorithm != Experiment001Algorithm.Manual;
    }

    void HandleMazeRegenerated()
    {
        if (!Application.isPlaying || _ignoreMazeRegenerated || !isActiveAndEnabled)
            return;

        if (_restartAfterRegenCoroutine != null)
            return;

        _restartAfterRegenCoroutine = StartCoroutine(RestartEpisodeAfterMazeRegenerated());
    }

    Coroutine _restartAfterRegenCoroutine;

    IEnumerator RestartEpisodeAfterMazeRegenerated()
    {
        yield return null;
        _restartAfterRegenCoroutine = null;
        if (isActiveAndEnabled)
            BeginEpisode();
    }

    [ContextMenu("Begin Episode")]
    public void BeginEpisode()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "[EXP-001] Experiment001Runner is disabled — enabling it so FixedUpdate can drive the agent.");
            enabled = true;
        }

        if (!EnsureMazeReady())
            return;

        MazeGenerator generator = mazeGen.Generator;
        MazeCellIndex resolvedGoal = ResolveGoalCell(generator);

        if (!ValidateEpisodeSetup(generator, resolvedGoal))
            return;

        EnsureAgentExists();
        EnsureGoalMarkerExists();
        EnsureRuntimeObjectsAreSeparate();

        _goalWorldPosition = generator.GetCellCenterWorld(resolvedGoal.x, resolvedGoal.y);
        Vector3 startPosition = generator.GetCellCenterWorld(startCell.x, startCell.y);
        startPosition.y = agentHeight;
        _goalWorldPosition.y = agentHeight * 0.5f;

        agent.SetParent(null, true);
        goalMarker.SetParent(null, true);

        agent.position = startPosition;
        agent.rotation = Quaternion.identity;
        goalMarker.position = _goalWorldPosition;

        _steps = 0;
        _collisions = 0;
        _pathLength = 0f;
        _coveragePercent = 0f;
        _visitedCellKeys.Clear();
        _lastAgentPosition = startPosition;
        TrackVisitedCell();
        _isRunning = true;
        Success = false;
        TerminationReason = EpisodeTerminationReason.None;

        ConfigureActiveAlgorithm(generator.MazeSeed);
        EnsureCameraFollow(true);
        ScheduleCameraFollowRefresh(true);

        if (IsWallFollowerAlgorithm())
        {
            Debug.Log(
                $"[EXP-001] wall-follower passages at start {startCell}: " +
                $"{generator.DescribeOpenPassages(startCell.x, startCell.y)} " +
                $"(seed={generator.MazeSeed}, fingerprint={generator.Fingerprint})");
        }

        Debug.Log(
            $"[EXP-001] episode started algorithm={algorithm} seed={generator.MazeSeed} " +
            $"start={startCell} ({startPosition.x:F1},{startPosition.z:F1}) " +
            $"goal={resolvedGoal} ({_goalWorldPosition.x:F1},{_goalWorldPosition.z:F1}) " +
            $"maxSteps={maxSteps}. Press F to toggle camera: full maze / follow agent.");
    }

    void ConfigureActiveAlgorithm(int mazeSeed)
    {
        SetManualControlEnabled(false);
        if (_randomWalkAgent != null)
            _randomWalkAgent.EndEpisode();
        if (_wallFollowerAgent != null)
            _wallFollowerAgent.EndEpisode();
        if (_localRrtAgent != null)
            _localRrtAgent.EndEpisode();

        switch (algorithm)
        {
            case Experiment001Algorithm.Manual:
                SetManualControlEnabled(true);
                break;
            case Experiment001Algorithm.RandomWalk:
                if (_randomWalkAgent != null)
                    _randomWalkAgent.BeginEpisode(mazeSeed, mazeGen.Generator);
                break;
            case Experiment001Algorithm.WallFollowerRight:
                if (_wallFollowerAgent != null)
                    _wallFollowerAgent.BeginEpisode(
                        WallFollowerAgent.HandRule.Right,
                        mazeGen.Generator,
                        agentHeight);
                break;
            case Experiment001Algorithm.WallFollowerLeft:
                if (_wallFollowerAgent != null)
                    _wallFollowerAgent.BeginEpisode(
                        WallFollowerAgent.HandRule.Left,
                        mazeGen.Generator,
                        agentHeight);
                break;
            case Experiment001Algorithm.LocalRrt:
                if (_localRrtAgent != null)
                {
                    MazeCellIndex resolvedGoal = ResolveGoalCell(mazeGen.Generator);
                    _localRrtAgent.BeginEpisode(
                        mazeSeed,
                        mazeGen.Generator,
                        resolvedGoal.x,
                        resolvedGoal.y,
                        agentHeight);
                }
                break;
        }
    }

    [ContextMenu("Reset Episode")]
    public void ResetEpisode()
    {
        BeginEpisode();
    }

    bool EnsureMazeReady()
    {
        if (mazeGen == null)
        {
            Debug.LogError("[EXP-001] MazeGen reference is missing.");
            return false;
        }

        if (!mazeGen.HasGeneratedMaze)
        {
            _ignoreMazeRegenerated = true;
            mazeGen.Regenerate(notifyListeners: false);
            _ignoreMazeRegenerated = false;
        }

        return mazeGen.HasGeneratedMaze;
    }

    MazeCellIndex ResolveGoalCell(MazeGenerator generator)
    {
        if (goalCell.x >= 0 && goalCell.y >= 0)
            return goalCell;

        int lastX = generator.Config.mazeWidthCells - 1;
        int lastY = generator.Config.mazeHeightCells - 1;
        return new MazeCellIndex(lastX, lastY);
    }

    bool ValidateEpisodeSetup(MazeGenerator generator, MazeCellIndex resolvedGoal)
    {
        if (!generator.IsCellInBounds(startCell.x, startCell.y) ||
            !generator.IsCellInBounds(resolvedGoal.x, resolvedGoal.y))
        {
            Debug.LogError(
                $"[EXP-001] Invalid start or goal cell. start={startCell} goal={resolvedGoal}");
            EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
            return false;
        }

        if (startCell.x == resolvedGoal.x && startCell.y == resolvedGoal.y)
        {
            Debug.LogError("[EXP-001] Start cell and goal cell must be different.");
            EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
            return false;
        }

        return true;
    }

    void EnsureAgentExists()
    {
        if (agent != null && !IsValidAgentTransform(agent))
        {
            Debug.LogWarning("[EXP-001] Invalid agent reference; creating a dedicated Agent object.");
            agent = null;
        }

        if (agent != null)
        {
            EnsureAgentComponents(agent.gameObject);
            return;
        }

        var agentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        agentObject.name = "Agent";
        agentObject.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
        RemoveColliderIfPresent(agentObject);
        agent = agentObject.transform;
        EnsureAgentComponents(agentObject);

        var renderer = agentObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.cyan;
            DisableShadows(agentObject);
        }
    }

    void EnsureAgentComponents(GameObject agentObject)
    {
        _manualController = agentObject.GetComponent<ManualAgentController>();
        if (_manualController == null)
            _manualController = agentObject.AddComponent<ManualAgentController>();

        _randomWalkAgent = agentObject.GetComponent<RandomWalkAgent>();
        if (_randomWalkAgent == null)
            _randomWalkAgent = agentObject.AddComponent<RandomWalkAgent>();

        _wallFollowerAgent = agentObject.GetComponent<WallFollowerAgent>();
        if (_wallFollowerAgent == null)
            _wallFollowerAgent = agentObject.AddComponent<WallFollowerAgent>();

        _localRrtAgent = agentObject.GetComponent<LocalRrtAgent>();
        if (_localRrtAgent == null)
            _localRrtAgent = agentObject.AddComponent<LocalRrtAgent>();

        RemoveColliderIfPresent(agentObject);
        DisableShadows(agentObject);
    }

    void EnsureGoalMarkerExists()
    {
        if (goalMarker != null && goalMarker == agent)
            goalMarker = null;

        if (goalMarker == null)
        {
            var goalObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            goalObject.name = "GoalMarker";
            goalMarker = goalObject.transform;
        }

        ApplyGoalVisual(goalMarker.gameObject);
    }

    void ApplyGoalVisual(GameObject goalObject)
    {
        float scale = goalVisualScale > 0f ? goalVisualScale : 8f;
        goalObject.transform.localScale = new Vector3(scale * 0.75f, scale * 0.5f, scale * 0.75f);
        RemoveColliderIfPresent(goalObject);

        var renderer = goalObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Unlit/Color"));
            renderer.material.color = new Color(0.1f, 1f, 0.2f);
            DisableShadows(goalObject);
        }
    }

    static void DisableShadows(GameObject obj)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void EnsureRuntimeObjectsAreSeparate()
    {
        if (agent == null || goalMarker == null)
            return;

        if (agent == goalMarker)
        {
            Debug.LogError("[EXP-001] Agent and goal cannot be the same object.");
            goalMarker = null;
            EnsureGoalMarkerExists();
        }

        if (goalMarker.IsChildOf(agent))
            goalMarker.SetParent(null, true);

        if (agent.IsChildOf(goalMarker))
            agent.SetParent(null, true);
    }

    static bool IsValidAgentTransform(Transform candidate)
    {
        if (candidate.GetComponent<MazeGen>() != null ||
            candidate.GetComponent<Experiment001Runner>() != null)
        {
            return false;
        }

        return candidate.name == "Agent" ||
               candidate.GetComponent<ManualAgentController>() != null ||
               candidate.GetComponent<RandomWalkAgent>() != null ||
               candidate.GetComponent<WallFollowerAgent>() != null ||
               candidate.GetComponent<LocalRrtAgent>() != null;
    }

    static void RemoveColliderIfPresent(GameObject obj)
    {
        var collider = obj.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    void ScheduleCameraFollowRefresh(bool showFullMaze)
    {
        if (_cameraSetupCoroutine != null)
            StopCoroutine(_cameraSetupCoroutine);

        _cameraSetupCoroutine = StartCoroutine(RefreshCameraFollowNextFrame(showFullMaze));
    }

    IEnumerator RefreshCameraFollowNextFrame(bool showFullMaze)
    {
        yield return null;
        EnsureCameraFollow(showFullMaze);
        _cameraSetupCoroutine = null;
    }

    void EnsureCameraFollow(bool showFullMaze)
    {
        if (agent == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        var follow = cam.GetComponent<Experiment001CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<Experiment001CameraFollow>();

        follow.Target = agent;
        follow.Height = 80f;
        follow.ConfigureForMaze(
            mazeGen.Config.mazeWidthCells,
            mazeGen.Config.mazeHeightCells,
            mazeGen.Config.cellSize);

        if (showFullMaze)
            follow.ShowFullMazeView(snapImmediately: true);
        else
            follow.FollowAgent(snapImmediately: true);
    }

    void EndEpisode(EpisodeTerminationReason reason, bool success)
    {
        if (!_isRunning && reason != EpisodeTerminationReason.InvalidConfiguration)
            return;

        _isRunning = false;
        Success = success;
        TerminationReason = reason;
        SetManualControlEnabled(false);

        int rrtNodes = 0;
        int rrtIterations = 0;
        if (algorithm == Experiment001Algorithm.LocalRrt && _localRrtAgent != null)
        {
            rrtNodes = _localRrtAgent.RrtNodesCreated;
            rrtIterations = _localRrtAgent.RrtIterations;
        }

        if (_randomWalkAgent != null)
            _randomWalkAgent.EndEpisode();
        if (_wallFollowerAgent != null)
            _wallFollowerAgent.EndEpisode();
        if (_localRrtAgent != null)
            _localRrtAgent.EndEpisode();

        WriteEpisodeMetrics(reason, success);

        if (algorithm == Experiment001Algorithm.LocalRrt)
        {
            Debug.Log(
                $"[EXP-003] episode ended algorithm=LocalRrt success={success} steps={_steps} " +
                $"collisions={_collisions} pathLength={_pathLength:F1} coverage={_coveragePercent:F1}% " +
                $"rrtNodes={rrtNodes} rrtIterations={rrtIterations} " +
                $"reason={reason} seed={mazeGen.Generator.MazeSeed}");
            return;
        }

        Debug.Log(
            $"[EXP-001] episode ended algorithm={algorithm} success={success} steps={_steps} " +
            $"collisions={_collisions} pathLength={_pathLength:F1} coverage={_coveragePercent:F1}% " +
            $"reason={reason} seed={mazeGen.Generator.MazeSeed}");
    }

    void WriteEpisodeMetrics(EpisodeTerminationReason reason, bool success)
    {
        if (_metricsLogger == null || !enableMetricsLogging || mazeGen == null || !mazeGen.HasGeneratedMaze)
            return;

        _metricsLogger.LogEpisode(new Experiment001EpisodeMetrics
        {
            mazeSeed = mazeGen.Generator.MazeSeed,
            algorithm = algorithm.ToString(),
            success = success,
            steps = _steps,
            collisions = _collisions,
            pathLength = _pathLength,
            coveragePercent = _coveragePercent,
            terminationReason = reason
        });
    }

    void SetManualControlEnabled(bool enabled)
    {
        if (_manualController != null)
            _manualController.ControlEnabled = enabled;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void OnDrawGizmosSelected()
    {
        if (mazeGen == null || !mazeGen.HasGeneratedMaze)
            return;

        MazeGenerator generator = mazeGen.Generator;
        MazeCellIndex resolvedGoal = ResolveGoalCell(generator);

        Gizmos.color = Color.blue;
        Vector3 start = generator.GetCellCenterWorld(startCell.x, startCell.y);
        start.y = agentHeight;
        Gizmos.DrawWireSphere(start, 0.75f);

        Gizmos.color = Color.green;
        Vector3 goal = generator.GetCellCenterWorld(resolvedGoal.x, resolvedGoal.y);
        goal.y = agentHeight * 0.5f;
        Gizmos.DrawWireSphere(goal, goalRadius);
    }
}
