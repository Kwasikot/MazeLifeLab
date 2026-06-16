using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class Experiment004Runner : MonoBehaviour
{
    static readonly Color[] AgentColors =
    {
        new Color(0.2f, 0.85f, 1f),
        new Color(1f, 0.35f, 0.85f),
        new Color(1f, 0.85f, 0.2f),
        new Color(1f, 0.55f, 0.15f),
        new Color(0.55f, 1f, 0.35f),
        new Color(0.75f, 0.55f, 1f),
        new Color(0.35f, 1f, 0.85f),
        new Color(1f, 0.45f, 0.45f)
    };

    static readonly Color[] AgentTreeColors =
    {
        new Color(0.2f, 0.85f, 1f),
        new Color(1f, 0.35f, 0.85f),
        new Color(1f, 0.75f, 0.15f),
        new Color(1f, 0.45f, 0.45f),
        new Color(0.55f, 1f, 0.35f),
        new Color(0.75f, 0.55f, 1f),
        new Color(0.35f, 1f, 0.85f),
        new Color(0.95f, 0.95f, 0.95f)
    };

    [SerializeField] MazeGen mazeGen;
    [SerializeField] Experiment001Algorithm algorithm = Experiment001Algorithm.LocalRrt;
    [SerializeField] int agentCount = 2;
    [SerializeField] MazeCellIndex goalCell = new MazeCellIndex(19, 19);
    [SerializeField] int maxSteps = 5000;
    [SerializeField] float goalRadius = 2f;
    [SerializeField] float goalVisualScale = 8f;
    [SerializeField] float agentHeight = 0.5f;
    [SerializeField] Transform goalMarker;
    [SerializeField] bool autoStartOnPlay = true;
    [SerializeField] bool disableSingleAgentRunner = true;
    [SerializeField] bool enableMetricsLogging = true;
    [SerializeField] string metricsCsvRelativePath = "results/experiment_004_multi_agent.csv";
    [SerializeField] bool drawLocalRrtTreeForFirstAgentOnly = false;

    readonly List<AgentRuntime> _agents = new List<AgentRuntime>();
    readonly HashSet<int> _teamVisitedCells = new HashSet<int>();
    readonly Dictionary<int, int> _cellAgentMask = new Dictionary<int, int>();

    Vector3 _goalWorldPosition;
    int _steps;
    int _stepsToFirstGoal = -1;
    int _agentsAtGoal;
    int _overlapCellCount;
    int _totalCollisions;
    float _totalPathLength;
    float _teamCoveragePercent;
    float _overlapPercent;
    bool _isRunning;
    bool _anyAgentReachedGoal;
    MultiAgentMetricsLogger _metricsLogger;
    Transform _agentsRoot;
    bool _ignoreMazeRegenerated;
    bool _beginEpisodeInProgress;
    Coroutine _cameraSetupCoroutine;

    public Experiment001Algorithm Algorithm => algorithm;
    public int AgentCount => _agents.Count;
    public int Steps => _steps;
    public float TeamCoveragePercent => _teamCoveragePercent;
    public float OverlapPercent => _overlapPercent;
    public bool IsRunning => _isRunning;
    public bool Success { get; private set; }
    public EpisodeTerminationReason TerminationReason { get; private set; } = EpisodeTerminationReason.None;

    sealed class AgentRuntime
    {
        public Transform Transform;
        public Vector3 LastPosition;
        public RandomWalkAgent RandomWalk;
        public WallFollowerAgent WallFollower;
        public LocalRrtAgent LocalRrt;
        public bool ReachedGoal;
    }

    void Awake()
    {
        if (mazeGen == null)
            mazeGen = GetComponent<MazeGen>();

        _metricsLogger = new MultiAgentMetricsLogger(metricsCsvRelativePath, enableMetricsLogging);
        DisableSingleAgentRunner();
    }

    void DisableSingleAgentRunner()
    {
        if (!disableSingleAgentRunner)
            return;

        var singleAgentRunner = GetComponent<Experiment001Runner>();
        if (singleAgentRunner != null)
            singleAgentRunner.enabled = false;
    }

    void OnValidate()
    {
        agentCount = Mathf.Max(2, agentCount);
    }

    void OnEnable()
    {
        DisableSingleAgentRunner();
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
        if (autoStartOnPlay)
            StartCoroutine(StartEpisodeWhenReady());
    }

    void FixedUpdate()
    {
        if (!_isRunning || _agents.Count == 0)
            return;

        ExecuteAgentSteps();
        TrackTeamMetrics();

        _steps++;

        int atGoal = CountAgentsAtGoal();
        _agentsAtGoal = atGoal;

        if (atGoal > 0 && _stepsToFirstGoal < 0)
        {
            _stepsToFirstGoal = _steps;
            _anyAgentReachedGoal = true;
            Success = true;
        }

        if (_steps <= 1)
            return;

        if (atGoal >= _agents.Count)
        {
            EndEpisode(EpisodeTerminationReason.Success, success: true);
            return;
        }

        if (_steps >= maxSteps)
        {
            EndEpisode(
                _anyAgentReachedGoal ? EpisodeTerminationReason.Success : EpisodeTerminationReason.Timeout,
                success: _anyAgentReachedGoal);
        }
    }

    IEnumerator StartEpisodeWhenReady()
    {
        DisableSingleAgentRunner();
        CleanupLegacySingleAgentObjects();

        yield return null;

        if (!EnsureMazeReady())
            yield break;

        if (!_isRunning)
            BeginEpisode();
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
        if (_beginEpisodeInProgress)
            return;

        _beginEpisodeInProgress = true;
        try
        {
            BeginEpisodeInternal();
        }
        finally
        {
            _beginEpisodeInProgress = false;
        }
    }

    void BeginEpisodeInternal()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "[EXP-004] Experiment004Runner is disabled — enabling it so FixedUpdate can drive agents.");
            enabled = true;
        }

        if (!EnsureMazeReady())
            return;

        MazeGenerator generator = mazeGen.Generator;
        MazeCellIndex resolvedGoal = ResolveGoalCell(generator);
        List<MazeCellIndex> startCells = MultiAgentStartLayout.ResolveStartCells(
            agentCount,
            generator.Config.mazeWidthCells,
            generator.Config.mazeHeightCells,
            resolvedGoal);

        if (startCells.Count < agentCount)
        {
            Debug.LogError(
                $"[EXP-004] Could not resolve {agentCount} distinct start cells away from goal {resolvedGoal}.");
            EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
            return;
        }

        if (!ValidateEpisodeSetup(generator, resolvedGoal, startCells))
            return;

        EnsureGoalMarkerExists();
        EnsureAgentsExist(startCells, generator);

        _goalWorldPosition = generator.GetCellCenterWorld(resolvedGoal.x, resolvedGoal.y);
        _goalWorldPosition.y = agentHeight * 0.5f;
        goalMarker.SetParent(null, true);
        goalMarker.position = _goalWorldPosition;

        _steps = 0;
        _stepsToFirstGoal = -1;
        _agentsAtGoal = 0;
        _totalCollisions = 0;
        _totalPathLength = 0f;
        _teamCoveragePercent = 0f;
        _overlapPercent = 0f;
        _overlapCellCount = 0;
        _teamVisitedCells.Clear();
        _cellAgentMask.Clear();
        _isRunning = true;
        Success = false;
        _anyAgentReachedGoal = false;
        TerminationReason = EpisodeTerminationReason.None;

        ConfigureActiveAlgorithms(generator, resolvedGoal);
        EnsureCameraFollow(true);
        ScheduleCameraFollowRefresh(true);
        TrackTeamMetrics();

        Debug.Log(
            $"[EXP-004] episode started algorithm={algorithm} agents={_agents.Count} seed={generator.MazeSeed} " +
            $"starts={FormatStartCells(startCells)} goal={resolvedGoal} maxSteps={maxSteps}. " +
            "No communication between agents.");
        LogSpawnedAgents(startCells);
    }

    [ContextMenu("Reset Episode")]
    public void ResetEpisode()
    {
        BeginEpisode();
    }

    void ExecuteAgentSteps()
    {
        for (int i = 0; i < _agents.Count; i++)
        {
            AgentRuntime runtime = _agents[i];
            if (runtime.Transform == null)
                continue;

            switch (algorithm)
            {
                case Experiment001Algorithm.RandomWalk:
                    if (runtime.RandomWalk != null)
                        runtime.RandomWalk.ExecuteStep();
                    break;
                case Experiment001Algorithm.WallFollowerRight:
                case Experiment001Algorithm.WallFollowerLeft:
                    if (runtime.WallFollower != null)
                        runtime.WallFollower.ExecuteStep();
                    break;
                case Experiment001Algorithm.LocalRrt:
                    if (runtime.LocalRrt != null)
                        runtime.LocalRrt.ExecuteStep();
                    else
                        Debug.LogError($"[EXP-004] Agent_{i} is missing LocalRrtAgent.");
                    break;
            }
        }
    }

    void TrackTeamMetrics()
    {
        if (mazeGen == null || !mazeGen.HasGeneratedMaze)
            return;

        MazeGenerator generator = mazeGen.Generator;
        _totalCollisions = 0;
        _totalPathLength = 0f;

        for (int i = 0; i < _agents.Count; i++)
        {
            AgentRuntime runtime = _agents[i];
            if (runtime.Transform == null)
                continue;

            Vector3 current = runtime.Transform.position;
            _totalPathLength += HorizontalDistance(runtime.LastPosition, current);
            runtime.LastPosition = current;

            if (algorithm == Experiment001Algorithm.RandomWalk && runtime.RandomWalk != null)
                _totalCollisions += runtime.RandomWalk.CollisionCount;
            else if (IsWallFollowerAlgorithm() && runtime.WallFollower != null)
                _totalCollisions += runtime.WallFollower.CollisionCount;
            else if (algorithm == Experiment001Algorithm.LocalRrt && runtime.LocalRrt != null)
                _totalCollisions += runtime.LocalRrt.CollisionCount;

            if (!generator.TryWorldToCell(current, out int cellX, out int cellY))
                continue;

            int key = CellKey(cellX, cellY);
            _teamVisitedCells.Add(key);

            int agentBit = 1 << i;
            if (!_cellAgentMask.TryGetValue(key, out int mask))
                mask = 0;

            if ((mask & agentBit) == 0)
            {
                int newMask = mask | agentBit;
                _cellAgentMask[key] = newMask;
                if (CountAgentBits(newMask) == 2)
                    _overlapCellCount++;
            }

            runtime.ReachedGoal = HorizontalDistance(current, _goalWorldPosition) <= goalRadius;
        }

        int totalCells = generator.Config.mazeWidthCells * generator.Config.mazeHeightCells;
        _teamCoveragePercent = totalCells > 0
            ? 100f * _teamVisitedCells.Count / totalCells
            : 0f;
        _overlapPercent = totalCells > 0
            ? 100f * _overlapCellCount / totalCells
            : 0f;
    }

    int CountAgentsAtGoal()
    {
        int count = 0;
        for (int i = 0; i < _agents.Count; i++)
        {
            if (_agents[i].ReachedGoal)
                count++;
        }

        return count;
    }

    void ConfigureActiveAlgorithms(MazeGenerator generator, MazeCellIndex resolvedGoal)
    {
        int mazeSeed = generator.MazeSeed;

        for (int i = 0; i < _agents.Count; i++)
        {
            AgentRuntime runtime = _agents[i];
            runtime.RandomWalk?.EndEpisode();
            runtime.WallFollower?.EndEpisode();
            runtime.LocalRrt?.EndEpisode();

            switch (algorithm)
            {
                case Experiment001Algorithm.RandomWalk:
                    runtime.RandomWalk.BeginEpisode(mazeSeed, generator, i);
                    break;
                case Experiment001Algorithm.WallFollowerRight:
                    runtime.WallFollower.BeginEpisode(
                        WallFollowerAgent.HandRule.Right,
                        generator,
                        agentHeight);
                    break;
                case Experiment001Algorithm.WallFollowerLeft:
                    runtime.WallFollower.BeginEpisode(
                        WallFollowerAgent.HandRule.Left,
                        generator,
                        agentHeight);
                    break;
                case Experiment001Algorithm.LocalRrt:
                    bool drawTree = !drawLocalRrtTreeForFirstAgentOnly || i == 0;
                    Color treeColor = AgentTreeColors[i % AgentTreeColors.Length];
                    Color pathColor = new Color(treeColor.r * 0.85f, treeColor.g * 0.85f, treeColor.b * 0.85f, 1f);
                    runtime.LocalRrt.ConfigureVisualization(treeColor, pathColor);
                    runtime.LocalRrt.BeginEpisode(
                        mazeSeed,
                        generator,
                        resolvedGoal.x,
                        resolvedGoal.y,
                        agentHeight,
                        i,
                        drawTree);
                    break;
            }
        }
    }

    void DestroyExistingAgents()
    {
        for (int i = 0; i < _agents.Count; i++)
        {
            AgentRuntime runtime = _agents[i];
            runtime.RandomWalk?.EndEpisode();
            runtime.WallFollower?.EndEpisode();
            runtime.LocalRrt?.EndEpisode();

            if (runtime.Transform != null)
                DestroyGameObjectImmediate(runtime.Transform.gameObject);
        }

        _agents.Clear();

        if (_agentsRoot != null)
        {
            DestroyGameObjectImmediate(_agentsRoot.gameObject);
            _agentsRoot = null;
        }
    }

    void EnsureAgentsExist(List<MazeCellIndex> startCells, MazeGenerator generator)
    {
        DestroyExistingAgents();
        EnsureAgentsRoot();

        float scale = agentCount <= 2 ? 1.5f : agentCount <= 4 ? 1.2f : 1f;

        for (int i = 0; i < startCells.Count; i++)
        {
            var runtime = new AgentRuntime();
            var agentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            agentObject.name = $"Agent_{i}";
            agentObject.transform.SetParent(_agentsRoot, false);
            RemoveColliderIfPresent(agentObject);
            runtime.Transform = agentObject.transform;
            runtime.RandomWalk = agentObject.AddComponent<RandomWalkAgent>();
            runtime.WallFollower = agentObject.AddComponent<WallFollowerAgent>();
            runtime.LocalRrt = agentObject.AddComponent<LocalRrtAgent>();
            DisableShadows(agentObject);

            runtime.Transform.localScale = new Vector3(scale, scale * 0.66f, scale);
            var renderer = runtime.Transform.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = AgentColors[i % AgentColors.Length];

            Vector3 start = generator.GetCellCenterWorld(startCells[i].x, startCells[i].y);
            start.y = agentHeight;
            runtime.Transform.position = start;
            runtime.Transform.rotation = Quaternion.identity;
            runtime.LastPosition = start;
            runtime.ReachedGoal = false;
            _agents.Add(runtime);
        }
    }

    void LogSpawnedAgents(List<MazeCellIndex> startCells)
    {
        for (int i = 0; i < _agents.Count; i++)
        {
            Transform t = _agents[i].Transform;
            if (t == null)
                continue;

            Debug.Log(
                $"[EXP-004] Agent_{i} startCell={startCells[i]} world=({t.position.x:F1},{t.position.z:F1})");
        }
    }

    void CleanupLegacySingleAgentObjects()
    {
        DestroyGameObjectIfExists("Agent");
        DestroyGameObjectIfExists("MazeRrtVisual");

        for (int i = 0; i < 32; i++)
        {
            DestroyGameObjectIfExists($"Agent_{i}");
            DestroyGameObjectIfExists($"MazeRrtVisual_A{i}");
        }
    }

    static void DestroyGameObjectIfExists(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null)
            DestroyGameObjectImmediate(obj);
    }

    static void DestroyGameObjectImmediate(GameObject obj)
    {
        if (obj != null)
            DestroyImmediate(obj);
    }

    static string FormatStartCells(List<MazeCellIndex> startCells)
    {
        if (startCells == null || startCells.Count == 0)
            return "[]";

        var parts = new List<string>(startCells.Count);
        for (int i = 0; i < startCells.Count; i++)
            parts.Add($"({startCells[i].x},{startCells[i].y})");

        return string.Join(",", parts);
    }

    void EnsureAgentsRoot()
    {
        if (_agentsRoot != null)
            return;

        var existing = GameObject.Find("MultiAgentRoot");
        if (existing != null)
        {
            _agentsRoot = existing.transform;
            return;
        }

        var root = new GameObject("MultiAgentRoot");
        root.transform.SetParent(null);
        root.transform.position = Vector3.zero;
        _agentsRoot = root.transform;
    }

    void EndEpisode(EpisodeTerminationReason reason, bool success)
    {
        if (!_isRunning && reason != EpisodeTerminationReason.InvalidConfiguration)
            return;

        _isRunning = false;
        Success = success;
        TerminationReason = reason;

        for (int i = 0; i < _agents.Count; i++)
        {
            _agents[i].RandomWalk?.EndEpisode();
            _agents[i].WallFollower?.EndEpisode();
            _agents[i].LocalRrt?.EndEpisode();
        }

        WriteEpisodeMetrics(reason, success);

        Debug.LogWarning(
            $"[EXP-004] episode ended reason={reason} success={success} steps={_steps} agents={_agents.Count}");
        Debug.Log(
            $"[EXP-004] episode ended algorithm={algorithm} agents={_agents.Count} success={success} " +
            $"steps={_steps} stepsToFirstGoal={_stepsToFirstGoal} agentsAtGoal={_agentsAtGoal} " +
            $"collisions={_totalCollisions} pathLength={_totalPathLength:F1} " +
            $"teamCoverage={_teamCoveragePercent:F1}% overlap={_overlapPercent:F1}% " +
            $"reason={reason} seed={mazeGen.Generator.MazeSeed}");
    }

    void WriteEpisodeMetrics(EpisodeTerminationReason reason, bool success)
    {
        if (_metricsLogger == null || !enableMetricsLogging || mazeGen == null || !mazeGen.HasGeneratedMaze)
            return;

        _metricsLogger.LogEpisode(new Experiment004EpisodeMetrics
        {
            mazeSeed = mazeGen.Generator.MazeSeed,
            algorithm = algorithm.ToString(),
            agentCount = _agents.Count,
            success = success,
            steps = _steps,
            stepsToFirstGoal = _stepsToFirstGoal,
            agentsAtGoal = _agentsAtGoal,
            totalCollisions = _totalCollisions,
            totalPathLength = _totalPathLength,
            teamCoveragePercent = _teamCoveragePercent,
            overlapPercent = _overlapPercent,
            terminationReason = reason
        });
    }

    bool EnsureMazeReady()
    {
        if (mazeGen == null)
        {
            Debug.LogError("[EXP-004] MazeGen reference is missing.");
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

    bool ValidateEpisodeSetup(
        MazeGenerator generator,
        MazeCellIndex resolvedGoal,
        List<MazeCellIndex> startCells)
    {
        if (agentCount < 2)
        {
            Debug.LogError("[EXP-004] agentCount must be at least 2.");
            EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
            return false;
        }

        if (!generator.IsCellInBounds(resolvedGoal.x, resolvedGoal.y))
        {
            Debug.LogError($"[EXP-004] Invalid goal cell: {resolvedGoal}");
            EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
            return false;
        }

        var uniqueStarts = new HashSet<int>();
        for (int i = 0; i < startCells.Count; i++)
        {
            MazeCellIndex start = startCells[i];
            if (!generator.IsCellInBounds(start.x, start.y))
            {
                Debug.LogError($"[EXP-004] Invalid start cell for agent {i}: {start}");
                EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
                return false;
            }

            if (start.x == resolvedGoal.x && start.y == resolvedGoal.y)
            {
                Debug.LogError($"[EXP-004] Agent {i} start overlaps goal: {start}");
                EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
                return false;
            }

            int key = CellKey(start.x, start.y);
            if (!uniqueStarts.Add(key))
            {
                Debug.LogError($"[EXP-004] Duplicate start cell for agent {i}: {start}");
                EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
                return false;
            }
        }

        return true;
    }

    void EnsureGoalMarkerExists()
    {
        if (goalMarker == null)
        {
            var goalObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            goalObject.name = "GoalMarker";
            goalMarker = goalObject.transform;
        }

        float scale = goalVisualScale > 0f ? goalVisualScale : 8f;
        goalMarker.localScale = new Vector3(scale * 0.75f, scale * 0.5f, scale * 0.75f);
        RemoveColliderIfPresent(goalMarker.gameObject);

        var renderer = goalMarker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Unlit/Color"));
            renderer.material.color = new Color(0.1f, 1f, 0.2f);
            DisableShadows(goalMarker.gameObject);
        }
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
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[EXP-004] Main Camera not found; top-down view was not configured.");
            return;
        }

        var follow = cam.GetComponent<Experiment001CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<Experiment001CameraFollow>();

        follow.Height = 80f;
        follow.ConfigureForMaze(
            mazeGen.Config.mazeWidthCells,
            mazeGen.Config.mazeHeightCells,
            mazeGen.Config.cellSize);

        if (_agents.Count > 0 && _agents[0].Transform != null)
            follow.Target = _agents[0].Transform;

        if (showFullMaze)
            follow.ShowFullMazeView(snapImmediately: true);
        else
            follow.FollowAgent(snapImmediately: true);
    }

    bool IsWallFollowerAlgorithm()
    {
        return algorithm == Experiment001Algorithm.WallFollowerRight ||
               algorithm == Experiment001Algorithm.WallFollowerLeft;
    }

    static int CellKey(int cellX, int cellY)
    {
        return cellX * 1000 + cellY;
    }

    static int CountAgentBits(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            count += mask & 1;
            mask >>= 1;
        }

        return count;
    }

    static void DisableShadows(GameObject obj)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static void RemoveColliderIfPresent(GameObject obj)
    {
        var collider = obj.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
