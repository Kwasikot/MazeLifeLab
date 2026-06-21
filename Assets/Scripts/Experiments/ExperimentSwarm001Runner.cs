using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(120)]
public class ExperimentSwarm001Runner : MonoBehaviour
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

    [Header("Episode")]
    [SerializeField] int seed = 42;
    [SerializeField] int agentCount = 24;
    [SerializeField] int maxSteps = 5000;
    [SerializeField] bool autoStartOnPlay = true;
    [SerializeField] bool enableMetricsLogging = true;
    [SerializeField] string metricsCsvRelativePath = "results/experiment_swarm_001_boids.csv";

    [Header("Arena")]
    [SerializeField] MazeGen mazeGen;
    [SerializeField] bool useMazeBounds = true;
    [SerializeField] Vector3 arenaCenter = new Vector3(0f, 15f, 0f);
    [SerializeField] Vector3 arenaSize = new Vector3(80f, 30f, 80f);
    [SerializeField] float mazeFlightMinHeight = 3f;
    [SerializeField] float mazeFlightBandHeight = 12f;
    [SerializeField] float spawnRadius = 12f;
    [SerializeField] float agentScale = 0f;
    [SerializeField] int coverageGridResolution = 10;
    [SerializeField] bool drawArenaGizmos = true;

    [Header("Boids")]
    [SerializeField] float neighborRadius = 7f;
    [SerializeField] float separationRadius = 2.2f;
    [SerializeField] float maxSpeed = 8f;
    [SerializeField] float maxForce = 18f;
    [SerializeField] float separationWeight = 2.2f;
    [SerializeField] float alignmentWeight = 1.1f;
    [SerializeField] float cohesionWeight = 0.9f;
    [SerializeField] float wanderWeight = 0.8f;
    [SerializeField] float boundaryWeight = 4f;
    [SerializeField] float boundaryMargin = 8f;
    [SerializeField] float mazeWallAvoidanceDistance = 2.5f;
    [SerializeField] float mazeWallAvoidanceWeight = 10f;

    readonly List<SwarmFlightAgent> _agents = new List<SwarmFlightAgent>();
    readonly HashSet<int> _visitedVoxels = new HashSet<int>();

    ExperimentSwarm001MetricsLogger _metricsLogger;
    Transform _agentsRoot;
    System.Random _rng;
    Vector3 _activeArenaCenter;
    Vector3 _activeArenaSize;
    int _steps;
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
    public EpisodeTerminationReason TerminationReason { get; private set; } = EpisodeTerminationReason.None;

    void Awake()
    {
        if (mazeGen == null)
            mazeGen = GetComponent<MazeGen>();

        _metricsLogger = new ExperimentSwarm001MetricsLogger(metricsCsvRelativePath, enableMetricsLogging);
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
            _agents[i].ExecuteStep(_agents, settings, deltaTime, _rng);

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
            Debug.LogWarning("[EXP-SWARM-001] Runner is disabled; enabling before starting.");
            enabled = true;
        }

        EndEpisodeWithoutLogging();
        _rng = new System.Random(seed);
        ResolveActiveArena();
        _steps = 0;
        MeanSpeed = 0f;
        MeanNeighborDistance = 0f;
        CohesionIndex = 0f;
        SeparationViolations = 0;
        BoundaryHits = 0;
        CoverageVolumePercent = 0f;
        TerminationReason = EpisodeTerminationReason.None;
        _visitedVoxels.Clear();

        EnsureAgentsRoot();
        SpawnAgents();
        TrackMetrics();

        _isRunning = true;
        Debug.Log(
            $"[EXP-SWARM-001] Started seed={seed} agents={_agents.Count} " +
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

        _metricsLogger.LogEpisode(new ExperimentSwarm001EpisodeMetrics
        {
            seed = seed,
            agentCount = _agents.Count,
            steps = _steps,
            meanSpeed = MeanSpeed,
            meanNeighborDistance = MeanNeighborDistance,
            cohesionIndex = CohesionIndex,
            separationViolations = SeparationViolations,
            boundaryHits = BoundaryHits,
            coverageVolumePercent = CoverageVolumePercent,
            terminationReason = reason
        });

        Debug.Log(
            $"[EXP-SWARM-001] Ended reason={reason} steps={_steps} " +
            $"meanSpeed={MeanSpeed:F2} cohesion={CohesionIndex:F2} coverageVolume={CoverageVolumePercent:F1}%.");
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
        _visitedVoxels.Clear();
    }

    void OnDisable()
    {
        EndEpisodeWithoutLogging();
    }

    void EnsureAgentsRoot()
    {
        if (_agentsRoot != null && _agentsRoot.gameObject != null)
            return;

        var root = new GameObject("EXP-SWARM-001 Agents");
        root.transform.SetParent(transform, false);
        _agentsRoot = root.transform;
    }

    void SpawnAgents()
    {
        SwarmFlightSettings settings = BuildSettings();
        float resolvedAgentScale = ResolveAgentVisualScale();
        for (int i = 0; i < agentCount; i++)
        {
            GameObject agentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            agentObject.name = $"SwarmFlightAgent_{i:00}";
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

        Debug.Log(
            $"[EXP-SWARM-001] Spawned {_agents.Count} visible boids under '{_agentsRoot.name}' " +
            $"scale={resolvedAgentScale:F2}.");

        if (_agents.Count > 0)
            Debug.Log($"[EXP-SWARM-001] First boid position={_agents[0].transform.position}.");
    }

    float ResolveAgentVisualScale()
    {
        if (agentScale > 0.1f)
            return agentScale;

        if (useMazeBounds && TryGetMazeGenerator(out MazeGenerator generator))
            return Mathf.Max(2.5f, ExperimentAgentVisuals.ResolveAgentCubeScale(generator, agentCount));

        return 3f;
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

    SwarmFlightSettings BuildSettings()
    {
        MazeGenerator generator = null;
        if (useMazeBounds)
            TryGetMazeGenerator(out generator);

        return new SwarmFlightSettings
        {
            ArenaCenter = _activeArenaCenter,
            ArenaSize = _activeArenaSize,
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
            MazeWallAvoidanceWeight = mazeWallAvoidanceWeight
        };
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

        int totalVoxels = coverageGridResolution * coverageGridResolution * coverageGridResolution;
        CoverageVolumePercent = totalVoxels > 0
            ? 100f * _visitedVoxels.Count / totalVoxels
            : 0f;
    }

    void TrackCoverageVoxel(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - (_activeArenaCenter - _activeArenaSize * 0.5f);
        int x = Mathf.Clamp(Mathf.FloorToInt(local.x / _activeArenaSize.x * coverageGridResolution), 0, coverageGridResolution - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(local.y / _activeArenaSize.y * coverageGridResolution), 0, coverageGridResolution - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(local.z / _activeArenaSize.z * coverageGridResolution), 0, coverageGridResolution - 1);
        _visitedVoxels.Add(x + coverageGridResolution * (y + coverageGridResolution * z));
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
