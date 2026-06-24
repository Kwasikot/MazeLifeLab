using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SwarmForagingAgentState
{
    Searching = 0,
    ReturningHome = 1
}

[DefaultExecutionOrder(150)]
public class ExperimentSwarm004Runner : MonoBehaviour, ISwarmExplorationField, ISwarmForagingField, ISwarmScentField
{
    static readonly Color SearchingColor = new Color(1f, 0.85f, 0.2f);
    static readonly Color HiveColor = new Color(0.15f, 0.45f, 1f);
    static readonly Color FoodColor = new Color(1f, 0.25f, 0.1f);

    static readonly Vector3[] ProbeDirections =
    {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
    };

    sealed class AgentRuntime
    {
        public SwarmFlightAgent Agent;
        public Renderer Renderer;
        public SwarmForagingAgentState State;
        public int FoodTargetIndex = -1;
        public int FoodReturned;
    }

    sealed class FoodSite
    {
        public Vector3 Position;
        public int Remaining;
        public bool Discovered;
        public Transform Visual;
    }

    [Header("Episode")]
    [SerializeField] int seed = 42;
    [SerializeField] int agentCount = 96;
    [SerializeField] int maxSteps = 7000;
    [SerializeField] bool autoStartOnPlay = true;
    [SerializeField] bool enableMetricsLogging = true;
    [SerializeField] string metricsCsvRelativePath = "results/experiment_swarm_004_foraging.csv";

    [Header("Arena")]
    [SerializeField] MazeGen mazeGen;
    [SerializeField] bool useMazeBounds = true;
    [SerializeField] Vector3 arenaCenter = new Vector3(0f, 15f, 0f);
    [SerializeField] Vector3 arenaSize = new Vector3(80f, 30f, 80f);
    [SerializeField] float mazeFlightMinHeight = 0.7f;
    [SerializeField] float mazeFlightBandHeight = 1.4f;
    [SerializeField] float agentScale = 0f;
    [SerializeField] int coverageGridResolution = 20;
    [SerializeField] bool drawArenaGizmos = true;
    [SerializeField] bool drawCoverageGizmos = true;
    [SerializeField] int maxCoverageGizmos = 512;

    [Header("Foraging")]
    [SerializeField] int foodSiteCount = 5;
    [SerializeField] int foodUnitsPerSite = 24;
    [SerializeField] float foodPickupRadius = 4f;
    [SerializeField] float foodDiscoveryRadius = 10f;
    [SerializeField] float hiveRadius = 8f;
    [SerializeField] float hiveVisualScale = 8f;
    [SerializeField] float foodVisualScale = 1.4f;
    [SerializeField] float foragingWeight = 18f;
    [SerializeField] float explorationWeight = 8f;
    [SerializeField] float explorationProbeDistance = 14f;

    [Header("Boids")]
    [SerializeField] float neighborRadius = 4f;
    [SerializeField] float separationRadius = 0.9f;
    [SerializeField] float maxSpeed = 10f;
    [SerializeField] float maxForce = 24f;
    [SerializeField] float separationWeight = 1.6f;
    [SerializeField] float alignmentWeight = 0.7f;
    [SerializeField] float cohesionWeight = 0.3f;
    [SerializeField] float wanderWeight = 1.8f;
    [SerializeField] float boundaryWeight = 4f;
    [SerializeField] float boundaryMargin = 8f;
    [SerializeField] float mazeWallAvoidanceDistance = 2.5f;
    [SerializeField] float mazeWallAvoidanceWeight = 10f;

    [Header("EXP-SWARM-005 Scent Trails")]
    [SerializeField] bool enableScentTrails;
    [SerializeField] float scentWeight = 10f;
    [SerializeField] float scentDecayPerStep = 0.985f;
    [SerializeField] float scentMaxStrength = 12f;
    [SerializeField] float returnTrailDeposit = 1.4f;
    [SerializeField] float foodDiscoveryDeposit = 2.5f;

