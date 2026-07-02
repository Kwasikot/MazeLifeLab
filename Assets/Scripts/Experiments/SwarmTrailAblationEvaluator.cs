using System;
using System.Collections.Generic;
using UnityEngine;

public enum SwarmTrailAblationVerdict
{
    Inconclusive = 0,
    TrailsNotWorking = 1,
    TrailsWorking = 2
}

[Serializable]
public struct SwarmForagingEpisodeSnapshot
{
    public int seed;
    public bool scentTrailsEnabled;
    public SwarmFoodCoordinationMode coordinationMode;
    public int steps;
    public int foodReturned;
    public int foodDiscovered;
    public int timeToFirstFood;
    public float foragingEfficiency;
    public float revisitRatio;
    public float coverageVolumePercent;
    public int scentDeposits;
    public int scentInfluencedSteps;
    public int signalsEmitted;
    public int signalsReceived;
    public int coordinationInfluencedSteps;
    public int staleSignalRejects;
    public int duplicateTargetAgentsPeak;
    public EpisodeTerminationReason terminationReason;
}

[Serializable]
public struct SwarmTrailAblationConditionStats
{
    public int runCount;
    public float meanFoodReturned;
    public float meanForagingEfficiency;
    public float meanTimeToFirstFood;
    public float meanRevisitRatio;
    public float meanScentDeposits;
    public float meanScentInfluencedSteps;
}

[Serializable]
public struct SwarmTrailAblationReport
{
    public bool isComplete;
    public SwarmTrailAblationVerdict verdict;
    public string verdictSummary;
    public SwarmTrailAblationConditionStats withoutTrails;
    public SwarmTrailAblationConditionStats withTrails;
    public float foodReturnedDeltaPercent;
    public float foragingEfficiencyDeltaPercent;
    public float timeToFirstFoodDeltaPercent;
    public float revisitRatioDeltaPercent;
    public int metricsPassed;
    public int metricsRequired;
}

public static class SwarmTrailAblationEvaluator
{
    const float MinImprovementPercent = 5f;
    const float MinTimeToFirstFoodImprovementPercent = 10f;

    public static SwarmTrailAblationReport Evaluate(
        IReadOnlyList<SwarmForagingEpisodeSnapshot> withoutTrails,
        IReadOnlyList<SwarmForagingEpisodeSnapshot> withTrails)
    {
        var report = new SwarmTrailAblationReport
        {
            isComplete = true,
            metricsRequired = 2,
            withoutTrails = BuildStats(withoutTrails),
            withTrails = BuildStats(withTrails)
        };

        if (withoutTrails == null || withTrails == null ||
            withoutTrails.Count == 0 || withTrails.Count == 0)
        {
            report.verdict = SwarmTrailAblationVerdict.Inconclusive;
            report.verdictSummary = "Need at least one completed run per condition (with and without trails).";
            return report;
        }

        report.foodReturnedDeltaPercent = DeltaPercent(
            report.withoutTrails.meanFoodReturned,
            report.withTrails.meanFoodReturned);
        report.foragingEfficiencyDeltaPercent = DeltaPercent(
            report.withoutTrails.meanForagingEfficiency,
            report.withTrails.meanForagingEfficiency);
        report.timeToFirstFoodDeltaPercent = DeltaPercent(
            report.withoutTrails.meanTimeToFirstFood,
            report.withTrails.meanTimeToFirstFood,
            lowerIsBetter: true);
        report.revisitRatioDeltaPercent = DeltaPercent(
            report.withoutTrails.meanRevisitRatio,
            report.withTrails.meanRevisitRatio,
            lowerIsBetter: true);

        int passed = 0;
        if (report.foodReturnedDeltaPercent >= MinImprovementPercent)
            passed++;
        if (report.foragingEfficiencyDeltaPercent >= MinImprovementPercent)
            passed++;
        if (report.timeToFirstFoodDeltaPercent >= MinTimeToFirstFoodImprovementPercent)
            passed++;

        report.metricsPassed = passed;

        bool trailsUsed = report.withTrails.meanScentInfluencedSteps > 0f &&
                          report.withTrails.meanScentDeposits > 0f;
        bool revisitNotWorse = report.withTrails.meanRevisitRatio <=
                               report.withoutTrails.meanRevisitRatio * 1.05f;

        if (!trailsUsed)
        {
            report.verdict = SwarmTrailAblationVerdict.Inconclusive;
            report.verdictSummary =
                "Trail runs recorded zero scent deposits or zero scent-influenced steps. " +
                "Check that scent trails are enabled and deposit weights are > 0.";
            return report;
        }

        if (passed >= report.metricsRequired && revisitNotWorse)
        {
            report.verdict = SwarmTrailAblationVerdict.TrailsWorking;
            report.verdictSummary =
                $"Trails PASS: {passed}/{report.metricsRequired} primary metrics improved by at least " +
                $"{MinImprovementPercent:F0}% (food returned, efficiency, or faster first food), " +
                "and revisit ratio did not worsen.";
            return report;
        }

        report.verdict = SwarmTrailAblationVerdict.TrailsNotWorking;
        report.verdictSummary =
            $"Trails FAIL: only {passed}/{report.metricsRequired} primary metrics improved enough " +
            $"(need {MinImprovementPercent:F0}% better food return or efficiency, or " +
            $"{MinTimeToFirstFoodImprovementPercent:F0}% faster first food)" +
            (revisitNotWorse ? "." : ", and revisit ratio worsened.");
        return report;
    }

    public static SwarmTrailAblationConditionStats BuildStats(IReadOnlyList<SwarmForagingEpisodeSnapshot> runs)
    {
        var stats = new SwarmTrailAblationConditionStats();
        if (runs == null || runs.Count == 0)
            return stats;

        stats.runCount = runs.Count;
        float foodReturned = 0f;
        float efficiency = 0f;
        float timeToFirstFood = 0f;
        float revisit = 0f;
        float scentDeposits = 0f;
        float scentSteps = 0f;
        int validFirstFood = 0;

        for (int i = 0; i < runs.Count; i++)
        {
            SwarmForagingEpisodeSnapshot run = runs[i];
            foodReturned += run.foodReturned;
            efficiency += run.foragingEfficiency;
            revisit += run.revisitRatio;
            scentDeposits += run.scentDeposits;
            scentSteps += run.scentInfluencedSteps;
            if (run.timeToFirstFood >= 0)
            {
                timeToFirstFood += run.timeToFirstFood;
                validFirstFood++;
            }
        }

        stats.meanFoodReturned = foodReturned / runs.Count;
        stats.meanForagingEfficiency = efficiency / runs.Count;
        stats.meanRevisitRatio = revisit / runs.Count;
        stats.meanScentDeposits = scentDeposits / runs.Count;
        stats.meanScentInfluencedSteps = scentSteps / runs.Count;
        stats.meanTimeToFirstFood = validFirstFood > 0 ? timeToFirstFood / validFirstFood : -1f;
        return stats;
    }

    static float DeltaPercent(float baseline, float candidate, bool lowerIsBetter = false)
    {
        if (Mathf.Approximately(baseline, 0f))
        {
            if (Mathf.Approximately(candidate, 0f))
                return 0f;

            return lowerIsBetter ? 0f : 100f;
        }

        float delta = lowerIsBetter
            ? (baseline - candidate) / baseline * 100f
            : (candidate - baseline) / baseline * 100f;
        return delta;
    }
}
