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
    int _agentIndex;
    bool _drawTreeThisEpisode = true;

    int _cellX;
    int _cellY;
    int _goalCellX;
    int _goalCellY;
    int _stepsSinceReplan;
    int _totalRrtIterations;
    int _totalRrtNodesCreated;
    int _collisionCount;
    int _failedPathMoves;
    int _stuckSteps;
    int _stepsWithoutNewDiscovery;
    int _lastDiscoveredCellCount;
    int _lastStuckCellX = -1;
    int _lastStuckCellY = -1;
    int _previousCellX = -1;
    int _previousCellY = -1;

    MazeStigmergyField _stigmergyField;
    AgentStigmergyController _stigmergy;
    bool _stigmergyReadEnabled;
    bool _stigmergyIgnoreOwnSignals;
    float _signalFollowChance = 0.5f;
    float _signalReadTemperature = 1.25f;
    float _signalCrowdingPenalty = 0.65f;
    float _signalMaxStrength = 8f;
    int _signalInfluencedSteps;

    readonly Dictionary<long, int> _visitCounts = new Dictionary<long, int>();
    readonly List<MazeFrontierCandidate> _frontierCandidates = new List<MazeFrontierCandidate>();
    readonly List<Vector2Int> _recentCells = new List<Vector2Int>(RecentCellHistoryLimit);

    MazeFrontierClaimField _frontierClaimField;
    bool _frontierClaimsEnabled;
    int _claimRadiusCells = 8;
    int _claimTtlSteps = 80;
    float _claimOwnWeight = 2f;
    float _claimAvoidanceWeight = 6f;
    float _claimExpansionWeight = 0.08f;
    float _claimSectorWeight = 0.04f;
    float _responsibilityAnchorX;
    float _responsibilityAnchorY;
    float _responsibilityDirX;
    float _responsibilityDirY;
    int _startCellX;
    int _startCellY;
    int _stepsSinceClaimRefresh;
    int _claimedFrontierX = -1;
    int _claimedFrontierY = -1;
    int _frontierClaimsCreated;
    int _claimGuidedSteps;

    const int ExplorationStallStepThreshold = 12;
    const int RecentCellHistoryLimit = 8;
    const int LocalConfinementUniqueCellLimit = 3;
    const int LocalConfinementSpanLimit = 5;
    const float MinFrontierDistanceFraction = 0.55f;
    const int ClaimReachedDistanceCells = 3;
    const int ClaimRefreshDiscoveryStallSteps = 6;

    SwarmRrtField _swarmField;
    Experiment006SwarmMode _swarmMode = Experiment006SwarmMode.Independent;
    int _swarmGraftNodes;

    public int SignalInfluencedSteps => _signalInfluencedSteps;
    public int FrontierClaimsCreated => _frontierClaimsCreated;
    public int ClaimGuidedSteps => _claimGuidedSteps;
    public int SwarmGraftNodes => _swarmGraftNodes;

    public void ConfigureStigmergy(
        MazeStigmergyField field,
        AgentStigmergyController controller,
        bool enableReadBias,
        bool ignoreOwnSignals)
    {
        ConfigureStigmergy(field, controller, enableReadBias, ignoreOwnSignals, 0.5f, 1.25f, 0.65f, 8f);
    }

    public void ConfigureStigmergy(
        MazeStigmergyField field,
        AgentStigmergyController controller,
        bool enableReadBias,
        bool ignoreOwnSignals,
        float followChance,
        float readTemperature,
        float crowdingPenalty,
        float maxSignalStrength)
    {
        _stigmergyField = field;
        _stigmergy = controller;
        _stigmergyReadEnabled = enableReadBias && field != null;
        _stigmergyIgnoreOwnSignals = ignoreOwnSignals;
        _signalFollowChance = Mathf.Clamp01(followChance);
        _signalReadTemperature = Mathf.Max(0.01f, readTemperature);
        _signalCrowdingPenalty = Mathf.Clamp01(crowdingPenalty);
        _signalMaxStrength = Mathf.Max(0.1f, maxSignalStrength);
        _signalInfluencedSteps = 0;
    }

    public void ConfigureExploration(float goalBias)
    {
        rrtGoalBias = Mathf.Clamp(goalBias, 0.05f, 0.45f);
    }

    public void ConfigureFrontierClaims(
        MazeFrontierClaimField field,
        bool enabled,
        int claimRadiusCells,
        int claimTtlSteps,
        float ownClaimWeight,
        float avoidanceWeight,
        float expansionWeight,
        float sectorWeight,
        int agentCount)
    {
        _frontierClaimField = field;
        _frontierClaimsEnabled = enabled && field != null;
        _claimRadiusCells = Mathf.Max(1, claimRadiusCells);
        _claimTtlSteps = Mathf.Max(1, claimTtlSteps);
        _claimOwnWeight = Mathf.Max(0f, ownClaimWeight);
        _claimAvoidanceWeight = Mathf.Max(0f, avoidanceWeight);
        _claimExpansionWeight = Mathf.Max(0f, expansionWeight);
        _claimSectorWeight = Mathf.Max(0f, sectorWeight);
        ResolveResponsibilityAnchor(Mathf.Max(1, agentCount));
        _claimedFrontierX = -1;
        _claimedFrontierY = -1;
        _frontierClaimsCreated = 0;
        _claimGuidedSteps = 0;
        _stepsSinceClaimRefresh = 0;
    }

    void ResolveResponsibilityAnchor(int agentCount)
    {
        int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(agentCount)));
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)agentCount / columns));
        int col = _agentIndex % columns;
        int row = _agentIndex / columns;

        float width = Mathf.Max(1, _map.WidthCells - 1);
        float height = Mathf.Max(1, _map.HeightCells - 1);
        _responsibilityAnchorX = width * (col + 0.5f) / columns;
        _responsibilityAnchorY = height * (row + 0.5f) / rows;

        float dx = _responsibilityAnchorX - _startCellX;
        float dy = _responsibilityAnchorY - _startCellY;
        float magnitude = Mathf.Sqrt(dx * dx + dy * dy);
        if (magnitude <= 0.001f)
        {
            _responsibilityDirX = 0f;
            _responsibilityDirY = 0f;
        }
        else
        {
            _responsibilityDirX = dx / magnitude;
            _responsibilityDirY = dy / magnitude;
        }
    }

    public void ConfigureSwarmRrt(SwarmRrtField field, Experiment006SwarmMode mode)
    {
        _swarmField = field;
        _swarmMode = mode;
        _swarmGraftNodes = 0;
    }

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

    public void ConfigurePerformance(int iterationsPerStep, int replanInterval)
    {
        rrtIterationsPerStep = Mathf.Max(16, iterationsPerStep);
        replanIntervalSteps = Mathf.Max(1, replanInterval);
    }

    public void ConfigureDrawing(bool drawTree, bool drawPath)
    {
        drawRrtTree = drawTree;
        drawPlannedPath = drawPath;
        _drawTreeThisEpisode = drawTree || drawPath;
    }

    public void RequestReplan()
    {
        if (!_enabled || _truth == null || _rng == null)
            return;

        Replan();
    }

    public void ConfigureVisualization(Color treeColor, Color pathColor)
    {
        treeEdgeColor = treeColor;
        pathEdgeColor = pathColor;
    }

    public void BeginEpisode(
        int mazeSeed,
        MazeGenerator truth,
        int goalCellX,
        int goalCellY,
        float height,
        int agentIndex = 0,
        bool drawRrtTree = true)
    {
        _truth = truth;
        _agentIndex = agentIndex;
        _drawTreeThisEpisode = drawRrtTree;
        _rng = new System.Random(mazeSeed + 120301 + agentIndex * 7919);
        _goalCellX = goalCellX;
        _goalCellY = goalCellY;
        agentHeight = height;
        _collisionCount = 0;
        _totalRrtIterations = 0;
        _totalRrtNodesCreated = 0;
        _stepsSinceReplan = replanIntervalSteps;
        _failedPathMoves = 0;
        _stuckSteps = 0;
        _stepsWithoutNewDiscovery = 0;
        _lastDiscoveredCellCount = 0;
        _lastStuckCellX = -1;
        _lastStuckCellY = -1;
        _previousCellX = -1;
        _previousCellY = -1;
        LastPlanFound = false;
        _enabled = true;
        _visualizer = null;
        _visitCounts.Clear();
        _recentCells.Clear();
        _frontierCandidates.Clear();
        _claimedFrontierX = -1;
        _claimedFrontierY = -1;
        _frontierClaimsCreated = 0;
        _claimGuidedSteps = 0;

        if (_drawTreeThisEpisode)
            EnsureVisualizer();

        _map.Reset(truth.Config.mazeWidthCells, truth.Config.mazeHeightCells);

        if (!truth.TryWorldToCell(transform.position, out _cellX, out _cellY))
        {
            _cellX = 0;
            _cellY = 0;
        }
        _startCellX = _cellX;
        _startCellY = _cellY;

        Sense();
        TrackDiscoveryProgress();
        MarkVisit(_cellX, _cellY);
        Replan();
        SnapToGrid();

        Debug.Log(
            $"[LocalRrt] agent={_agentIndex} start cell=({_cellX},{_cellY}) goal=({_goalCellX},{_goalCellY}) " +
            $"coverage={CoveragePercent:F1}% treeEdges={_treeEdges.Count} sensorRadius={sensorRadiusCells}");
    }

    public void EndEpisode()
    {
        _enabled = false;
        _truth = null;
        _rng = null;
        _stigmergyField = null;
        _stigmergy = null;
        _stigmergyReadEnabled = false;
        _frontierClaimField = null;
        _frontierClaimsEnabled = false;
        _claimedFrontierX = -1;
        _claimedFrontierY = -1;
        _swarmField = null;
        _swarmMode = Experiment006SwarmMode.Independent;
        _swarmGraftNodes = 0;
        _pathQueue.Clear();
        _plannedPath.Clear();
        _treeEdges.Clear();
        _recentCells.Clear();
        if (_visualizer != null)
        {
            _visualizer.Clear();
            _visualizer = null;
        }
    }

    public void ExecuteStep()
    {
        if (!_enabled || _truth == null || _rng == null)
            return;

        Sense();
        TrackDiscoveryProgress();
        _stepsSinceReplan++;
        _stepsSinceClaimRefresh++;
        TrackStuckState();

        if (_stepsSinceReplan >= replanIntervalSteps || _pathQueue.Count == 0)
            Replan();

        if (IsOscillating() || IsLocallyConfined())
        {
            BreakOscillation();
            return;
        }

        if (_stepsWithoutNewDiscovery >= ExplorationStallStepThreshold)
        {
            _pathQueue.Clear();
            _plannedPath.Clear();

            if (TryMoveTowardBestClaimAwareFrontier(forceRefresh: true))
                return;

            if (TryMoveTowardRandomFrontier())
                return;

            if (TryMoveLeastVisitedOpenPassage(avoidImmediateBacktrack: true))
                return;
        }

        if (_frontierClaimsEnabled && TryMoveTowardBestClaimAwareFrontier(forceRefresh: false))
            return;

        if (_pathQueue.Count > 0)
        {
            Vector2Int next = _pathQueue[0];
            if (next.x == _cellX && next.y == _cellY)
            {
                _pathQueue.RemoveAt(0);
                _failedPathMoves = 0;
                if (_pathQueue.Count > 0)
                    next = _pathQueue[0];
                else
                    return;
            }

            if (IsImmediateBacktrack(next.x, next.y) &&
                HasAlternativeOpenPassage(_previousCellX, _previousCellY))
            {
                _pathQueue.RemoveAt(0);
                if (_pathQueue.Count == 0)
                {
                    _stepsSinceReplan = replanIntervalSteps;
                    return;
                }

                next = _pathQueue[0];
            }

            if (TryMoveToCell(next.x, next.y))
            {
                _pathQueue.RemoveAt(0);
                _failedPathMoves = 0;
                NotifyStigmergyDeposit();
            }
            else
            {
                _collisionCount++;
                _failedPathMoves++;
                if (_failedPathMoves >= 2)
                {
                    _pathQueue.Clear();
                    _plannedPath.Clear();
                    _failedPathMoves = 0;
                    _stepsSinceReplan = replanIntervalSteps;
                }
            }

            return;
        }

        _failedPathMoves = 0;

        if (_map.TryFindFrontierMove(_cellX, _cellY, _rng, out int immediateFrontierDirX, out int immediateFrontierDirZ))
        {
            if (TryMoveByDirection(immediateFrontierDirX, immediateFrontierDirZ))
            {
                NotifyStigmergyDeposit();
                return;
            }
            _collisionCount++;
        }
        else if (_map.TryFindStepTowardFrontier(_cellX, _cellY, _goalCellX, _goalCellY, out int frontierDirX, out int frontierDirZ))
        {
            if (TryMoveByDirection(frontierDirX, frontierDirZ))
            {
                NotifyStigmergyDeposit();
                return;
            }

            _collisionCount++;
        }
        else if (_map.TryFindGreedyGoalStep(_cellX, _cellY, _goalCellX, _goalCellY, out int goalDirX, out int goalDirZ))
        {
            if (TryMoveByDirection(goalDirX, goalDirZ))
            {
                NotifyStigmergyDeposit();
                return;
            }

            _collisionCount++;
        }
        else if (TryMoveViaStigmergyBias())
        {
            return;
        }
        else if (TryEscapeViaOpenPassage())
        {
            NotifyStigmergyDeposit();
            return;
        }

        NotifyStigmergyDeposit();
    }

    void TrackDiscoveryProgress()
    {
        int discovered = _map.DiscoveredCellCount;
        if (discovered > _lastDiscoveredCellCount)
        {
            _lastDiscoveredCellCount = discovered;
            _stepsWithoutNewDiscovery = 0;
            return;
        }

        _stepsWithoutNewDiscovery++;
    }

    bool TryMoveTowardRandomFrontier()
    {
        if (!_map.TryFindStepTowardRandomFrontier(_cellX, _cellY, _rng, out int dirX, out int dirZ))
            return false;

        if (!TryMoveByDirection(dirX, dirZ))
            return false;

        _stepsWithoutNewDiscovery = 0;
        NotifyStigmergyDeposit();
        return true;
    }

    void BreakOscillation()
    {
        _pathQueue.Clear();
        _plannedPath.Clear();
        _failedPathMoves = 0;
        _stepsWithoutNewDiscovery = ExplorationStallStepThreshold;
        _stepsSinceReplan = replanIntervalSteps;
        _claimedFrontierX = -1;
        _claimedFrontierY = -1;
        _stepsSinceClaimRefresh = _claimTtlSteps;
        _recentCells.Clear();

        if (TryMoveLeastVisitedOpenPassage(avoidImmediateBacktrack: true))
            return;

        if (TryMoveTowardBestClaimAwareFrontier(forceRefresh: true))
            return;

        TryMoveTowardRandomFrontier();
    }

    bool TryMoveTowardBestClaimAwareFrontier(bool forceRefresh)
    {
        if (!_frontierClaimsEnabled)
            return false;

        if (!TryRefreshFrontierClaim(forceRefresh, out MazeFrontierCandidate target))
            return false;

        if (target.FirstStepX == 0 && target.FirstStepY == 0)
            return false;

        int stepX = _cellX + target.FirstStepX;
        int stepY = _cellY + target.FirstStepY;
        if (!forceRefresh &&
            IsImmediateBacktrack(stepX, stepY) &&
            HasAlternativeOpenPassage(_previousCellX, _previousCellY))
        {
            if (!TryRefreshFrontierClaim(forceRefresh: true, out target))
                return false;

            if (target.FirstStepX == 0 && target.FirstStepY == 0)
                return false;

            stepX = _cellX + target.FirstStepX;
            stepY = _cellY + target.FirstStepY;
            if (IsImmediateBacktrack(stepX, stepY))
                return false;
        }

        if (!TryMoveByDirection(target.FirstStepX, target.FirstStepY))
            return false;

        _claimGuidedSteps++;
        _stepsWithoutNewDiscovery = 0;
        NotifyStigmergyDeposit();
        return true;
    }

    bool TryRefreshFrontierClaim(bool forceRefresh, out MazeFrontierCandidate selected)
    {
        selected = new MazeFrontierCandidate();
        if (!_frontierClaimsEnabled || _frontierClaimField == null || _rng == null)
            return false;

        _map.CollectReachableFrontiers(_cellX, _cellY, _goalCellX, _goalCellY, _frontierCandidates);
        if (_frontierCandidates.Count == 0)
            return false;

        if (!forceRefresh && _stepsWithoutNewDiscovery >= ClaimRefreshDiscoveryStallSteps)
            forceRefresh = true;

        bool hasExistingClaim = _claimedFrontierX >= 0 &&
                                _claimedFrontierY >= 0 &&
                                _frontierClaimField.TryGetClaim(_agentIndex, out FrontierClaimRecord claim) &&
                                claim.X == _claimedFrontierX &&
                                claim.Y == _claimedFrontierY;
        int distToClaim = hasExistingClaim
            ? Mathf.Abs(_cellX - _claimedFrontierX) + Mathf.Abs(_cellY - _claimedFrontierY)
            : int.MaxValue;
        bool claimReached = distToClaim <= ClaimReachedDistanceCells;
        int refreshInterval = Mathf.Max(4, _claimTtlSteps / 5);

        if (!forceRefresh && hasExistingClaim && !claimReached && _stepsSinceClaimRefresh < refreshInterval)
        {
            for (int i = 0; i < _frontierCandidates.Count; i++)
            {
                MazeFrontierCandidate candidate = _frontierCandidates[i];
                if (candidate.X != _claimedFrontierX || candidate.Y != _claimedFrontierY)
                    continue;

                if (candidate.FirstStepX == 0 && candidate.FirstStepY == 0)
                    break;

                selected = candidate;
                _frontierClaimField.PublishClaim(
                    _agentIndex,
                    candidate.X,
                    candidate.Y,
                    _claimRadiusCells,
                    _claimTtlSteps);
                return true;
            }
        }

        if (!TrySelectBestFrontierCandidate(out selected, out _))
            return false;

        _claimedFrontierX = selected.X;
        _claimedFrontierY = selected.Y;
        if (_frontierClaimField.PublishClaim(
                _agentIndex,
                selected.X,
                selected.Y,
                _claimRadiusCells,
                _claimTtlSteps))
        {
            _stepsSinceClaimRefresh = 0;
            _frontierClaimsCreated++;
        }

        return true;
    }

    bool TrySelectBestFrontierCandidate(out MazeFrontierCandidate selected, out float bestScore)
    {
        selected = new MazeFrontierCandidate();
        bestScore = float.NegativeInfinity;

        int maxReachableDistance = 0;
        for (int i = 0; i < _frontierCandidates.Count; i++)
        {
            maxReachableDistance = Mathf.Max(
                maxReachableDistance,
                _frontierCandidates[i].DistanceFromStart);
        }

        int minReachableDistance = Mathf.Max(
            1,
            Mathf.CeilToInt(maxReachableDistance * MinFrontierDistanceFraction));

        bool found = TrySelectBestFrontierCandidate(minReachableDistance, out selected, out bestScore);
        if (found)
            return true;

        return TrySelectBestFrontierCandidate(1, out selected, out bestScore);
    }

    bool TrySelectBestFrontierCandidate(
        int minReachableDistance,
        out MazeFrontierCandidate selected,
        out float bestScore)
    {
        selected = new MazeFrontierCandidate();
        bestScore = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < _frontierCandidates.Count; i++)
        {
            MazeFrontierCandidate candidate = _frontierCandidates[i];
            if (candidate.FirstStepX == 0 && candidate.FirstStepY == 0)
                continue;

            if (candidate.DistanceFromStart < minReachableDistance)
                continue;

            float score = ScoreFrontierCandidate(candidate);
            if (score <= bestScore)
                continue;

            bestScore = score;
            selected = candidate;
            found = true;
        }

        return found;
    }

    float ScoreFrontierCandidate(MazeFrontierCandidate candidate)
    {
        float peerClaim = _frontierClaimField.OtherClaimStrength(_agentIndex, candidate.X, candidate.Y);
        int visits = _visitCounts.TryGetValue(CellKey(candidate.X, candidate.Y), out int count) ? count : 0;
        int firstStepX = _cellX + candidate.FirstStepX;
        int firstStepY = _cellY + candidate.FirstStepY;
        int firstStepVisits = _visitCounts.TryGetValue(CellKey(firstStepX, firstStepY), out int stepCount)
            ? stepCount
            : 0;
        float randomBonus = _rng.NextDouble() < 0.5 ? 0.05f : 0.1f;
        float anchorDistance = Mathf.Abs(candidate.X - _responsibilityAnchorX) +
                               Mathf.Abs(candidate.Y - _responsibilityAnchorY);
        float maxAnchorDistance = Mathf.Max(1f, _map.WidthCells + _map.HeightCells);
        float sectorAlignment = 1f - Mathf.Clamp01(anchorDistance / maxAnchorDistance);
        float progressX = candidate.X - _startCellX;
        float progressY = candidate.Y - _startCellY;
        float directionalProgress = Mathf.Max(0f, progressX * _responsibilityDirX + progressY * _responsibilityDirY);
        int distFromSpawn = Mathf.Abs(candidate.X - _startCellX) + Mathf.Abs(candidate.Y - _startCellY);
        float backtrackPenalty = IsImmediateBacktrack(firstStepX, firstStepY) ? 12f : 0f;

        return randomBonus
               - peerClaim * _claimAvoidanceWeight
               - visits * 1.25f
               - firstStepVisits * 2.5f
               - backtrackPenalty
               + candidate.DistanceFromStart * _claimExpansionWeight
               + distFromSpawn * _claimExpansionWeight * 0.35f
               + directionalProgress * _claimExpansionWeight * 1.5f
               + sectorAlignment * _claimSectorWeight
               - candidate.DistanceToGoal * 0.0005f;
    }

    bool TryMoveViaStigmergyBias()
    {
        if (!_stigmergyReadEnabled || _stigmergyField == null || _rng == null)
            return false;

        if (_rng.NextDouble() > _signalFollowChance)
            return false;

        if (!_stigmergyField.TrySampleBiasStep(
                _cellX,
                _cellY,
                _agentIndex,
                _stigmergyIgnoreOwnSignals,
                _truth,
                _rng,
                _signalReadTemperature,
                _signalMaxStrength,
                _signalCrowdingPenalty,
                out int dirX,
                out int dirZ))
            return false;

        if (!TryMoveByDirection(dirX, dirZ))
            return false;

        _signalInfluencedSteps++;
        NotifyStigmergyDeposit();
        return true;
    }

    void NotifyStigmergyDeposit()
    {
        if (_stigmergy == null)
            return;

        _stigmergy.NotifyAtCell(_cellX, _cellY, _map.IsFrontierCell(_cellX, _cellY));
    }

    void TrackStuckState()
    {
        if (_cellX == _lastStuckCellX && _cellY == _lastStuckCellY)
            _stuckSteps++;
        else
        {
            _stuckSteps = 0;
            _lastStuckCellX = _cellX;
            _lastStuckCellY = _cellY;
        }

        if (_stuckSteps < 8)
            return;

        _stuckSteps = 0;
        _pathQueue.Clear();
        _plannedPath.Clear();
        _failedPathMoves = 0;
        _stepsSinceReplan = replanIntervalSteps;
    }

    bool TryEscapeViaOpenPassage()
    {
        int[][] directions =
        {
            new[] { 0, 1 },
            new[] { 1, 0 },
            new[] { 0, -1 },
            new[] { -1, 0 }
        };

        for (int i = 0; i < directions.Length; i++)
        {
            int dirX = directions[i][0];
            int dirZ = directions[i][1];
            if (!_truth.IsPassageOpen(_cellX, _cellY, dirX, dirZ))
                continue;

            if (TryMoveByDirection(dirX, dirZ))
                return true;
        }

        return false;
    }

    bool TryMoveLeastVisitedOpenPassage(bool avoidImmediateBacktrack = false)
    {
        int[][] directions =
        {
            new[] { 0, 1 },
            new[] { 1, 0 },
            new[] { 0, -1 },
            new[] { -1, 0 }
        };

        int bestDirX = 0;
        int bestDirZ = 0;
        int bestVisits = int.MaxValue;
        bool found = false;

        for (int i = 0; i < directions.Length; i++)
        {
            int dirX = directions[i][0];
            int dirZ = directions[i][1];
            if (!_truth.IsPassageOpen(_cellX, _cellY, dirX, dirZ))
                continue;

            int nextX = _cellX + dirX;
            int nextY = _cellY + dirZ;
            if (!_truth.IsCellInBounds(nextX, nextY))
                continue;

            if (avoidImmediateBacktrack &&
                nextX == _previousCellX &&
                nextY == _previousCellY &&
                HasAlternativeOpenPassage(nextX, nextY))
            {
                continue;
            }

            int visits = _visitCounts.TryGetValue(CellKey(nextX, nextY), out int count) ? count : 0;
            if (visits >= bestVisits)
                continue;

            bestVisits = visits;
            bestDirX = dirX;
            bestDirZ = dirZ;
            found = true;
        }

        if (!found || !TryMoveByDirection(bestDirX, bestDirZ))
            return false;

        _stepsWithoutNewDiscovery = 0;
        NotifyStigmergyDeposit();
        return true;
    }

    bool HasAlternativeOpenPassage(int excludedX, int excludedY)
    {
        int[][] directions =
        {
            new[] { 0, 1 },
            new[] { 1, 0 },
            new[] { 0, -1 },
            new[] { -1, 0 }
        };

        for (int i = 0; i < directions.Length; i++)
        {
            int dirX = directions[i][0];
            int dirZ = directions[i][1];
            if (!_truth.IsPassageOpen(_cellX, _cellY, dirX, dirZ))
                continue;

            int nextX = _cellX + dirX;
            int nextY = _cellY + dirZ;
            if (!_truth.IsCellInBounds(nextX, nextY))
                continue;

            if (nextX != excludedX || nextY != excludedY)
                return true;
        }

        return false;
    }

    void EnsureVisualizer()
    {
        var legacyOnAgent = GetComponent<LocalRrtTreeVisualizer>();
        if (legacyOnAgent != null)
            Destroy(legacyOnAgent);

        var legacyShared = GameObject.Find("MazeRrtVisual");
        if (legacyShared != null)
            Destroy(legacyShared);

        string visualName = $"MazeRrtVisual_A{_agentIndex}";
        GameObject existing = GameObject.Find(visualName);
        if (existing != null)
        {
            _visualizer = existing.GetComponent<LocalRrtTreeVisualizer>();
            if (_visualizer == null)
                _visualizer = existing.AddComponent<LocalRrtTreeVisualizer>();
            return;
        }

        var visualObject = new GameObject(visualName);
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

        List<Vector2Int> path;
        List<LocalRrtEdge> treeEdges;
        int nodesCreated;
        int graftCount = 0;
        int planGoalX = _goalCellX;
        int planGoalY = _goalCellY;

        if (_frontierClaimsEnabled &&
            !_map.IsCellDiscovered(_goalCellX, _goalCellY) &&
            TryRefreshFrontierClaim(forceRefresh: false, out MazeFrontierCandidate claimTarget))
        {
            planGoalX = claimTarget.X;
            planGoalY = claimTarget.Y;
        }

        if (_swarmMode.UsesSwarmGraft() && _swarmField != null)
        {
            LastPlanFound = SwarmRrtPlanner.TryFindPath(
                _map,
                _cellX,
                _cellY,
                planGoalX,
                planGoalY,
                rrtIterationsPerStep,
                rrtGoalBias,
                _rng,
                _swarmField,
                _agentIndex,
                out path,
                out treeEdges,
                out nodesCreated,
                out graftCount);
        }
        else
        {
            LastPlanFound = LocalRrtPlanner.TryFindPath(
                _map,
                _cellX,
                _cellY,
                planGoalX,
                planGoalY,
                rrtIterationsPerStep,
                rrtGoalBias,
                _rng,
                out path,
                out treeEdges,
                out nodesCreated);
        }

        _swarmGraftNodes += graftCount;

        _treeEdges.Clear();
        if (treeEdges != null)
            _treeEdges.AddRange(treeEdges);

        if (_swarmMode.DepositsTreeEdges() && _swarmField != null && _treeEdges.Count > 0)
            _swarmField.DepositTreeEdges(_agentIndex, _treeEdges);

        _totalRrtNodesCreated = Mathf.Max(_totalRrtNodesCreated, nodesCreated);

        if (LastPlanFound && path != null && path.Count > 1)
        {
            _plannedPath.AddRange(path);
            if (!_frontierClaimsEnabled)
            {
                for (int i = 1; i < path.Count; i++)
                    _pathQueue.Add(path[i]);
            }
        }

        RefreshVisualization();
    }

    void RefreshVisualization()
    {
        if (_visualizer == null || _truth == null)
            return;

        bool showTree = drawRrtTree && _drawTreeThisEpisode;
        bool showPath = drawPlannedPath && _drawTreeThisEpisode;
        if (!showTree && !showPath)
        {
            _visualizer.Clear();
            return;
        }

        _visualizer.Rebuild(
            _truth,
            _treeEdges,
            _plannedPath,
            showTree,
            showPath,
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

        _previousCellX = _cellX;
        _previousCellY = _cellY;
        _cellX = nextX;
        _cellY = nextY;
        MarkVisit(_cellX, _cellY);
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

    void MarkVisit(int cellX, int cellY)
    {
        long key = CellKey(cellX, cellY);
        _visitCounts.TryGetValue(key, out int count);
        _visitCounts[key] = count + 1;

        _recentCells.Add(new Vector2Int(cellX, cellY));
        if (_recentCells.Count > RecentCellHistoryLimit)
            _recentCells.RemoveAt(0);
    }

    bool IsOscillating()
    {
        if (_recentCells.Count < 4)
            return false;

        int last = _recentCells.Count - 1;
        Vector2Int a0 = _recentCells[last];
        Vector2Int b0 = _recentCells[last - 1];
        Vector2Int a1 = _recentCells[last - 2];
        Vector2Int b1 = _recentCells[last - 3];

        if (a0 == a1 && b0 == b1 && a0 != b0)
            return true;

        if (_recentCells.Count < 6)
            return false;

        Vector2Int a2 = _recentCells[last - 4];
        Vector2Int b2 = _recentCells[last - 5];
        return a0 == a1 && a1 == a2 &&
               b0 == b1 && b1 == b2 &&
               a0 != b0;
    }

    bool IsLocallyConfined()
    {
        int count = _recentCells.Count;
        if (count < 6)
            return false;

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;
        int uniqueCount = 0;

        for (int i = 0; i < count; i++)
        {
            Vector2Int cell = _recentCells[i];
            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxY = Mathf.Max(maxY, cell.y);

            bool isNew = true;
            for (int j = 0; j < i; j++)
            {
                if (_recentCells[j] == cell)
                {
                    isNew = false;
                    break;
                }
            }

            if (isNew)
                uniqueCount++;
        }

        if (uniqueCount <= LocalConfinementUniqueCellLimit)
            return true;

        return (maxX - minX) + (maxY - minY) <= LocalConfinementSpanLimit;
    }

    bool IsImmediateBacktrack(int targetX, int targetY)
    {
        return _previousCellX >= 0 &&
               _previousCellY >= 0 &&
               targetX == _previousCellX &&
               targetY == _previousCellY;
    }

    static long CellKey(int cellX, int cellY)
    {
        return ((long)cellX << 32) | (uint)cellY;
    }
}
