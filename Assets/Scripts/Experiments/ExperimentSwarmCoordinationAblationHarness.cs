using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SwarmCoordinationAblationLiveStatus
{
    public bool isRunningBatch;
    public int completedRuns;
    public int totalRuns;
    public int currentRunIndex;
    public int currentSeed;
    public SwarmFoodCoordinationMode currentMode;
    public int liveSteps;
    public int liveFoodReturned;
    public int liveFoodDiscovered;
    public int liveCoordinationSteps;
    public int liveSignalsEmitted;
    public float liveForagingEfficiency;
    public float liveRevisitRatio;
    public SwarmFoodCoordinationConditionStats partialNone;
    public SwarmFoodCoordinationConditionStats partialHive;
    public SwarmFoodCoordinationConditionStats partialLocal;
    public SwarmFoodCoordinationConditionStats partialGlobal;
}

/// <summary>
/// EXP-SWARM-005b batch ablation across food coordination modes vs independent baseline.
/// </summary>
[DisallowMultipleComponent]
public class ExperimentSwarmCoordinationAblationHarness : MonoBehaviour
{
    static readonly SwarmFoodCoordinationMode[] DefaultModes =
    {
        SwarmFoodCoordinationMode.None,
        SwarmFoodCoordinationMode.HiveBulletin,
        SwarmFoodCoordinationMode.LocalBroadcast,
        SwarmFoodCoordinationMode.GlobalBroadcast
    };

    [Header("Target")]
    [SerializeField] ExperimentSwarm004Runner foragingRunner;
    [SerializeField] MazeGen mazeGen;

    [Header("Batch Settings")]
    [SerializeField] int[] testSeeds = { 42, 137 };
    [SerializeField] bool includeGlobalOracle = true;
    [SerializeField] bool logBatchRunsToCsv;

    [Header("Latest Report")]
    [SerializeField] SwarmCoordinationAblationLiveStatus liveStatus;
    [SerializeField] SwarmFoodCoordinationAblationReport lastReport;
    [SerializeField] List<SwarmForagingEpisodeSnapshot> lastNoneRuns = new List<SwarmForagingEpisodeSnapshot>();
    [SerializeField] List<SwarmForagingEpisodeSnapshot> lastHiveRuns = new List<SwarmForagingEpisodeSnapshot>();
    [SerializeField] List<SwarmForagingEpisodeSnapshot> lastLocalRuns = new List<SwarmForagingEpisodeSnapshot>();
    [SerializeField] List<SwarmForagingEpisodeSnapshot> lastGlobalRuns = new List<SwarmForagingEpisodeSnapshot>();

    Coroutine _batchCoroutine;
    bool _isRunningBatch;

    public bool IsRunningBatch => _isRunningBatch;
    public SwarmCoordinationAblationLiveStatus LiveStatus => liveStatus;
    public SwarmFoodCoordinationAblationReport LastReport => lastReport;
    public IReadOnlyList<SwarmForagingEpisodeSnapshot> LastNoneRuns => lastNoneRuns;
    public IReadOnlyList<SwarmForagingEpisodeSnapshot> LastHiveRuns => lastHiveRuns;
    public IReadOnlyList<SwarmForagingEpisodeSnapshot> LastLocalRuns => lastLocalRuns;
    public IReadOnlyList<SwarmForagingEpisodeSnapshot> LastGlobalRuns => lastGlobalRuns;

    void Awake()
    {
        if (foragingRunner == null)
            foragingRunner = GetComponent<ExperimentSwarm004Runner>();
        if (mazeGen == null)
            mazeGen = GetComponent<MazeGen>();
    }