    readonly List<AgentRuntime> _agents = new List<AgentRuntime>();
    readonly List<FoodSite> _foodSites = new List<FoodSite>();

    ExperimentSwarm004MetricsLogger _metricsLogger;
    Transform _agentsRoot;
    Transform _foodRoot;
    Transform _hiveVisual;
    System.Random _rng;
    Vector3 _activeArenaCenter;
    Vector3 _activeArenaSize;
    Vector3 _hivePosition;
    int[] _visitCounts;
    int _steps;
    int _foodDiscovered;
    int _foodReturned;
    int _timeToFirstFood = -1;
    int _totalVoxelSamples;
    int _revisitSamples;
    bool _isRunning;
    SwarmScentField _scentField;
    int _scentInfluencedSteps;
    SwarmForagingEpisodeSnapshot _lastSnapshot;
    bool _suppressMetricsLogging;

    public int ConfiguredAgentCount => agentCount;
    public int AgentCount => _agents.Count;
    public int Steps => _steps;
    public bool IsRunning => _isRunning;
    public int FoodDiscovered => _foodDiscovered;
    public int FoodReturned => _foodReturned;
    public int TimeToFirstFood => _timeToFirstFood;
    public int CarryingAgents { get; private set; }
    public float CoverageVolumePercent { get; private set; }
    public float RevisitRatio { get; private set; }
    public float ForagingEfficiency { get; private set; }
    public EpisodeTerminationReason TerminationReason { get; private set; } = EpisodeTerminationReason.None;
    public bool EnableScentTrails
    {
        get => enableScentTrails;
        set => enableScentTrails = value;
    }
    public int ScentDeposits => _scentField != null ? _scentField.TotalDeposits : 0;
    public int ScentInfluencedSteps => _scentInfluencedSteps;
    public SwarmForagingEpisodeSnapshot LastEpisodeSnapshot => _lastSnapshot;
    public bool AutoStartOnPlay
    {
        get => autoStartOnPlay;
        set => autoStartOnPlay = value;
    }

    void Awake()
    {
        if (mazeGen == null)
            mazeGen = GetComponent<MazeGen>();

        _metricsLogger = new ExperimentSwarm004MetricsLogger(metricsCsvRelativePath, enableMetricsLogging);
        ExperimentRunnerExclusivity.ActivateExclusive(this);
    }

    void OnValidate()
    {
        agentCount = Mathf.Max(2, agentCount);
        maxSteps = Mathf.Max(1, maxSteps);
        mazeFlightMinHeight = Mathf.Max(0f, mazeFlightMinHeight);
        mazeFlightBandHeight = Mathf.Max(0.5f, mazeFlightBandHeight);
        agentScale = Mathf.Max(0f, agentScale);
        coverageGridResolution = Mathf.Clamp(coverageGridResolution, 2, 64);
        maxCoverageGizmos = Mathf.Max(0, maxCoverageGizmos);
        foodSiteCount = Mathf.Clamp(foodSiteCount, 1, 64);
        foodUnitsPerSite = Mathf.Max(1, foodUnitsPerSite);
        foodPickupRadius = Mathf.Max(0.1f, foodPickupRadius);
        foodDiscoveryRadius = Mathf.Max(foodPickupRadius, foodDiscoveryRadius);
        hiveRadius = Mathf.Max(0.1f, hiveRadius);
        hiveVisualScale = Mathf.Max(0.1f, hiveVisualScale);
        foodVisualScale = Mathf.Max(0.1f, foodVisualScale);
        foragingWeight = Mathf.Max(0f, foragingWeight);
        explorationWeight = Mathf.Max(0f, explorationWeight);
        explorationProbeDistance = Mathf.Max(0.1f, explorationProbeDistance);
        neighborRadius = Mathf.Max(0.1f, neighborRadius);
        separationRadius = Mathf.Clamp(separationRadius, 0.05f, neighborRadius);
        maxSpeed = Mathf.Max(0.1f, maxSpeed);
        maxForce = Mathf.Max(0.1f, maxForce);
        separationWeight = Mathf.Max(0f, separationWeight);
        alignmentWeight = Mathf.Max(0f, alignmentWeight);
        cohesionWeight = Mathf.Max(0f, cohesionWeight);
        wanderWeight = Mathf.Max(0f, wanderWeight);
        boundaryWeight = Mathf.Max(0f, boundaryWeight);
        boundaryMargin = Mathf.Max(0.1f, boundaryMargin);
        mazeWallAvoidanceDistance = Mathf.Max(0f, mazeWallAvoidanceDistance);
        mazeWallAvoidanceWeight = Mathf.Max(0f, mazeWallAvoidanceWeight);
        scentWeight = Mathf.Max(0f, scentWeight);
        scentDecayPerStep = Mathf.Clamp(scentDecayPerStep, 0.8f, 1f);
        scentMaxStrength = Mathf.Max(0.5f, scentMaxStrength);
        returnTrailDeposit = Mathf.Max(0f, returnTrailDeposit);
        foodDiscoveryDeposit = Mathf.Max(0f, foodDiscoveryDeposit);
    }

