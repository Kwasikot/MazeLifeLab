using UnityEngine;

/// <summary>
/// Ensures only one experiment runner drives agents on MazeSystem at a time.
/// When several are enabled, the newest swarm-flight runner wins, then legacy maze experiments.
/// </summary>
public static class ExperimentRunnerExclusivity
{
    public static void EnforceExclusiveRunners(GameObject host)
    {
        if (host == null)
            return;

        MonoBehaviour winner = ResolveWinningRunner(host);
        if (winner != null)
        {
            ActivateExclusive(winner);
            LogActiveRunner(winner);
        }
    }

    static void LogActiveRunner(MonoBehaviour winner)
    {
        string name = winner.GetType().Name;
        int agents = ReadConfiguredAgentCount(winner);
        string agentInfo = agents >= 0 ? $", agentCount={agents}" : string.Empty;
        Debug.Log($"[MazeSystem] Active experiment runner: {name}{agentInfo}");
    }

    static int ReadConfiguredAgentCount(MonoBehaviour runner)
    {
        if (runner is Experiment005Runner runner005)
            return runner005.ConfiguredAgentCount;
        if (runner is Experiment006Runner runner006)
            return runner006.ConfiguredAgentCount;
        if (runner is ExperimentSwarm002Runner swarm002)
            return swarm002.ConfiguredAgentCount;
        if (runner is ExperimentSwarm001Runner swarm001)
            return swarm001.ConfiguredAgentCount;
        if (runner is Experiment004Runner runner004)
            return runner004.ConfiguredAgentCount;

        return -1;
    }

    public static void ActivateExclusive(MonoBehaviour activeRunner)
    {
        if (activeRunner == null || !activeRunner.enabled)
            return;

        GameObject host = activeRunner.gameObject;
        DisableIfNot<Experiment001Runner>(host, activeRunner);
        DisableIfNot<Experiment004Runner>(host, activeRunner);
        DisableIfNot<Experiment005Runner>(host, activeRunner);
        DisableIfNot<Experiment006Runner>(host, activeRunner);
        DisableIfNot<ExperimentSwarm001Runner>(host, activeRunner);
        DisableIfNot<ExperimentSwarm002Runner>(host, activeRunner);
    }

    static MonoBehaviour ResolveWinningRunner(GameObject host)
    {
        var swarm002 = host.GetComponent<ExperimentSwarm002Runner>();
        if (swarm002 != null && swarm002.enabled)
            return swarm002;

        var swarm001 = host.GetComponent<ExperimentSwarm001Runner>();
        if (swarm001 != null && swarm001.enabled)
            return swarm001;

        var runner006 = host.GetComponent<Experiment006Runner>();
        if (runner006 != null && runner006.enabled)
            return runner006;

        var runner005 = host.GetComponent<Experiment005Runner>();
        if (runner005 != null && runner005.enabled)
            return runner005;

        var runner004 = host.GetComponent<Experiment004Runner>();
        if (runner004 != null && runner004.enabled)
            return runner004;

        var runner001 = host.GetComponent<Experiment001Runner>();
        if (runner001 != null && runner001.enabled)
            return runner001;

        return null;
    }

    static void DisableIfNot<T>(GameObject host, MonoBehaviour activeRunner) where T : MonoBehaviour
    {
        T runner = host.GetComponent<T>();
        if (runner != null && runner != activeRunner)
            runner.enabled = false;
    }
}
