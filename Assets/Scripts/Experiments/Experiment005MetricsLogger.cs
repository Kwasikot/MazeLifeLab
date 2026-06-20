using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public struct Experiment005EpisodeMetrics
{
    public int mazeSeed;
    public string algorithm;
    public string communicationMode;
    public int agentCount;
    public bool success;
    public int steps;
    public int stepsToFirstGoal;
    public int agentsAtGoal;
    public int totalCollisions;
    public float totalPathLength;
    public float teamCoveragePercent;
    public float overlapPercent;
    public int signalsDeposited;
    public int signalInfluencedSteps;
    public EpisodeTerminationReason terminationReason;
}

public sealed class Experiment005MetricsLogger
{
    const string CsvHeader =
        "episode_id,maze_seed,algorithm,communication_mode,agent_count,success,steps,steps_to_first_goal,agents_at_goal," +
        "total_collisions,total_path_length,team_coverage_percent,overlap_percent,signals_deposited,signal_influenced_steps,termination_reason";

    readonly string _filePath;
    readonly object _writeLock = new object();
    int _nextEpisodeId;

    public Experiment005MetricsLogger(string relativePath, bool enabled = true)
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

    public int LogEpisode(in Experiment005EpisodeMetrics metrics)
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

        Debug.Log($"[EXP-005 Metrics] episode_id={episodeId} -> {_filePath}");
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

    static string FormatRow(int episodeId, in Experiment005EpisodeMetrics metrics)
    {
        return string.Join(",",
            episodeId.ToString(CultureInfo.InvariantCulture),
            metrics.mazeSeed.ToString(CultureInfo.InvariantCulture),
            EscapeCsvField(metrics.algorithm ?? string.Empty),
            EscapeCsvField(metrics.communicationMode ?? string.Empty),
            metrics.agentCount.ToString(CultureInfo.InvariantCulture),
            metrics.success ? "true" : "false",
            metrics.steps.ToString(CultureInfo.InvariantCulture),
            metrics.stepsToFirstGoal.ToString(CultureInfo.InvariantCulture),
            metrics.agentsAtGoal.ToString(CultureInfo.InvariantCulture),
            metrics.totalCollisions.ToString(CultureInfo.InvariantCulture),
            metrics.totalPathLength.ToString("F2", CultureInfo.InvariantCulture),
            metrics.teamCoveragePercent.ToString("F2", CultureInfo.InvariantCulture),
            metrics.overlapPercent.ToString("F2", CultureInfo.InvariantCulture),
            metrics.signalsDeposited.ToString(CultureInfo.InvariantCulture),
            metrics.signalInfluencedSteps.ToString(CultureInfo.InvariantCulture),
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
