using System.Collections.Generic;
using UnityEngine;

public enum ExperimentSwarm003Mode
{
    PlainBoids = 0,
    FruitFlySearch = 1
}

[DefaultExecutionOrder(140)]
public class ExperimentSwarm003Runner : MonoBehaviour, ISwarmExplorationField
{
    static readonly Color[] AgentColors =
    {
        new Color(1f, 0.85f, 0.2f),
        new Color(0.2f, 0.85f, 1f),
        new Color(1f, 0.45f, 0.65f),
        new Color(0.55f, 1f, 0.35f),
        new Color(0.75f, 0.55f, 1f),
        new Color(1f, 0.55f, 0.15f)
    };

    static readonly Vector3[] ProbeDirections =
    {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right,
        Vector3.up,
        Vector3.down
    };

    [Header("Episode")]
    [SerializeField] ExperimentSwarm003Mode explorationMode = ExperimentSwarm003Mode.FruitFlySearch;
    [SerializeField] int seed = 42;
    [SerializeField] int agentCount = 96;
    [SerializeField] int maxSteps = 5000;
    [SerializeField] bool autoStartOnPlay = true;
    [SerializeField] bool enableMetricsLogging = true;
    [SerializeField] string metricsCsvRelativePath = "results/experiment_swarm_003_exploration.csv";

    [Header("Arena")]
    [SerializeField] MazeGen mazeGen;
    [SerializeField] bool useMazeBounds = true;
    [SerializeField] Vector3 arenaCenter = new Vector3(0f, 15f, 0f);
    [SerializeField] Vector3 arenaSize = new Vector3(80f, 30f, 80f);
    [SerializeField] float mazeFlightMinHeight = 0.7f;
    [SerializeField] float mazeFlightBandHeight = 1.4f;
    [SerializeField] float spawnRadius = 12f;
    [SerializeField] float agentScale = 0.7f;
    [SerializeField] int coverageGridResolution = 20;
    [SerializeField] bool drawArenaGizmos = true;
    [SerializeField] bool drawCoverageGizmos = true;
    [SerializeField] int maxCoverageGizmos = 512;

    [Header("Exploration")]
    [SerializeField] float explorationProbeDistance = 14f;
    [SerializeField] float explorationWeight = 14f;
    [SerializeField] float fruitFlyWanderWeight = 2.6f;
    [SerializeField] int noveltyProbeSamples = 4;

    [Header("Boids")]
    [SerializeField] float neighborRadius = 4f;
    [SerializeField] float separationRadius = 0.9f;
    [SerializeField] float maxSpeed = 10f;
    [SerializeField] float maxForce = 22f;
    [SerializeField] float separationWeight = 1.7f;
    [SerializeField] float alignmentWeight = 0.7f;
    [SerializeField] float cohesionWeight = 0.35f;
    [SerializeField] float wanderWeight = 0.8f;
    [SerializeField] float boundaryWeight = 4f;
    [SerializeField] float boundaryMargin = 8f;
    [SerializeField] float mazeWallAvoidanceDistance = 2.5f;
    [SerializeField] float mazeWallAvoidanceWeight = 10f;

    readonly List<SwarmFlightAgent> _agents = new List<SwarmFlightAgent>();

    ExperimentSwarm003MetricsLogger _metricsLogger;
    Transform _agentsRoot;
    System.Random _rng;
    Vector3 _activeArenaCenter;
    Vector3 _activeArenaSize;
    int[] _visitCounts;
    int _steps;
    int _newVoxelsDiscovered;
    int _totalVoxelSamples;
    int _revisitSamples;
    int _frontierBiasSteps;
    bool _isRunning;

    public int Seed => seed;
    public int ConfiguredAgentCount => agentCount;
    public int AgentCount => _agents.Count;
    public int Steps => _steps;
    public bool IsRunning => _isRunning;
    public float MeanSpeed { get; private set; }
    public float MeanNeighborDistance { get; private set; }
    public float CohesionIndex { get; private set; }
    public int SeparationViolations { get; private set; }
    public int BoundaryHits { get; private set; }
    public float CoverageVolumePercent { get; private set; }
    public int NewVoxelsDiscovered => _newVoxelsDiscovered;
    public float RevisitRatio { get; private set; }
    public float MeanVisitCount { get; private set; }
    public float ExplorationEfficiency { get; private set; }
    public int FrontierBiasSteps => _frontierBiasSteps;
    public EpisodeTerminationReason TerminationReason { get; private set; } = EpisodeTerminationReason.None;

