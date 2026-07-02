using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public struct ExperimentSwarm005bEpisodeMetrics
{
    public int seed;
    public int agentCount;
    public SwarmFoodCoordinationMode coordinationMode;
    public int steps;
    public int foodDiscovered;
    public int foodReturned;
    public int timeToFirstFood;
    public float coverageVolumePercent;
    public float revisitRatio;
    public float foragingEfficiency;
    public int signalsEmitted;
    public int signalsReceived;
    public int coordinationInfluencedSteps;
    public int staleSignalRejects;
    public int duplicateTargetAgentsPeak;
    public EpisodeTerminationReason terminationReason;
}

public sealed class ExperimentSwarm005bMetricsLogger
{
    const string CsvHeader =
        "episode_id,seed,agent_count,coordination_mode,steps,food_discovered,food_returned," +
        "time_to_first_food,coverage_volume_percent,revisit_ratio,foraging_efficiency," +
        "signals_emitted,signals_received,coordination_influenced_steps,stale_signal_rejects," +
        "duplicate_target_agents_peak,termination_reason";

    readonly string _filePath;
    readonly object _writeLock = new object();
    int _nextEpisodeId;

    public ExperimentSwarm005bMetricsLogger(string relativePath, bool enabled = true)
    {
        Enabled = enabled;
        _filePath = ResolveProjectPath(relativePath);
        if (!Enabled)
            return;

        EnsureDirectory();
        _nextEpisodeId = ReadNextEpisodeId();
    }

    public bool Enabled { get; set; }
    public string FilePath => _filePath;

    public int LogEpisode(in ExperimentSwarm005bEpisodeMetrics metrics)
    {
        if (!Enabled)
            return 0;

        int episodeId;
        string line;
        lock (_writeLock)
        {
            EnsureHeaderExists();
            episodeId = _nextEpisodeId++;
            line = FormatRow(episodeId, metrics);
            File.AppendAllText(_filePath, line + Environment.NewLine, Encoding.UTF8);
        }

        Debug.Log($"[EXP-SWARM-005b Metrics] episode_id={episodeId} mode={metrics.coordinationMode} -> {_filePath}");
        return episodeId;
    }

    static string ResolveProjectPath(string relativePath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.GetFullPath(Path.Combine(projectRoot, relativePath));
    }

    void EnsureDirectory()
    {
        string directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    void EnsureHeaderExists()
    {
        if (File.Exists(_filePath) && new FileInfo(_filePath).Length > 0)
            return;

        File.WriteAllText(_filePath, CsvHeader + Environment.NewLine, Encoding.UTF8);
    }

    int ReadNextEpisodeId()
    {
        if (!File.Exists(_filePath))
            return 1;

        int maxId = 0;
        foreach (string line in File.ReadLines(_filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("episode_id", StringComparison.Ordinal))
                continue;

            int comma = line.IndexOf(',');
            if (comma <= 0)
                continue;

            if (int.TryParse(line.Substring(0, comma), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                maxId = Math.Max(maxId, id);
        }

        return maxId + 1;
    }

    static string FormatRow(int episodeId, in ExperimentSwarm005bEpisodeMetrics metrics)
    {
        return string.Join(",",
            episodeId.ToString(CultureInfo.InvariantCulture),
            metrics.seed.ToString(CultureInfo.InvariantCulture),
            metrics.agentCount.ToString(CultureInfo.InvariantCulture),
            EscapeCsvField(metrics.coordinationMode.ToReportLabel()),
            metrics.steps.ToString(CultureInfo.InvariantCulture),
            metrics.foodDiscovered.ToString(CultureInfo.InvariantCulture),
            metrics.foodReturned.ToString(CultureInfo.InvariantCulture),
            metrics.timeToFirstFood.ToString(CultureInfo.InvariantCulture),
            metrics.coverageVolumePercent.ToString("F3", CultureInfo.InvariantCulture),
            metrics.revisitRatio.ToString("F3", CultureInfo.InvariantCulture),
            metrics.foragingEfficiency.ToString("F6", CultureInfo.InvariantCulture),
            metrics.signalsEmitted.ToString(CultureInfo.InvariantCulture),
            metrics.signalsReceived.ToString(CultureInfo.InvariantCulture),
            metrics.coordinationInfluencedSteps.ToString(CultureInfo.InvariantCulture),
            metrics.staleSignalRejects.ToString(CultureInfo.InvariantCulture),
            metrics.duplicateTargetAgentsPeak.ToString(CultureInfo.InvariantCulture),
            EscapeCsvField(metrics.terminationReason.ToString()));
    }

    static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
        if (!needsQuotes)
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