    void OnEnable()
    {
        ExperimentRunnerExclusivity.ActivateExclusive(this);
    }

    void Start()
    {
        if (autoStartOnPlay)
            BeginEpisode();
    }

    void FixedUpdate()
    {
        if (!_isRunning || _agents.Count == 0)
            return;

        SwarmFlightSettings settings = BuildSettings();
        IReadOnlyList<SwarmFlightAgent> agentComponents = GetAgentComponents();
        for (int i = 0; i < _agents.Count; i++)
            _agents[i].Agent.ExecuteStep(agentComponents, settings, Time.fixedDeltaTime, _rng);

        UpdateForagingState();
        UpdateScentField();
        TrackCoverage();
        _steps++;

        if (_steps >= maxSteps || AllFoodCollected())
        {
            EndEpisode(AllFoodCollected() ? EpisodeTerminationReason.Success : EpisodeTerminationReason.Timeout);
        }
    }

    public void PrepareForBatchRun(int episodeSeed, bool scentTrailsEnabled, bool logMetricsToCsv = false)
    {
        seed = episodeSeed;
        enableScentTrails = scentTrailsEnabled;
        autoStartOnPlay = false;
        _suppressMetricsLogging = !logMetricsToCsv;
    }

    public IEnumerator RunEpisodeUntilComplete()
    {
        BeginEpisode();
        while (_isRunning)
            yield return new WaitForFixedUpdate();
    }

    [ContextMenu("Begin Episode")]
    public void BeginEpisode()
    {
        EndEpisodeWithoutLogging();
        _rng = new System.Random(seed);
        ResolveActiveArena();
        ResetMetrics();
        ResetCoverageField();
        ResetScentField();
        EnsureRoots();
        SpawnHive();
        SpawnFoodSites();
        SpawnAgents();
        TrackCoverage();
        EnsureTopDownCamera();
        _isRunning = true;

        Debug.Log(
            $"[EXP-SWARM-004] Started seed={seed} agents={_agents.Count} foodSites={_foodSites.Count} " +
            $"hive={_hivePosition} maxSteps={maxSteps}.");
    }

    [ContextMenu("End Episode")]
    public void EndEpisodeFromMenu()
    {
        EndEpisode(EpisodeTerminationReason.None);
    }

    void EndEpisode(EpisodeTerminationReason reason)
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        TerminationReason = reason;
        for (int i = 0; i < _agents.Count; i++)
            _agents[i].Agent.EndEpisode();