    void Awake()
    {
        if (mazeGen == null)
            mazeGen = GetComponent<MazeGen>();

        _metricsLogger = new ExperimentSwarm003MetricsLogger(metricsCsvRelativePath, enableMetricsLogging);
        ExperimentRunnerExclusivity.ActivateExclusive(this);
    }

    void OnValidate()
    {
        agentCount = Mathf.Max(2, agentCount);
        maxSteps = Mathf.Max(1, maxSteps);
        arenaSize.x = Mathf.Max(1f, arenaSize.x);
        arenaSize.y = Mathf.Max(1f, arenaSize.y);
        arenaSize.z = Mathf.Max(1f, arenaSize.z);
        mazeFlightMinHeight = Mathf.Max(0f, mazeFlightMinHeight);
        mazeFlightBandHeight = Mathf.Max(0.5f, mazeFlightBandHeight);
        spawnRadius = Mathf.Max(0.1f, spawnRadius);
        agentScale = Mathf.Max(0f, agentScale);
        coverageGridResolution = Mathf.Clamp(coverageGridResolution, 2, 64);
        maxCoverageGizmos = Mathf.Max(0, maxCoverageGizmos);
        explorationProbeDistance = Mathf.Max(0.1f, explorationProbeDistance);
        explorationWeight = Mathf.Max(0f, explorationWeight);
        fruitFlyWanderWeight = Mathf.Max(0f, fruitFlyWanderWeight);
        noveltyProbeSamples = Mathf.Clamp(noveltyProbeSamples, 1, ProbeDirections.Length);
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
        float deltaTime = Time.fixedDeltaTime;
        for (int i = 0; i < _agents.Count; i++)
        {
            _agents[i].ExecuteStep(_agents, settings, deltaTime, _rng);
            if (_agents[i].LastUsedExplorationBias)
                _frontierBiasSteps++;
        }

        TrackMetrics();
        _steps++;

        if (_steps >= maxSteps)
            EndEpisode(EpisodeTerminationReason.Timeout);
    }

