using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public struct ExperimentSwarm004EpisodeMetrics
{
    public int seed;
    public int agentCount;
    public int foodSiteCount;
    public int foodUnitsPerSite;
    public int steps;
    public int foodDiscovered;
    public int foodReturned;
    public int timeToFirstFood;
    public int carryingAgents;
    public float coverageVolumePercent;
    public float revisitRatio;
    public float foragingEfficiency;
    public EpisodeTerminationReason terminationReason;
}

public sealed class ExperimentSwarm004MetricsLogger
{
    const string CsvHeader =
        "episode_id,seed,agent_count,food_site_count,food_units_per_site,steps,food_discovered,food_returned," +
        "time_to_first_food,carrying_agents,coverage_volume_percent,revisit_ratio,foraging_efficiency,termination_reason";

    readonly string _filePath;
    readonly object _writeLock = new object();
    int _nextEpisodeId;

    public ExperimentSwarm004MetricsLogger(string relativePath, bool enabled = true)
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

    public int LogEpisode(in ExperimentSwarm004EpisodeMetrics metrics)
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

        Debug.Log($"[EXP-SWARM-004 Metrics] episode_id={episodeId} -> {_filePath}");
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

    static string FormatRow(int episodeId, in ExperimentSwarm004EpisodeMetrics metrics)
    {
        return string.Join(",",
            episodeId.ToString(CultureInfo.InvariantCulture),
            metrics.seed.ToString(CultureInfo.InvariantCulture),
            metrics.agentCount.ToString(CultureInfo.InvariantCulture),
            metrics.foodSiteCount.ToString(CultureInfo.InvariantCulture),
            metrics.foodUnitsPerSite.ToString(CultureInfo.InvariantCulture),
            metrics.steps.ToString(CultureInfo.InvariantCulture),
            metrics.foodDiscovered.ToString(CultureInfo.InvariantCulture),
            metrics.foodReturned.ToString(CultureInfo.InvariantCulture),
            metrics.timeToFirstFood.ToString(CultureInfo.InvariantCulture),
            metrics.carryingAgents.ToString(CultureInfo.InvariantCulture),
            metrics.coverageVolumePercent.ToString("F3", CultureInfo.InvariantCulture),
            metrics.revisitRatio.ToString("F3", CultureInfo.InvariantCulture),
            metrics.foragingEfficiency.ToString("F6", CultureInfo.InvariantCulture),
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
