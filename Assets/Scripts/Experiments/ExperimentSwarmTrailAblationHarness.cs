using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public struct SwarmTrailAblationLiveStatus
{
    public bool isRunningBatch;
    public int completedRuns;
    public int totalRuns;
    public int currentRunIndex;
    public int currentSeed;
    public bool currentScentEnabled;
    public int liveSteps;
    public int liveFoodReturned;
    public int liveFoodDiscovered;
    public int liveScentSteps;
    public int liveScentDeposits;
    public float liveForagingEfficiency;
    public float liveRevisitRatio;
    public SwarmTrailAblationConditionStats partialWithoutTrails;
    public SwarmTrailAblationConditionStats partialWithTrails;
}

/// <summary>
/// Automated EXP-SWARM-005 trail ablation: runs the same seeds with and without scent trails,
/// aggregates metrics, and decides whether trails are working.
/// </summary>
[DisallowMultipleComponent]
public class ExperimentSwarmTrailAblationHarness : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] ExperimentSwarm004Runner foragingRunner;
    [SerializeField] MazeGen mazeGen;

    [Header("Batch Settings")]
    [SerializeField] int[] testSeeds = { 42, 137 };
    [SerializeField] bool logBatchRunsToCsv;

    [Header("Display")]
    [SerializeField] bool showOnScreenDisplay = true;
    [SerializeField] SwarmTrailAblationOsdAnchor osdAnchor = SwarmTrailAblationOsdAnchor.TopRight;
    [SerializeField] float osdScreenMargin = 16f;

    [Header("Latest Report")]
    [SerializeField] SwarmTrailAblationLiveStatus liveStatus;
    [SerializeField] SwarmTrailAblationReport lastReport;
    [SerializeField] List<SwarmForagingEpisodeSnapshot> lastWithoutTrails = new List<SwarmForagingEpisodeSnapshot>();
    [SerializeField] List<SwarmForagingEpisodeSnapshot> lastWithTrails = new List<SwarmForagingEpisodeSnapshot>();

    Coroutine _batchCoroutine;
    bool _isRunningBatch;
    ExperimentSwarmTrailAblationOsd _osd;

    public bool IsRunningBatch => _isRunningBatch;
    public bool ShowOnScreenDisplay => showOnScreenDisplay;
    public SwarmTrailAblationOsdAnchor OsdAnchor => osdAnchor;
    public float OsdScreenMargin => osdScreenMargin;
    public SwarmTrailAblationLiveStatus LiveStatus => liveStatus;
    public SwarmTrailAblationReport LastReport => lastReport;
    public IReadOnlyList<SwarmForagingEpisodeSnapshot> LastWithoutTrails => lastWithoutTrails;
    public IReadOnlyList<SwarmForagingEpisodeSnapshot> LastWithTrails => lastWithTrails;

    void Awake()
    {
        if (foragingRunner == null)
            foragingRunner = GetComponent<ExperimentSwarm004Runner>();
        if (mazeGen == null)
            mazeGen = GetComponent<MazeGen>();

        EnsureOsd();
    }

    void EnsureOsd()
    {
        _osd = GetComponent<ExperimentSwarmTrailAblationOsd>();
        if (_osd == null)
            _osd = gameObject.AddComponent<ExperimentSwarmTrailAblationOsd>();

        _osd.Bind(this);
    }

    public void RunAllTests()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Trail Ablation] Enter Play mode before running batch tests.");
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
            Debug.LogError("[Trail Ablation] Missing ExperimentSwarm004Runner reference.");
            yield break;
        }

        if (mazeGen != null && !mazeGen.HasGeneratedMaze)
        {
            Debug.LogWarning("[Trail Ablation] Waiting one frame for maze generation.");
            yield return null;
        }

        _isRunningBatch = true;
        lastWithoutTrails.Clear();
        lastWithTrails.Clear();
        lastReport = default;

        foragingRunner.enabled = true;
        foragingRunner.AutoStartOnPlay = false;

        int[] seeds = testSeeds != null && testSeeds.Length > 0 ? testSeeds : new[] { 42, 137 };
        int totalRuns = seeds.Length * 2;
        int completedRuns = 0;

        liveStatus = new SwarmTrailAblationLiveStatus
        {
            isRunningBatch = true,
            totalRuns = totalRuns,
            completedRuns = 0
        };

        Debug.Log($"[Trail Ablation] Starting {totalRuns} runs ({seeds.Length} seeds x 2 conditions).");

        for (int i = 0; i < seeds.Length; i++)
        {
            int episodeSeed = seeds[i];
            yield return RunSingleCondition(
                episodeSeed,
                scentEnabled: false,
                runIndex: completedRuns + 1,
                totalRuns,
                lastWithoutTrails,
                () => completedRuns++);
            yield return RunSingleCondition(
                episodeSeed,
                scentEnabled: true,
                runIndex: completedRuns + 1,
                totalRuns,
                lastWithTrails,
                () => completedRuns++);
        }

        lastReport = SwarmTrailAblationEvaluator.Evaluate(lastWithoutTrails, lastWithTrails);
        _isRunningBatch = false;
        _batchCoroutine = null;
        liveStatus.isRunningBatch = false;
        liveStatus.completedRuns = totalRuns;

        Debug.Log($"[Trail Ablation] Complete. Verdict={lastReport.verdict}. {lastReport.verdictSummary}");
    }

    IEnumerator RunSingleCondition(
        int episodeSeed,
        bool scentEnabled,
        int runIndex,
        int totalRuns,
        List<SwarmForagingEpisodeSnapshot> bucket,
        Action onCompleted)
    {
        foragingRunner.PrepareForBatchRun(episodeSeed, scentEnabled, logBatchRunsToCsv);
        foragingRunner.BeginEpisode();

        liveStatus.currentRunIndex = runIndex;
        liveStatus.currentSeed = episodeSeed;
        liveStatus.currentScentEnabled = scentEnabled;
        liveStatus.totalRuns = totalRuns;

        while (foragingRunner.IsRunning)
        {
            UpdateLiveEpisodeMetrics();
            liveStatus.partialWithoutTrails = SwarmTrailAblationEvaluator.BuildStats(lastWithoutTrails);
            liveStatus.partialWithTrails = SwarmTrailAblationEvaluator.BuildStats(lastWithTrails);
            yield return null;
        }

        bucket.Add(foragingRunner.LastEpisodeSnapshot);
        onCompleted();

        liveStatus.completedRuns = lastWithoutTrails.Count + lastWithTrails.Count;
        liveStatus.partialWithoutTrails = SwarmTrailAblationEvaluator.BuildStats(lastWithoutTrails);
        liveStatus.partialWithTrails = SwarmTrailAblationEvaluator.BuildStats(lastWithTrails);

        SwarmForagingEpisodeSnapshot snapshot = foragingRunner.LastEpisodeSnapshot;
        Debug.Log(
            $"[Trail Ablation] seed={episodeSeed} trails={scentEnabled} " +
            $"foodReturned={snapshot.foodReturned} efficiency={snapshot.foragingEfficiency:F6} " +
            $"firstFood={snapshot.timeToFirstFood} scentSteps={snapshot.scentInfluencedSteps}");
    }

    void UpdateLiveEpisodeMetrics()
    {
        liveStatus.isRunningBatch = _isRunningBatch;
        liveStatus.liveSteps = foragingRunner.Steps;
        liveStatus.liveFoodReturned = foragingRunner.FoodReturned;
        liveStatus.liveFoodDiscovered = foragingRunner.FoodDiscovered;
        liveStatus.liveScentSteps = foragingRunner.ScentInfluencedSteps;
        liveStatus.liveScentDeposits = foragingRunner.ScentDeposits;
        liveStatus.liveForagingEfficiency = foragingRunner.ForagingEfficiency;
        liveStatus.liveRevisitRatio = foragingRunner.RevisitRatio;
    }
}