        if (!_suppressMetricsLogging)
        {
            _metricsLogger.LogEpisode(new ExperimentSwarm004EpisodeMetrics
            {
                seed = seed,
                agentCount = _agents.Count,
                foodSiteCount = _foodSites.Count,
                foodUnitsPerSite = foodUnitsPerSite,
                steps = _steps,
                foodDiscovered = FoodDiscovered,
                foodReturned = FoodReturned,
                timeToFirstFood = TimeToFirstFood,
                carryingAgents = CarryingAgents,
                coverageVolumePercent = CoverageVolumePercent,
                revisitRatio = RevisitRatio,
                foragingEfficiency = ForagingEfficiency,
                terminationReason = reason
            });

            Debug.Log(
                $"[EXP-SWARM-004] Ended reason={reason} steps={_steps} " +
                $"foodReturned={FoodReturned} foodDiscovered={FoodDiscovered} " +
                $"scentTrails={enableScentTrails} scentSteps={ScentInfluencedSteps}.");
        }

        _lastSnapshot = BuildEpisodeSnapshot(reason);
        _suppressMetricsLogging = false;
    }

    SwarmForagingEpisodeSnapshot BuildEpisodeSnapshot(EpisodeTerminationReason reason)
    {
        return new SwarmForagingEpisodeSnapshot
        {
            seed = seed,
            scentTrailsEnabled = enableScentTrails,
            steps = _steps,
            foodReturned = FoodReturned,
            foodDiscovered = FoodDiscovered,
            timeToFirstFood = TimeToFirstFood,
            foragingEfficiency = ForagingEfficiency,
            revisitRatio = RevisitRatio,
            coverageVolumePercent = CoverageVolumePercent,
            scentDeposits = ScentDeposits,
            scentInfluencedSteps = ScentInfluencedSteps,
            terminationReason = reason
        };
    }

    void EndEpisodeWithoutLogging()
    {
        _isRunning = false;
        for (int i = 0; i < _agents.Count; i++)
        {
            if (_agents[i].Agent != null)
                Destroy(_agents[i].Agent.gameObject);
        }

        DestroyChildren(_foodRoot);
        if (_hiveVisual != null)
            Destroy(_hiveVisual.gameObject);

        _agents.Clear();
        _foodSites.Clear();
    }

    void OnDisable()
    {
        EndEpisodeWithoutLogging();
    }

    void ResetMetrics()
    {
        _steps = 0;
        _foodDiscovered = 0;
        _foodReturned = 0;
        _timeToFirstFood = -1;
        _totalVoxelSamples = 0;
        _revisitSamples = 0;
        CarryingAgents = 0;
        CoverageVolumePercent = 0f;
        RevisitRatio = 0f;
        ForagingEfficiency = 0f;
        TerminationReason = EpisodeTerminationReason.None;
        _scentInfluencedSteps = 0;
    }

    void ResetScentField()
    {
        if (!enableScentTrails)
        {
            _scentField = null;
            return;
        }

        if (_scentField == null)
            _scentField = new SwarmScentField(scentDecayPerStep, scentMaxStrength);

        _scentField.Reset(_activeArenaCenter, _activeArenaSize, coverageGridResolution);
    }

    void ResetCoverageField()
    {
        _visitCounts = new int[coverageGridResolution * coverageGridResolution];
    }

    void EnsureRoots()
    {
        if (_agentsRoot == null || _agentsRoot.gameObject == null)
        {
            var agents = new GameObject("EXP-SWARM-004 Agents");
            agents.transform.SetParent(transform, false);
            _agentsRoot = agents.transform;
        }

        if (_foodRoot == null || _foodRoot.gameObject == null)
        {
            var food = new GameObject("EXP-SWARM-004 Food");
            food.transform.SetParent(transform, false);
            _foodRoot = food.transform;
        }
    }

    void SpawnHive()
    {
        _hivePosition = _activeArenaCenter;
        _hivePosition.y = mazeFlightMinHeight + 0.15f;

        GameObject hive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hive.name = "EXP-SWARM-004 Hive";
        hive.transform.SetParent(transform, true);
        hive.transform.position = _hivePosition;
        hive.transform.localScale = new Vector3(hiveVisualScale, 0.2f, hiveVisualScale);
        RemoveCollider(hive);
        ExperimentAgentVisuals.ApplyUnlitColor(hive.GetComponent<Renderer>(), HiveColor);
        _hiveVisual = hive.transform;
    }

    void SpawnFoodSites()
    {
        for (int i = 0; i < foodSiteCount; i++)
        {
            Vector3 position = RandomFoodPosition();
            var site = new FoodSite
            {
                Position = position,
                Remaining = foodUnitsPerSite,
                Discovered = false
            };

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = $"Swarm004Food_{i:00}";
            visual.transform.SetParent(_foodRoot, true);
            visual.transform.position = position;
            visual.transform.localScale = Vector3.one * foodVisualScale;
            RemoveCollider(visual);
            ExperimentAgentVisuals.ApplyUnlitColor(visual.GetComponent<Renderer>(), FoodColor);
            site.Visual = visual.transform;
            _foodSites.Add(site);

            Debug.Log($"[EXP-SWARM-004] Food site {i} position={position} units={foodUnitsPerSite}.");
        }
    }

    void SpawnAgents()
    {
        for (int i = 0; i < agentCount; i++)
        {
            GameObject agentObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            agentObject.name = $"Swarm004Forager_{i:00}";
            agentObject.transform.SetParent(_agentsRoot, true);
            agentObject.transform.position = RandomNearHivePosition();
            agentObject.transform.localScale = Vector3.one * ResolveAgentVisualScale();
            RemoveCollider(agentObject);

            Renderer renderer = agentObject.GetComponent<Renderer>();
            ExperimentAgentVisuals.ApplyUnlitColor(renderer, SearchingColor);

            SwarmFlightAgent agent = agentObject.AddComponent<SwarmFlightAgent>();
            Vector3 velocity = RandomUnitVector() * Mathf.Max(0.1f, maxSpeed * 0.65f);
            agent.BeginEpisode(i, velocity);
            _agents.Add(new AgentRuntime
            {
                Agent = agent,
                Renderer = renderer,
                State = SwarmForagingAgentState.Searching
            });
        }
    }

    void UpdateForagingState()
    {
        int carrying = 0;
        for (int i = 0; i < _agents.Count; i++)
        {
            AgentRuntime runtime = _agents[i];
            Vector3 position = runtime.Agent.transform.position;
            if (runtime.State == SwarmForagingAgentState.ReturningHome)
            {
                carrying++;
                if (HorizontalDistance(position, _hivePosition) <= hiveRadius)
                {
                    _foodReturned++;
                    runtime.FoodReturned++;
                    runtime.State = SwarmForagingAgentState.Searching;
                    runtime.FoodTargetIndex = -1;
                }
                continue;
            }

            int foodIndex = FindNearestAvailableFood(position, foodDiscoveryRadius);
            if (foodIndex >= 0)
            {
                FoodSite site = _foodSites[foodIndex];
                if (!site.Discovered)
                {
                    site.Discovered = true;
                    _foodDiscovered++;
                    if (_timeToFirstFood < 0)
                        _timeToFirstFood = _steps;
                    if (enableScentTrails && _scentField != null && foodDiscoveryDeposit > 0f)
                        _scentField.DepositAt(site.Position, foodDiscoveryDeposit);
                }

                runtime.FoodTargetIndex = foodIndex;
                if (HorizontalDistance(position, site.Position) <= foodPickupRadius && site.Remaining > 0)
                {
                    site.Remaining--;
                    runtime.State = SwarmForagingAgentState.ReturningHome;
                    carrying++;
                    UpdateFoodVisual(site);
                }
            }
        }

        CarryingAgents = carrying;
        ForagingEfficiency = _steps > 0 && _agents.Count > 0
            ? (float)_foodReturned / (_steps * _agents.Count)
            : 0f;
    }

    int FindNearestAvailableFood(Vector3 position, float maxDistance)
    {
        int best = -1;
        float bestDistance = maxDistance;
        for (int i = 0; i < _foodSites.Count; i++)
        {
            if (_foodSites[i].Remaining <= 0)
                continue;

            float distance = HorizontalDistance(position, _foodSites[i].Position);
            if (distance > bestDistance)
                continue;

            bestDistance = distance;
            best = i;
        }

        return best;
    }

    void UpdateScentField()
    {
        if (!enableScentTrails || _scentField == null)
            return;

        _scentField.DecayStep();
        for (int i = 0; i < _agents.Count; i++)
        {
            AgentRuntime runtime = _agents[i];
            Vector3 position = runtime.Agent.transform.position;
            if (runtime.State == SwarmForagingAgentState.ReturningHome && returnTrailDeposit > 0f)
                _scentField.DepositAt(position, returnTrailDeposit);

            if (runtime.Agent.LastUsedScentBias)
                _scentInfluencedSteps++;
        }
    }

    public bool TrySampleScentDirection(int agentIndex, Vector3 position, Vector3 velocity, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (!enableScentTrails || _scentField == null || scentWeight <= 0f)
            return false;

        if (agentIndex < 0 || agentIndex >= _agents.Count)
            return false;

        if (_agents[agentIndex].State != SwarmForagingAgentState.Searching)
            return false;

        return _scentField.TrySampleGradientDirection(position, out direction);
    }

    public bool TrySampleForagingDirection(int agentIndex, Vector3 position, Vector3 velocity, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (agentIndex < 0 || agentIndex >= _agents.Count)
            return false;

        AgentRuntime runtime = _agents[agentIndex];
        Vector3 target;
        if (runtime.State == SwarmForagingAgentState.ReturningHome)
        {
            target = _hivePosition;
        }
        else
        {
            int foodIndex = runtime.FoodTargetIndex >= 0 && runtime.FoodTargetIndex < _foodSites.Count &&
                            _foodSites[runtime.FoodTargetIndex].Remaining > 0
                ? runtime.FoodTargetIndex
                : FindNearestAvailableFood(position, foodDiscoveryRadius);
            if (foodIndex < 0)
                return false;

            target = _foodSites[foodIndex].Position;
            runtime.FoodTargetIndex = foodIndex;
        }

        direction = target - position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f;
    }

    public bool TrySampleNoveltyDirection(Vector3 position, Vector3 velocity, float probeDistance, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (_visitCounts == null || _visitCounts.Length == 0)
            return false;

        Vector3 bestDirection = Vector3.zero;
        int bestVisits = int.MaxValue;
        for (int i = 0; i < ProbeDirections.Length; i++)
        {
            Vector3 sample = ClampToArena(position + ProbeDirections[i] * probeDistance);
            int visits = VisitCountAt(sample);
            if (visits >= bestVisits)
                continue;

            bestVisits = visits;
            bestDirection = ProbeDirections[i];
        }

        direction = bestDirection;
        return direction.sqrMagnitude > 0.001f;
    }

    SwarmFlightSettings BuildSettings()
    {
        MazeGenerator generator = null;
        if (useMazeBounds)
            TryGetMazeGenerator(out generator);

        return new SwarmFlightSettings
        {
            ArenaCenter = _activeArenaCenter,
            ArenaSize = _activeArenaSize,
            Obstacles = null,
            ExplorationField = this,
            ForagingField = this,
            ScentField = enableScentTrails ? this : null,
            NeighborRadius = neighborRadius,
            SeparationRadius = separationRadius,
            MaxSpeed = maxSpeed,
            MaxForce = maxForce,
            SeparationWeight = separationWeight,
            AlignmentWeight = alignmentWeight,
            CohesionWeight = cohesionWeight,
            WanderWeight = wanderWeight,
            BoundaryWeight = boundaryWeight,
            BoundaryMargin = boundaryMargin,
            Maze = generator,
            MazeWallAvoidanceDistance = mazeWallAvoidanceDistance,
            MazeWallAvoidanceWeight = mazeWallAvoidanceWeight,
            ExplorationProbeDistance = explorationProbeDistance,
            ExplorationWeight = explorationWeight,
            ForagingWeight = foragingWeight,
            ScentWeight = enableScentTrails ? scentWeight : 0f
        };
    }

    void TrackCoverage()
    {
        if (_visitCounts == null)
            return;

        for (int i = 0; i < _agents.Count; i++)
        {
            int index = VoxelIndexAt(_agents[i].Agent.transform.position);
            if (index < 0 || index >= _visitCounts.Length)
                continue;

            _totalVoxelSamples++;
            if (_visitCounts[index] > 0)
                _revisitSamples++;
            _visitCounts[index]++;
        }

        int visited = 0;
        for (int i = 0; i < _visitCounts.Length; i++)
        {
            if (_visitCounts[i] > 0)
                visited++;
        }

        CoverageVolumePercent = 100f * visited / _visitCounts.Length;
        RevisitRatio = _totalVoxelSamples > 0 ? (float)_revisitSamples / _totalVoxelSamples : 0f;
    }

    int VisitCountAt(Vector3 worldPosition)
    {
        int index = VoxelIndexAt(worldPosition);
        if (index < 0 || _visitCounts == null || index >= _visitCounts.Length)
            return int.MaxValue;

        return _visitCounts[index];
    }

    int VoxelIndexAt(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - (_activeArenaCenter - _activeArenaSize * 0.5f);
        int x = Mathf.Clamp(Mathf.FloorToInt(local.x / _activeArenaSize.x * coverageGridResolution), 0, coverageGridResolution - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(local.z / _activeArenaSize.z * coverageGridResolution), 0, coverageGridResolution - 1);
        return x + coverageGridResolution * z;
    }

    void ResolveActiveArena()
    {
        if (useMazeBounds && TryGetMazeGenerator(out MazeGenerator generator))
        {
            float width = generator.Config.mazeWidthCells * generator.Config.cellSize;
            float depth = generator.Config.mazeHeightCells * generator.Config.cellSize;
            _activeArenaCenter = new Vector3(
                width * 0.5f,
                mazeFlightMinHeight + mazeFlightBandHeight * 0.5f,
                depth * 0.5f);
            _activeArenaSize = new Vector3(width, mazeFlightBandHeight, depth);
            return;
        }

        _activeArenaCenter = arenaCenter;
        _activeArenaSize = arenaSize;
    }

    bool TryGetMazeGenerator(out MazeGenerator generator)
    {
        if (mazeGen == null)
            mazeGen = GetComponent<MazeGen>();

        generator = mazeGen != null ? mazeGen.Generator : null;
        return generator != null;
    }

    Vector3 RandomFoodPosition()
    {
        Vector3 position = RandomArenaPosition();
        if (HorizontalDistance(position, _activeArenaCenter) < hiveRadius * 2f)
            position += Vector3.right * hiveRadius * 2f;
        position = ClampToArena(position);
        position.y = mazeFlightMinHeight + mazeFlightBandHeight * 0.35f;
        return position;
    }

    Vector3 RandomNearHivePosition()
    {
        float angle = (float)(_rng.NextDouble() * Mathf.PI * 2.0);
        float radius = (float)_rng.NextDouble() * hiveRadius;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        Vector3 position = _hivePosition + new Vector3(offset.x, 0.6f, offset.y);
        position.y = mazeFlightMinHeight + (float)_rng.NextDouble() * mazeFlightBandHeight;
        return ClampToArena(position);
    }

    Vector3 RandomArenaPosition()
    {
        if (useMazeBounds && TryGetMazeGenerator(out MazeGenerator generator))
        {
            int cellX = _rng.Next(generator.Config.mazeWidthCells);
            int cellY = _rng.Next(generator.Config.mazeHeightCells);
            Vector3 position = generator.GetCellCenterWorld(cellX, cellY);
            position.y = mazeFlightMinHeight + (float)_rng.NextDouble() * mazeFlightBandHeight;
            return position;
        }

        Vector3 half = _activeArenaSize * 0.5f;
        return new Vector3(
            _activeArenaCenter.x + ((float)_rng.NextDouble() * 2f - 1f) * half.x,
            _activeArenaCenter.y,
            _activeArenaCenter.z + ((float)_rng.NextDouble() * 2f - 1f) * half.z);
    }

    Vector3 ClampToArena(Vector3 position)
    {
        Vector3 half = _activeArenaSize * 0.5f;
        Vector3 min = _activeArenaCenter - half;
        Vector3 max = _activeArenaCenter + half;
        return new Vector3(
            Mathf.Clamp(position.x, min.x, max.x),
            Mathf.Clamp(position.y, min.y, max.y),
            Mathf.Clamp(position.z, min.z, max.z));
    }

    void EnsureTopDownCamera()
    {
        if (mazeGen == null)
            mazeGen = GetComponent<MazeGen>();

        Experiment001CameraFollow.TryConfigureMainCamera(mazeGen, showFullMaze: true);
    }

    float ResolveAgentVisualScale()
    {
        TryGetMazeGenerator(out MazeGenerator generator);
        return ExperimentAgentVisuals.ResolveSwarmSphereScale(generator, agentScale, agentCount);
    }

    Vector3 RandomUnitVector()
    {
        float x = (float)(_rng.NextDouble() * 2.0 - 1.0);
        float y = (float)(_rng.NextDouble() * 2.0 - 1.0);
        float z = (float)(_rng.NextDouble() * 2.0 - 1.0);
        Vector3 value = new Vector3(x, y, z);
        return value.sqrMagnitude > 0.001f ? value.normalized : Vector3.forward;
    }

    bool AllFoodCollected()
    {
        return _foodReturned >= foodSiteCount * foodUnitsPerSite;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static void RemoveCollider(GameObject target)
    {
        var collider = target.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    static void DestroyChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    void UpdateFoodVisual(FoodSite site)
    {
        if (site.Visual == null)
            return;

        float fraction = foodUnitsPerSite > 0 ? (float)site.Remaining / foodUnitsPerSite : 0f;
        site.Visual.localScale = Vector3.one * Mathf.Max(0.35f, foodVisualScale * fraction);
    }

    void OnDrawGizmos()
    {
        if (!drawArenaGizmos)
            return;

        Vector3 center = Application.isPlaying ? _activeArenaCenter : ResolvePreviewArenaCenter();
        Vector3 size = Application.isPlaying ? _activeArenaSize : ResolvePreviewArenaSize();
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
        Gizmos.DrawWireCube(center, size);

        if (Application.isPlaying)
            DrawForagingGizmos();
    }

    void DrawForagingGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < _foodSites.Count; i++)
        {
            Vector3 position = _foodSites[i].Position;
            Gizmos.DrawWireSphere(position, foodPickupRadius);
            Gizmos.DrawLine(position, position + Vector3.up * 4f);
        }

        Gizmos.color = HiveColor;
        Gizmos.DrawWireSphere(_hivePosition, hiveRadius);
        Gizmos.DrawLine(_hivePosition, _hivePosition + Vector3.up * 6f);
    }

    Vector3 ResolvePreviewArenaCenter()
    {
        if (useMazeBounds && mazeGen != null)
        {
            MazeSeedConfig config = mazeGen.Config;
            float width = config.mazeWidthCells * config.cellSize;
            float depth = config.mazeHeightCells * config.cellSize;
            return new Vector3(width * 0.5f, mazeFlightMinHeight + mazeFlightBandHeight * 0.5f, depth * 0.5f);
        }

        return arenaCenter;
    }

    Vector3 ResolvePreviewArenaSize()
    {
        if (useMazeBounds && mazeGen != null)
        {
            MazeSeedConfig config = mazeGen.Config;
            return new Vector3(config.mazeWidthCells * config.cellSize, mazeFlightBandHeight, config.mazeHeightCells * config.cellSize);
        }

        return arenaSize;
    }

    IReadOnlyList<SwarmFlightAgent> GetAgentComponents()
    {
        _agentComponentBuffer.Clear();
        for (int i = 0; i < _agents.Count; i++)
            _agentComponentBuffer.Add(_agents[i].Agent);
        return _agentComponentBuffer;
    }

    readonly List<SwarmFlightAgent> _agentComponentBuffer = new List<SwarmFlightAgent>();
}