    [ContextMenu("Begin Episode")]
    public void BeginEpisode()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[EXP-SWARM-003] Runner is disabled; enabling before starting.");
            enabled = true;
        }

        EndEpisodeWithoutLogging();
        _rng = new System.Random(seed);
        ResolveActiveArena();
        ResetCoverageField();
        ResetMetrics();

        EnsureAgentsRoot();
        SpawnAgents();
        TrackMetrics();

        _isRunning = true;
        Debug.Log(
            $"[EXP-SWARM-003] Started mode={explorationMode} seed={seed} agents={_agents.Count} " +
            $"arenaCenter={_activeArenaCenter} arenaSize={_activeArenaSize} maxSteps={maxSteps}.");
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
            _agents[i].EndEpisode();

        _metricsLogger.LogEpisode(new ExperimentSwarm003EpisodeMetrics
        {
            seed = seed,
            explorationMode = explorationMode.ToString(),
            agentCount = _agents.Count,
            steps = _steps,
            meanSpeed = MeanSpeed,
            meanNeighborDistance = MeanNeighborDistance,
            cohesionIndex = CohesionIndex,
            separationViolations = SeparationViolations,
            boundaryHits = BoundaryHits,
            coverageVolumePercent = CoverageVolumePercent,
            newVoxelsDiscovered = NewVoxelsDiscovered,
            revisitRatio = RevisitRatio,
            meanVisitCount = MeanVisitCount,
            explorationEfficiency = ExplorationEfficiency,
            frontierBiasSteps = FrontierBiasSteps,
            terminationReason = reason
        });

        Debug.Log(
            $"[EXP-SWARM-003] Ended reason={reason} steps={_steps} coverage={CoverageVolumePercent:F1}% " +
            $"newVoxels={NewVoxelsDiscovered} revisitRatio={RevisitRatio:F2}.");
    }

    void ResetMetrics()
    {
        _steps = 0;
        _newVoxelsDiscovered = 0;
        _totalVoxelSamples = 0;
        _revisitSamples = 0;
        _frontierBiasSteps = 0;
        MeanSpeed = 0f;
        MeanNeighborDistance = 0f;
        CohesionIndex = 0f;
        SeparationViolations = 0;
        BoundaryHits = 0;
        CoverageVolumePercent = 0f;
        RevisitRatio = 0f;
        MeanVisitCount = 0f;
        ExplorationEfficiency = 0f;
        TerminationReason = EpisodeTerminationReason.None;
    }

    void ResetCoverageField()
    {
        int total = coverageGridResolution * coverageGridResolution;
        _visitCounts = new int[total];
    }

    void EndEpisodeWithoutLogging()
    {
        _isRunning = false;
        for (int i = 0; i < _agents.Count; i++)
        {
            if (_agents[i] != null)
                Destroy(_agents[i].gameObject);
        }

        _agents.Clear();
    }

    void OnDisable()
    {
        EndEpisodeWithoutLogging();
    }

    void EnsureAgentsRoot()
    {
        if (_agentsRoot != null && _agentsRoot.gameObject != null)
            return;

        var root = new GameObject("EXP-SWARM-003 Agents");
        root.transform.SetParent(transform, false);
        _agentsRoot = root.transform;
    }

    void SpawnAgents()
    {
        SwarmFlightSettings settings = BuildSettings();
        float resolvedAgentScale = ResolveAgentVisualScale();
        for (int i = 0; i < agentCount; i++)
        {
            GameObject agentObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            agentObject.name = $"Swarm003Agent_{i:00}";
            agentObject.transform.SetParent(_agentsRoot, true);
            agentObject.transform.position = RandomSpawnPosition();
            agentObject.transform.localScale = Vector3.one * resolvedAgentScale;

            var collider = agentObject.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            Renderer renderer = agentObject.GetComponent<Renderer>();
            ExperimentAgentVisuals.ApplyUnlitColor(renderer, AgentColors[i % AgentColors.Length]);

            SwarmFlightAgent agent = agentObject.AddComponent<SwarmFlightAgent>();
            Vector3 velocity = RandomUnitVector() * Mathf.Max(0.1f, settings.MaxSpeed * 0.65f);
            agent.BeginEpisode(i, velocity);
            _agents.Add(agent);
        }

        Debug.Log($"[EXP-SWARM-003] Spawned {_agents.Count} exploration boids under '{_agentsRoot.name}'.");
    }

    Vector3 RandomSpawnPosition()
    {
        if (useMazeBounds && TryGetMazeGenerator(out MazeGenerator generator))
        {
            int cellX = _rng.Next(generator.Config.mazeWidthCells);
            int cellY = _rng.Next(generator.Config.mazeHeightCells);
            Vector3 position = generator.GetCellCenterWorld(cellX, cellY);
            position.y = _activeArenaCenter.y - _activeArenaSize.y * 0.5f +
                         (float)_rng.NextDouble() * _activeArenaSize.y;
            return position;
        }

        Vector3 half = _activeArenaSize * 0.5f;
        float radius = Mathf.Min(spawnRadius, Mathf.Min(half.x, Mathf.Min(half.y, half.z)));
        return _activeArenaCenter + RandomUnitVector() * radius;
    }

    float ResolveAgentVisualScale()
    {
        if (agentScale > 0.1f)
            return agentScale;

        if (useMazeBounds && TryGetMazeGenerator(out MazeGenerator generator))
            return Mathf.Max(0.45f, generator.Config.cellSize * 0.18f);

        return 0.7f;
    }

    SwarmFlightSettings BuildSettings()
    {
        MazeGenerator generator = null;
        if (useMazeBounds)
            TryGetMazeGenerator(out generator);

        bool fruitFly = explorationMode == ExperimentSwarm003Mode.FruitFlySearch;
        return new SwarmFlightSettings
        {
            ArenaCenter = _activeArenaCenter,
            ArenaSize = _activeArenaSize,
            Obstacles = null,
            ExplorationField = fruitFly ? this : null,
            NeighborRadius = neighborRadius,
            SeparationRadius = separationRadius,
            MaxSpeed = maxSpeed,
            MaxForce = maxForce,
            SeparationWeight = separationWeight,
            AlignmentWeight = alignmentWeight,
            CohesionWeight = cohesionWeight,
            WanderWeight = fruitFly ? fruitFlyWanderWeight : wanderWeight,
            BoundaryWeight = boundaryWeight,
            BoundaryMargin = boundaryMargin,
            Maze = generator,
            MazeWallAvoidanceDistance = mazeWallAvoidanceDistance,
            MazeWallAvoidanceWeight = mazeWallAvoidanceWeight,
            ObstacleAvoidanceDistance = 0f,
            ObstacleAvoidanceWeight = 0f,
            ExplorationProbeDistance = explorationProbeDistance,
            ExplorationWeight = fruitFly ? explorationWeight : 0f
        };
    }

    public bool TrySampleNoveltyDirection(
        Vector3 position,
        Vector3 velocity,
        float probeDistance,
        out Vector3 direction)
    {
        direction = Vector3.zero;
        if (_visitCounts == null || _visitCounts.Length == 0)
            return false;

        Vector3 bestDirection = Vector3.zero;
        int bestVisits = int.MaxValue;
        int samples = Mathf.Clamp(noveltyProbeSamples, 1, ProbeDirections.Length);

        for (int i = 0; i < samples; i++)
        {
            Vector3 candidate = ProbeDirections[i];
            Vector3 sample = ClampToArena(position + candidate * probeDistance);
            int visits = VisitCountAt(sample);
            if (visits >= bestVisits)
                continue;

            bestVisits = visits;
            bestDirection = candidate;
        }

        Vector3 forward = velocity.sqrMagnitude > 0.001f ? velocity.normalized : transform.forward;
        Vector3 forwardSample = ClampToArena(position + forward * probeDistance);
        int forwardVisits = VisitCountAt(forwardSample);
        if (forwardVisits <= bestVisits)
            bestDirection = forward;

        if (bestDirection.sqrMagnitude <= 0.001f)
            return false;

        direction = bestDirection.normalized;
        return true;
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

    void TrackMetrics()
    {
        if (_agents.Count == 0)
            return;

        Vector3 center = Vector3.zero;
        float speedSum = 0f;
        int boundaryHits = 0;

        for (int i = 0; i < _agents.Count; i++)
        {
            SwarmFlightAgent agent = _agents[i];
            center += agent.transform.position;
            speedSum += agent.Velocity.magnitude;
            boundaryHits += agent.BoundaryHits;
            TrackCoverageVoxel(agent.transform.position);
        }

        center /= _agents.Count;
        MeanSpeed = speedSum / _agents.Count;
        BoundaryHits = boundaryHits;
        TrackSwarmShapeMetrics(center);
        UpdateCoverageMetrics();
    }

    void TrackSwarmShapeMetrics(Vector3 center)
    {
        float spreadSum = 0f;
        float nearestSum = 0f;
        int nearestCount = 0;
        int violations = 0;

        for (int i = 0; i < _agents.Count; i++)
        {
            Vector3 position = _agents[i].transform.position;
            spreadSum += Vector3.Distance(position, center);

            float nearest = float.PositiveInfinity;
            for (int j = 0; j < _agents.Count; j++)
            {
                if (i == j)
                    continue;

                float distance = Vector3.Distance(position, _agents[j].transform.position);
                if (distance < nearest)
                    nearest = distance;

                if (j > i && distance < separationRadius)
                    violations++;
            }

            if (!float.IsInfinity(nearest))
            {
                nearestSum += nearest;
                nearestCount++;
            }
        }

        float meanSpread = spreadSum / _agents.Count;
        float maxSpread = Mathf.Max(0.1f, _activeArenaSize.magnitude * 0.5f);
        CohesionIndex = 1f - Mathf.Clamp01(meanSpread / maxSpread);
        MeanNeighborDistance = nearestCount > 0 ? nearestSum / nearestCount : 0f;
        SeparationViolations = violations;
    }

    void TrackCoverageVoxel(Vector3 worldPosition)
    {
        int index = VoxelIndexAt(worldPosition);
        if (index < 0 || _visitCounts == null || index >= _visitCounts.Length)
            return;

        _totalVoxelSamples++;
        if (_visitCounts[index] == 0)
            _newVoxelsDiscovered++;
        else
            _revisitSamples++;

        _visitCounts[index]++;
    }

    void UpdateCoverageMetrics()
    {
        if (_visitCounts == null || _visitCounts.Length == 0)
            return;

        int visited = 0;
        int totalVisits = 0;
        for (int i = 0; i < _visitCounts.Length; i++)
        {
            if (_visitCounts[i] <= 0)
                continue;

            visited++;
            totalVisits += _visitCounts[i];
        }

        CoverageVolumePercent = 100f * visited / _visitCounts.Length;
        RevisitRatio = _totalVoxelSamples > 0 ? (float)_revisitSamples / _totalVoxelSamples : 0f;
        MeanVisitCount = visited > 0 ? (float)totalVisits / visited : 0f;
        ExplorationEfficiency = _steps > 0 && _agents.Count > 0
            ? (float)_newVoxelsDiscovered / (_steps * _agents.Count)
            : 0f;
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

    Vector3 RandomUnitVector()
    {
        float x = (float)(_rng.NextDouble() * 2.0 - 1.0);
        float y = (float)(_rng.NextDouble() * 2.0 - 1.0);
        float z = (float)(_rng.NextDouble() * 2.0 - 1.0);
        Vector3 value = new Vector3(x, y, z);
        return value.sqrMagnitude > 0.001f ? value.normalized : Vector3.forward;
    }

    void OnDrawGizmos()
    {
        if (!drawArenaGizmos)
            return;

        Vector3 center = Application.isPlaying ? _activeArenaCenter : ResolvePreviewArenaCenter();
        Vector3 size = Application.isPlaying ? _activeArenaSize : ResolvePreviewArenaSize();
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
        Gizmos.DrawWireCube(center, size);

        if (Application.isPlaying && drawCoverageGizmos)
            DrawCoverageGizmos();
    }

    void DrawCoverageGizmos()
    {
        if (_visitCounts == null || _visitCounts.Length == 0 || maxCoverageGizmos <= 0)
            return;

        int drawn = 0;
        Vector3 voxelSize = _activeArenaSize / coverageGridResolution;
        voxelSize.y = 0.04f;
        Vector3 origin = _activeArenaCenter - _activeArenaSize * 0.5f;

        for (int i = 0; i < _visitCounts.Length && drawn < maxCoverageGizmos; i++)
        {
            int visits = _visitCounts[i];
            if (visits <= 0)
                continue;

            int x = i % coverageGridResolution;
            int z = i / coverageGridResolution;
            Vector3 center = origin + new Vector3(
                (x + 0.5f) * voxelSize.x,
                0.08f,
                (z + 0.5f) * voxelSize.z);

            float intensity = Mathf.Clamp01(visits / 6f);
            Gizmos.color = new Color(0.05f, 0.45f + 0.35f * intensity, 1f, 0.22f);
            Gizmos.DrawCube(center, new Vector3(voxelSize.x * 0.75f, voxelSize.y, voxelSize.z * 0.75f));
            drawn++;
        }
    }

    Vector3 ResolvePreviewArenaCenter()
    {
        if (useMazeBounds && mazeGen != null)
        {
            MazeSeedConfig config = mazeGen.Config;
            float width = config.mazeWidthCells * config.cellSize;
            float depth = config.mazeHeightCells * config.cellSize;
            return new Vector3(
                width * 0.5f,
                mazeFlightMinHeight + mazeFlightBandHeight * 0.5f,
                depth * 0.5f);
        }

        return arenaCenter;
    }

    Vector3 ResolvePreviewArenaSize()
    {
        if (useMazeBounds && mazeGen != null)
        {
            MazeSeedConfig config = mazeGen.Config;
            return new Vector3(
                config.mazeWidthCells * config.cellSize,
                mazeFlightBandHeight,
                config.mazeHeightCells * config.cellSize);
        }

        return arenaSize;
    }
}