    public void RunAllTests()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Coordination Ablation] Enter Play mode before running batch tests.");
            return;
        }

        if (_batchCoroutine != null)
            StopCoroutine(_batchCoroutine);

        _batchCoroutine = StartCoroutine(RunAllTestsCoroutine());
    }

    public void StopBatch()
    {
        if (_batchCoroutine != null)
        {
            StopCoroutine(_batchCoroutine);
            _batchCoroutine = null;
        }

        _isRunningBatch = false;
        liveStatus.isRunningBatch = false;
    }

    IEnumerator RunAllTestsCoroutine()
    {
        if (foragingRunner == null)
        {
            Debug.LogError("[Coordination Ablation] Missing ExperimentSwarm004Runner reference.");
            yield break;
        }

        if (mazeGen != null && !mazeGen.HasGeneratedMaze)
            yield return null;

        _isRunningBatch = true;
        lastNoneRuns.Clear();
        lastHiveRuns.Clear();
        lastLocalRuns.Clear();
        lastGlobalRuns.Clear();
        lastReport = default;

        foragingRunner.enabled = true;
        foragingRunner.AutoStartOnPlay = false;

        int[] seeds = testSeeds != null && testSeeds.Length > 0 ? testSeeds : new[] { 42, 137 };
        SwarmFoodCoordinationMode[] modes = BuildModeList();
        int totalRuns = seeds.Length * modes.Length;
        int completedRuns = 0;

        liveStatus = new SwarmCoordinationAblationLiveStatus
        {
            isRunningBatch = true,
            totalRuns = totalRuns,
            completedRuns = 0
        };

        Debug.Log($"[Coordination Ablation] Starting {totalRuns} runs ({seeds.Length} seeds x {modes.Length} modes).");

        for (int i = 0; i < seeds.Length; i++)
        {
            for (int m = 0; m < modes.Length; m++)
            {
                yield return RunSingleCondition(
                    seeds[i],
                    modes[m],
                    completedRuns + 1,
                    totalRuns,
                    SelectBucket(modes[m]),
                    () => completedRuns++);
            }
        }

        lastReport = SwarmFoodCoordinationEvaluator.Evaluate(
            lastNoneRuns,
            lastHiveRuns,
            lastLocalRuns,
            lastGlobalRuns);

        _isRunningBatch = false;
        _batchCoroutine = null;
        liveStatus.isRunningBatch = false;
        liveStatus.completedRuns = totalRuns;

        Debug.Log($"[Coordination Ablation] Complete. Verdict={lastReport.verdict}. {lastReport.verdictSummary}");
    }

    SwarmFoodCoordinationMode[] BuildModeList()
    {
        if (!includeGlobalOracle)
            return new[]
            {
                SwarmFoodCoordinationMode.None,
                SwarmFoodCoordinationMode.HiveBulletin,
                SwarmFoodCoordinationMode.LocalBroadcast
            };

        return DefaultModes;
    }

    List<SwarmForagingEpisodeSnapshot> SelectBucket(SwarmFoodCoordinationMode mode)
    {
        switch (mode)
        {
            case SwarmFoodCoordinationMode.HiveBulletin:
                return lastHiveRuns;
            case SwarmFoodCoordinationMode.LocalBroadcast:
                return lastLocalRuns;
            case SwarmFoodCoordinationMode.GlobalBroadcast:
                return lastGlobalRuns;
            default:
                return lastNoneRuns;
        }
    }

    IEnumerator RunSingleCondition(
        int episodeSeed,
        SwarmFoodCoordinationMode mode,
        int runIndex,
        int totalRuns,
        List<SwarmForagingEpisodeSnapshot> bucket,
        Action onCompleted)
    {
        foragingRunner.PrepareForCoordinationBatchRun(episodeSeed, mode, logBatchRunsToCsv);
        foragingRunner.BeginEpisode();

        liveStatus.currentRunIndex = runIndex;
        liveStatus.currentSeed = episodeSeed;
        liveStatus.currentMode = mode;
        liveStatus.totalRuns = totalRuns;

        while (foragingRunner.IsRunning)
        {
            UpdateLiveEpisodeMetrics();
            UpdatePartialStats();
            yield return null;
        }

        bucket.Add(foragingRunner.LastEpisodeSnapshot);
        onCompleted();

        liveStatus.completedRuns = lastNoneRuns.Count + lastHiveRuns.Count + lastLocalRuns.Count + lastGlobalRuns.Count;
        UpdatePartialStats();

        SwarmForagingEpisodeSnapshot snapshot = foragingRunner.LastEpisodeSnapshot;
        Debug.Log(
            $"[Coordination Ablation] seed={episodeSeed} mode={mode.ToReportLabel()} " +
            $"foodReturned={snapshot.foodReturned} efficiency={snapshot.foragingEfficiency:F6} " +
            $"coordSteps={snapshot.coordinationInfluencedSteps} signals={snapshot.signalsEmitted}");
    }

    void UpdateLiveEpisodeMetrics()
    {
        liveStatus.isRunningBatch = _isRunningBatch;
        liveStatus.liveSteps = foragingRunner.Steps;
        liveStatus.liveFoodReturned = foragingRunner.FoodReturned;
        liveStatus.liveFoodDiscovered = foragingRunner.FoodDiscovered;
        liveStatus.liveCoordinationSteps = foragingRunner.CoordinationInfluencedSteps;
        liveStatus.liveSignalsEmitted = foragingRunner.SignalsEmitted;
        liveStatus.liveForagingEfficiency = foragingRunner.ForagingEfficiency;
        liveStatus.liveRevisitRatio = foragingRunner.RevisitRatio;
    }

    void UpdatePartialStats()
    {
        liveStatus.partialNone = SwarmFoodCoordinationEvaluator.BuildStats(lastNoneRuns);
        liveStatus.partialHive = SwarmFoodCoordinationEvaluator.BuildStats(lastHiveRuns);
        liveStatus.partialLocal = SwarmFoodCoordinationEvaluator.BuildStats(lastLocalRuns);
        liveStatus.partialGlobal = SwarmFoodCoordinationEvaluator.BuildStats(lastGlobalRuns);
    }
}
