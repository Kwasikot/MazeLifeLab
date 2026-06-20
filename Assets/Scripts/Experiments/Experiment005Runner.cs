using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(110)]
public class Experiment005Runner : MonoBehaviour
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
        new Color(1f, 0.45f, 0.45f),
        new Color(0.95f, 0.95f, 0.95f),
        new Color(0.6f, 0.4f, 0.2f),
        new Color(0.2f, 0.45f, 1f),
        new Color(0.85f, 0.2f, 0.55f)
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

    [SerializeField] Experiment005CommunicationMode communicationMode = Experiment005CommunicationMode.FrontierClaim;
    [SerializeField] float signalDecayPerStep = 0.995f;
    [SerializeField] float signalMaxCellStrength = 8f;
    [SerializeField] float signalReadTemperature = 1.35f;
    [SerializeField] float signalFollowChance = 0.45f;
    [SerializeField] float signalCrowdingPenalty = 0.65f;
    [SerializeField] float signalDepositNeighborSpread = 0.55f;
    [SerializeField] int signalDepositSpreadRadiusCells = 3;
    [SerializeField] float perAgentGoalBiasStep = 0.025f;
    [SerializeField] int frontierClaimRadiusCells = 10;
    [SerializeField] int frontierClaimTtlSteps = 36;
    [SerializeField] float frontierClaimOwnWeight = 0f;
    [SerializeField] float frontierClaimAvoidanceWeight = 10f;
    [SerializeField] float frontierClaimExpansionWeight = 0.35f;
    [SerializeField] float frontierClaimSectorWeight = 4f;
    [SerializeField] MazeGen mazeGen;
    [SerializeField] Experiment001Algorithm algorithm = Experiment001Algorithm.LocalRrt;
    [SerializeField] int agentCount = 2;
    [SerializeField] MazeCellIndex goalCell = new MazeCellIndex(-1, -1);
    [SerializeField] int maxSteps = 0;
    [SerializeField] float goalRadius = 2f;
    [SerializeField] float goalVisualScale = 8f;
    [SerializeField] float agentHeight = 0.5f;
    [SerializeField] Transform goalMarker;
    [SerializeField] bool autoStartOnPlay = true;
    [SerializeField] bool disableSingleAgentRunner = true;
    [SerializeField] bool enableMetricsLogging = true;
    [SerializeField] string metricsCsvRelativePath = "results/experiment_005_multi_agent_signals.csv";
    [SerializeField] bool drawLocalRrtTreeForFirstAgentOnly = false;
    [SerializeField] bool followAgentOnStart = false;
    [SerializeField] bool frameAllAgentsOnStart = true;
    [SerializeField] bool nearbyStartsForCommunication = true;
    [SerializeField] int communicationStartSpacingCells = 12;
    [SerializeField] bool showStigmergyField = true;
    [SerializeField] int stigmergyVisualRefreshIntervalSteps = 8;

    readonly List<AgentRuntime> _agents = new List<AgentRuntime>();
    readonly HashSet<int> _teamVisitedCells = new HashSet<int>();
    readonly Dictionary<int, int> _cellAgentMask = new Dictionary<int, int>();
    readonly List<MazeStigmergyVisualizer.AgentTrailLayer> _trailLayers = new List<MazeStigmergyVisualizer.AgentTrailLayer>();
    readonly List<MazeStigmergyVisualizer.ClaimLayer> _claimLayers = new List<MazeStigmergyVisualizer.ClaimLayer>();
    readonly List<FrontierClaimRecord> _claimRecordBuffer = new List<FrontierClaimRecord>();
    readonly List<Vector2Int> _trailCellBuffer = new List<Vector2Int>();

    Vector3 _goalWorldPosition;
    int _steps;
    int _episodeMaxSteps = 5000;
    int _stepsToFirstGoal = -1;
    int _agentsAtGoal;
    int _overlapCellCount;
    int _totalCollisions;
    float _totalPathLength;
    float _teamCoveragePercent;
    float _overlapPercent;
    bool _isRunning;
    bool _anyAgentReachedGoal;
    Experiment005MetricsLogger _metricsLogger;
    MazeStigmergyField _stigmergyField;
    MazeFrontierClaimField _frontierClaimField;
    MazeStigmergyVisualizer _stigmergyVisualizer;
    int _signalInfluencedSteps;
    int _frontierClaimsCreated;
    int _claimConflicts;
    int _claimedFrontierSteps;
    Transform _agentsRoot;
    bool _ignoreMazeRegenerated;
    bool _beginEpisodeInProgress;
    Coroutine _cameraSetupCoroutine;
    int _spawnedForAgentCount = -1;

    public Experiment005CommunicationMode CommunicationMode => communicationMode;
    public Experiment001Algorithm Algorithm => algorithm;
    public int ConfiguredAgentCount => agentCount;
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
        public AgentStigmergyController Stigmergy;
        public bool ReachedGoal;
    }

    void Awake()
    {
        if (mazeGen == null)
            mazeGen = GetComponent<MazeGen>();

        _metricsLogger = new Experiment005MetricsLogger(metricsCsvRelativePath, enableMetricsLogging);
        _stigmergyField = new MazeStigmergyField(signalDecayPerStep, signalMaxCellStrength);
        _frontierClaimField = new MazeFrontierClaimField();
        ExperimentRunnerExclusivity.ActivateExclusive(this);
    }

    void DisableOtherRunners()
    {
        ExperimentRunnerExclusivity.ActivateExclusive(this);
    }

    void DisableSingleAgentRunner()
    {
        DisableOtherRunners();
    }

    void OnValidate()
    {
        agentCount = Mathf.Clamp(agentCount, 2, 32);
        signalDepositSpreadRadiusCells = Mathf.Max(1, signalDepositSpreadRadiusCells);
        frontierClaimRadiusCells = Mathf.Max(1, frontierClaimRadiusCells);
        frontierClaimTtlSteps = Mathf.Max(1, frontierClaimTtlSteps);
        frontierClaimOwnWeight = Mathf.Max(0f, frontierClaimOwnWeight);
        frontierClaimAvoidanceWeight = Mathf.Max(0f, frontierClaimAvoidanceWeight);
        frontierClaimExpansionWeight = Mathf.Max(0f, frontierClaimExpansionWeight);
        frontierClaimSectorWeight = Mathf.Max(0f, frontierClaimSectorWeight);
        if (agentCount > 4 && !drawLocalRrtTreeForFirstAgentOnly)
            communicationStartSpacingCells = Mathf.Min(communicationStartSpacingCells, 4);

        if (Application.isPlaying && isActiveAndEnabled && enabled &&
            _spawnedForAgentCount >= 0 && _spawnedForAgentCount != agentCount)
        {
            BeginEpisode();
        }
    }

    void OnEnable()
    {
        ExperimentRunnerExclusivity.ActivateExclusive(this);
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

        if (communicationMode.DepositsSignals())
            _stigmergyField?.DecayStep();

        if (communicationMode == Experiment005CommunicationMode.FrontierClaim)
            _frontierClaimField?.Step();

        ExecuteAgentSteps();
        TrackTeamMetrics();

        if (showStigmergyField && communicationMode.DepositsSignals() &&
            stigmergyVisualRefreshIntervalSteps > 0 &&
            _steps % stigmergyVisualRefreshIntervalSteps == 0)
        {
            RefreshStigmergyVisual();
        }

        if (_steps > 0 && _steps % 1000 == 0)
        {
            LogProgressSnapshot();
        }

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

        if (_steps >= _episodeMaxSteps)
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
                "[EXP-005] Experiment004Runner is disabled — enabling it so FixedUpdate can drive agents.");
            enabled = true;
        }

        if (!EnsureMazeReady())
            return;

        MazeGenerator generator = mazeGen.Generator;
        MazeCellIndex resolvedGoal = ResolveGoalCell(generator);
        _episodeMaxSteps = MultiAgentStartLayout.ResolveMaxSteps(maxSteps, generator);
        List<MazeCellIndex> startCells = ResolveStartCells(generator, resolvedGoal);

        if (startCells.Count < agentCount)
        {
            Debug.LogError(
                $"[EXP-005] Could not resolve {agentCount} distinct start cells (got {startCells.Count}). " +
                $"Check Nearby Starts For Communication / maze size.");
            EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
            return;
        }

        if (startCells.Count > agentCount)
            startCells.RemoveRange(agentCount, startCells.Count - agentCount);

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
        _signalInfluencedSteps = 0;
        _frontierClaimsCreated = 0;
        _claimConflicts = 0;
        _claimedFrontierSteps = 0;
        TerminationReason = EpisodeTerminationReason.None;

        _stigmergyField = new MazeStigmergyField(signalDecayPerStep, signalMaxCellStrength);
        _stigmergyField.Reset(
            generator.Config.mazeWidthCells,
            generator.Config.mazeHeightCells);
        _frontierClaimField = new MazeFrontierClaimField();
        _frontierClaimField.Reset(
            generator.Config.mazeWidthCells,
            generator.Config.mazeHeightCells);
        EnsureStigmergyVisualizer();
        RefreshStigmergyVisual();

        ConfigureActiveAlgorithms(generator, resolvedGoal);
        EnsureCameraFollow();
        ScheduleCameraFollowRefresh();
        TrackTeamMetrics();

        if (agentCount > 4 && !drawLocalRrtTreeForFirstAgentOnly)
        {
            Debug.LogWarning(
                "[EXP-005] With many agents, enable Draw Local Rrt Tree For First Agent Only to avoid lag.");
        }

        Debug.Log(
            $"[EXP-005] episode started algorithm={algorithm} communication={communicationMode} agents={_agents.Count} " +
            $"seed={generator.MazeSeed} starts={FormatStartCells(startCells)} goal={resolvedGoal} maxSteps={_episodeMaxSteps}. " +
            GetCameraHint());
        LogSpawnedAgents(startCells);
        _spawnedForAgentCount = agentCount;

        if (_agents.Count != agentCount)
        {
            Debug.LogError(
                $"[EXP-005] Spawn mismatch: configured={agentCount} spawned={_agents.Count}. " +
                "Check Console for the active runner (EXP-006 may be overriding EXP-005).");
        }
    }

    List<MazeCellIndex> ResolveStartCells(MazeGenerator generator, MazeCellIndex resolvedGoal)
    {
        if (communicationMode == Experiment005CommunicationMode.FrontierClaim)
        {
            return MultiAgentStartLayout.ResolveDistributedRandomStarts(
                agentCount,
                generator.Config.mazeWidthCells,
                generator.Config.mazeHeightCells,
                resolvedGoal,
                generator.MazeSeed,
                marginCells: 3);
        }

        if (nearbyStartsForCommunication && communicationMode != Experiment005CommunicationMode.None)
        {
            return MultiAgentStartLayout.ResolveNearbyCommunicationStarts(
                agentCount,
                generator.Config.mazeWidthCells,
                generator.Config.mazeHeightCells,
                resolvedGoal,
                communicationStartSpacingCells);
        }

        return MultiAgentStartLayout.ResolveStartCells(
            agentCount,
            generator.Config.mazeWidthCells,
            generator.Config.mazeHeightCells,
            resolvedGoal);
    }

    string GetCameraHint()
    {
        if (frameAllAgentsOnStart && _agents.Count > 1)
            return "Camera framing all agents (press F for full maze).";
        if (followAgentOnStart)
            return "Camera following Agent_0 (press F for full maze).";
        return "Full maze view (press F to follow agents).";
    }

    void EnsureStigmergyVisualizer()
    {
        if (!showStigmergyField || !communicationMode.DepositsSignals())
            return;

        if (_stigmergyVisualizer == null)
        {
            var existing = GameObject.Find("StigmergyVisual");
            if (existing != null)
                _stigmergyVisualizer = existing.GetComponent<MazeStigmergyVisualizer>();

            if (_stigmergyVisualizer == null)
            {
                var visualObject = new GameObject("StigmergyVisual");
                visualObject.transform.SetParent(null);
                visualObject.transform.position = Vector3.zero;
                _stigmergyVisualizer = visualObject.AddComponent<MazeStigmergyVisualizer>();
            }
        }
    }

    void RefreshStigmergyVisual()
    {
        if (_stigmergyVisualizer == null || _stigmergyField == null || mazeGen == null || !mazeGen.HasGeneratedMaze)
            return;

        BuildAgentTrailLayers(_trailLayers);
        BuildClaimLayers(_claimLayers);

        _stigmergyVisualizer.Rebuild(
            _stigmergyField,
            mazeGen.Config.cellSize,
            signalMaxCellStrength,
            _trailLayers,
            _claimLayers);
    }

    void BuildAgentTrailLayers(List<MazeStigmergyVisualizer.AgentTrailLayer> layers)
    {
        layers.Clear();

        for (int i = 0; i < _agents.Count; i++)
        {
            AgentStigmergyController stigmergy = _agents[i].Stigmergy;
            if (stigmergy == null)
                continue;

            stigmergy.CopyTrailCells(_trailCellBuffer);
            if (_trailCellBuffer.Count == 0)
                continue;

            Color agentColor = AgentColors[i % AgentColors.Length];
            agentColor.a = 0.55f;

            layers.Add(new MazeStigmergyVisualizer.AgentTrailLayer
            {
                AgentIndex = i,
                Cells = new List<Vector2Int>(_trailCellBuffer),
                Color = agentColor
            });
        }
    }

    void BuildClaimLayers(List<MazeStigmergyVisualizer.ClaimLayer> layers)
    {
        layers.Clear();
        if (_frontierClaimField == null || communicationMode != Experiment005CommunicationMode.FrontierClaim)
            return;

        _frontierClaimField.CopyActiveClaims(_claimRecordBuffer);
        for (int i = 0; i < _claimRecordBuffer.Count; i++)
        {
            FrontierClaimRecord claim = _claimRecordBuffer[i];
            Color agentColor = AgentColors[claim.AgentIndex % AgentColors.Length];
            agentColor.a = 0.18f;
            layers.Add(new MazeStigmergyVisualizer.ClaimLayer
            {
                AgentIndex = claim.AgentIndex,
                Center = new Vector2Int(claim.X, claim.Y),
                RadiusCells = claim.RadiusCells,
                Color = agentColor
            });
        }
    }

    void LogProgressSnapshot()
    {
        var parts = new List<string>(_agents.Count);
        for (int i = 0; i < _agents.Count; i++)
        {
            AgentStigmergyController stigmergy = _agents[i].Stigmergy;
            if (stigmergy == null)
                continue;

            parts.Add($"A{i}:trail={stigmergy.TrailCellCount} dep={stigmergy.Deposits}");
        }

        Debug.Log(
            $"[EXP-005] step={_steps} coverage={_teamCoveragePercent:F1}% overlap={_overlapPercent:F1}% " +
            $"signals={_stigmergyField?.TotalDeposits ?? 0} signalSteps={_signalInfluencedSteps} " +
            $"claims={_frontierClaimsCreated} claimSteps={_claimedFrontierSteps} conflicts={_claimConflicts} " +
            $"agentsAtGoal={_agentsAtGoal}/{_agents.Count} [{string.Join(", ", parts)}]");
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
                        Debug.LogError($"[EXP-005] Agent_{i} is missing LocalRrtAgent.");
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
        _signalInfluencedSteps = 0;
        _frontierClaimsCreated = 0;
        _claimedFrontierSteps = 0;
        _claimConflicts = _frontierClaimField != null ? _frontierClaimField.ClaimConflicts : 0;

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
            {
                _totalCollisions += runtime.LocalRrt.CollisionCount;
                _signalInfluencedSteps += runtime.LocalRrt.SignalInfluencedSteps;
                _frontierClaimsCreated += runtime.LocalRrt.FrontierClaimsCreated;
                _claimedFrontierSteps += runtime.LocalRrt.ClaimGuidedSteps;
            }

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
        bool drawFirstOnly = drawLocalRrtTreeForFirstAgentOnly || agentCount > 4;
        int mazeWidth = generator.Config.mazeWidthCells;
        int mazeHeight = generator.Config.mazeHeightCells;

        for (int i = 0; i < _agents.Count; i++)
        {
            AgentRuntime runtime = _agents[i];
            runtime.RandomWalk?.EndEpisode();
            runtime.WallFollower?.EndEpisode();
            runtime.LocalRrt?.EndEpisode();
            runtime.Stigmergy?.EndEpisode();

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
                    var tuning = ExperimentMultiAgentPerformance.ResolveLocalRrtTuning(
                        i,
                        _agents.Count,
                        mazeWidth,
                        mazeHeight,
                        drawFirstOnly);
                    Color treeColor = AgentTreeColors[i % AgentTreeColors.Length];
                    Color pathColor = new Color(treeColor.r * 0.85f, treeColor.g * 0.85f, treeColor.b * 0.85f, 1f);
                    runtime.LocalRrt.ConfigurePerformance(
                        tuning.RrtIterationsPerStep,
                        tuning.ReplanIntervalSteps);
                    runtime.LocalRrt.ConfigureVisualization(treeColor, pathColor);
                    runtime.LocalRrt.ConfigureDrawing(tuning.DrawTree, tuning.DrawPath);
                    runtime.LocalRrt.BeginEpisode(
                        mazeSeed,
                        generator,
                        resolvedGoal.x,
                        resolvedGoal.y,
                        agentHeight,
                        i,
                        tuning.DrawTree || tuning.DrawPath);
                    runtime.LocalRrt.ConfigureExploration(0.16f + (i % 8) * perAgentGoalBiasStep);
                    runtime.Stigmergy.Configure(signalDepositNeighborSpread, signalDepositSpreadRadiusCells);
                    runtime.Stigmergy.BeginEpisode(
                        _stigmergyField,
                        communicationMode,
                        i,
                        mazeSeed,
                        generator);
                    runtime.LocalRrt.ConfigureStigmergy(
                        communicationMode.DepositsSignals() ? _stigmergyField : null,
                        communicationMode.DepositsSignals() ? runtime.Stigmergy : null,
                        communicationMode.EnablesSignalRead(),
                        communicationMode.IgnoreOwnSignalsWhenReading(),
                        signalFollowChance,
                        signalReadTemperature,
                        signalCrowdingPenalty,
                        signalMaxCellStrength);
                    runtime.LocalRrt.ConfigureFrontierClaims(
                        communicationMode == Experiment005CommunicationMode.FrontierClaim ? _frontierClaimField : null,
                        communicationMode == Experiment005CommunicationMode.FrontierClaim,
                        frontierClaimRadiusCells,
                        frontierClaimTtlSteps,
                        frontierClaimOwnWeight,
                        frontierClaimAvoidanceWeight,
                        frontierClaimExpansionWeight,
                        frontierClaimSectorWeight,
                        _agents.Count);
                    runtime.LocalRrt.RequestReplan();
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
            runtime.Stigmergy?.EndEpisode();

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

        float scale = ExperimentAgentVisuals.ResolveAgentCubeScale(generator, agentCount);
        float height = ExperimentAgentVisuals.ResolveAgentHeight(generator, agentHeight);

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
            runtime.Stigmergy = agentObject.AddComponent<AgentStigmergyController>();
            DisableShadows(agentObject);

            runtime.Transform.localScale = new Vector3(scale, scale * 0.66f, scale);
            ExperimentAgentVisuals.ApplyUnlitColor(
                runtime.Transform.GetComponent<Renderer>(),
                AgentColors[i % AgentColors.Length]);
            AddAgentHeadMarker(runtime.Transform, AgentColors[i % AgentColors.Length], scale);

            Vector3 start = generator.GetCellCenterWorld(startCells[i].x, startCells[i].y);
            start.y = height;
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
                $"[EXP-005] Agent_{i} startCell={startCells[i]} world=({t.position.x:F1},{t.position.z:F1})");
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
            _agents[i].Stigmergy?.EndEpisode();
        }

        WriteEpisodeMetrics(reason, success);

        int signalsDeposited = _stigmergyField != null ? _stigmergyField.TotalDeposits : 0;

        Debug.LogWarning(
            $"[EXP-005] episode ended reason={reason} success={success} steps={_steps} agents={_agents.Count}");
        Debug.Log(
            $"[EXP-005] episode ended algorithm={algorithm} communication={communicationMode} agents={_agents.Count} " +
            $"success={success} steps={_steps} stepsToFirstGoal={_stepsToFirstGoal} agentsAtGoal={_agentsAtGoal} " +
            $"collisions={_totalCollisions} pathLength={_totalPathLength:F1} signals={signalsDeposited} " +
            $"signalSteps={_signalInfluencedSteps} claims={_frontierClaimsCreated} claimConflicts={_claimConflicts} " +
            $"claimSteps={_claimedFrontierSteps} teamCoverage={_teamCoveragePercent:F1}% overlap={_overlapPercent:F1}% " +
            $"reason={reason} seed={mazeGen.Generator.MazeSeed}");
    }

    void WriteEpisodeMetrics(EpisodeTerminationReason reason, bool success)
    {
        if (_metricsLogger == null || !enableMetricsLogging || mazeGen == null || !mazeGen.HasGeneratedMaze)
            return;

        _metricsLogger.LogEpisode(new Experiment005EpisodeMetrics
        {
            mazeSeed = mazeGen.Generator.MazeSeed,
            algorithm = algorithm.ToString(),
            communicationMode = communicationMode.ToString(),
            agentCount = _agents.Count,
            success = success,
            steps = _steps,
            stepsToFirstGoal = _stepsToFirstGoal,
            agentsAtGoal = _agentsAtGoal,
            totalCollisions = _totalCollisions,
            totalPathLength = _totalPathLength,
            teamCoveragePercent = _teamCoveragePercent,
            overlapPercent = _overlapPercent,
            signalsDeposited = _stigmergyField != null ? _stigmergyField.TotalDeposits : 0,
            signalInfluencedSteps = _signalInfluencedSteps,
            frontierClaimsCreated = _frontierClaimsCreated,
            claimConflicts = _claimConflicts,
            claimedFrontierSteps = _claimedFrontierSteps,
            terminationReason = reason
        });
    }

    bool EnsureMazeReady()
    {
        if (mazeGen == null)
        {
            Debug.LogError("[EXP-005] MazeGen reference is missing.");
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
        return MultiAgentStartLayout.ResolveGoalCell(goalCell, generator);
    }

    bool ValidateEpisodeSetup(
        MazeGenerator generator,
        MazeCellIndex resolvedGoal,
        List<MazeCellIndex> startCells)
    {
        if (agentCount < 2)
        {
            Debug.LogError("[EXP-005] agentCount must be at least 2.");
            EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
            return false;
        }

        if (!generator.IsCellInBounds(resolvedGoal.x, resolvedGoal.y))
        {
            Debug.LogError($"[EXP-005] Invalid goal cell: {resolvedGoal}");
            EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
            return false;
        }

        var uniqueStarts = new HashSet<int>();
        for (int i = 0; i < startCells.Count; i++)
        {
            MazeCellIndex start = startCells[i];
            if (!generator.IsCellInBounds(start.x, start.y))
            {
                Debug.LogError($"[EXP-005] Invalid start cell for agent {i}: {start}");
                EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
                return false;
            }

            if (start.x == resolvedGoal.x && start.y == resolvedGoal.y)
            {
                Debug.LogError($"[EXP-005] Agent {i} start overlaps goal: {start}");
                EndEpisode(EpisodeTerminationReason.InvalidConfiguration, success: false);
                return false;
            }

            int key = CellKey(start.x, start.y);
            if (!uniqueStarts.Add(key))
            {
                Debug.LogError($"[EXP-005] Duplicate start cell for agent {i}: {start}");
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

    void ScheduleCameraFollowRefresh()
    {
        if (_cameraSetupCoroutine != null)
            StopCoroutine(_cameraSetupCoroutine);

        _cameraSetupCoroutine = StartCoroutine(RefreshCameraFollowNextFrame());
    }

    IEnumerator RefreshCameraFollowNextFrame()
    {
        yield return null;
        EnsureCameraFollow();
        _cameraSetupCoroutine = null;
    }

    void EnsureCameraFollow()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[EXP-005] Main Camera not found; top-down view was not configured.");
            return;
        }

        var follow = cam.GetComponent<Experiment001CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<Experiment001CameraFollow>();

        follow.ConfigureForMaze(
            mazeGen.Config.mazeWidthCells,
            mazeGen.Config.mazeHeightCells,
            mazeGen.Config.cellSize);

        if (_agents.Count > 0 && _agents[0].Transform != null)
            follow.Target = _agents[0].Transform;

        if (frameAllAgentsOnStart && _agents.Count > 1)
        {
            var transforms = new List<Transform>(_agents.Count);
            for (int i = 0; i < _agents.Count; i++)
            {
                if (_agents[i].Transform != null)
                    transforms.Add(_agents[i].Transform);
            }

            follow.FollowTeam(transforms, mazeGen.Config.cellSize, snapImmediately: true);
        }
        else if (followAgentOnStart)
        {
            follow.FollowAgent(snapImmediately: true);
        }
        else
        {
            follow.ShowFullMazeView(snapImmediately: true);
        }
    }

    static void AddAgentHeadMarker(Transform agentRoot, Color color, float agentScale)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "HeadMarker";
        marker.transform.SetParent(agentRoot, false);
        marker.transform.localPosition = new Vector3(0f, agentScale * 0.9f, 0f);
        marker.transform.localScale = Vector3.one * (agentScale * 0.35f);

        var collider = marker.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        ExperimentAgentVisuals.ApplyUnlitColor(marker.GetComponent<Renderer>(), color);
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
