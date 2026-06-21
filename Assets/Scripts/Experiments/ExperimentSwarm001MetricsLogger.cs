using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public struct ExperimentSwarm001EpisodeMetrics
{
    public int seed;
    public int agentCount;
    public int steps;
    public float meanSpeed;
    public float meanNeighborDistance;
    public float cohesionIndex;
    public int separationViolations;
    public int boundaryHits;
    public float coverageVolumePercent;
    public EpisodeTerminationReason terminationReason;
}

public sealed class ExperimentSwarm001MetricsLogger
{
    const string CsvHeader =
        "episode_id,seed,agent_count,steps,mean_speed,mean_neighbor_distance,cohesion_index," +
        "separation_violations,boundary_hits,coverage_volume_percent,termination_reason";

    readonly string _filePath;
    readonly object _writeLock = new object();
    int _nextEpisodeId;

    public ExperimentSwarm001MetricsLogger(string relativePath, bool enabled = true)
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

    public int LogEpisode(in ExperimentSwarm001EpisodeMetrics metrics)
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

        Debug.Log($"[EXP-SWARM-001 Metrics] episode_id={episodeId} -> {_filePath}");
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

    static string FormatRow(int episodeId, in ExperimentSwarm001EpisodeMetrics metrics)
    {
        return string.Join(",",
            episodeId.ToString(CultureInfo.InvariantCulture),
            metrics.seed.ToString(CultureInfo.InvariantCulture),
            metrics.agentCount.ToString(CultureInfo.InvariantCulture),
            metrics.steps.ToString(CultureInfo.InvariantCulture),
            metrics.meanSpeed.ToString("F3", CultureInfo.InvariantCulture),
            metrics.meanNeighborDistance.ToString("F3", CultureInfo.InvariantCulture),
            metrics.cohesionIndex.ToString("F3", CultureInfo.InvariantCulture),
            metrics.separationViolations.ToString(CultureInfo.InvariantCulture),
            metrics.boundaryHits.ToString(CultureInfo.InvariantCulture),
            metrics.coverageVolumePercent.ToString("F3", CultureInfo.InvariantCulture),
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
