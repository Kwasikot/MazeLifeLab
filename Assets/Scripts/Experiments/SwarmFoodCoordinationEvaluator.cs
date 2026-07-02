using System;
using System.Collections.Generic;
using UnityEngine;

public enum SwarmFoodCoordinationVerdict
{
    Inconclusive = 0,
    CoordinationNotWorking = 1,
    CoordinationWorking = 2
}

[Serializable]
public struct SwarmFoodCoordinationConditionStats
{
    public int runCount;
    public float meanFoodReturned;
    public float meanForagingEfficiency;
    public float meanTimeToFirstFood;
    public float meanRevisitRatio;
    public float meanSignalsEmitted;
    public float meanCoordinationInfluencedSteps;
}

[Serializable]
public struct SwarmFoodCoordinationAblationReport
{
    public bool isComplete;
    public SwarmFoodCoordinationVerdict verdict;
    public string verdictSummary;
    public SwarmFoodCoordinationMode bestMode;
    public SwarmFoodCoordinationConditionStats baselineNone;
    public SwarmFoodCoordinationConditionStats hiveBulletin;
    public SwarmFoodCoordinationConditionStats localBroadcast;
    public SwarmFoodCoordinationConditionStats globalBroadcast;
    public float bestFoodReturnedDeltaPercent;
    public float bestForagingEfficiencyDeltaPercent;
    public int metricsPassed;
    public int metricsRequired;
}

public static class SwarmFoodCoordinationEvaluator
{
    const float MinImprovementPercent = 5f;
    const float MinTimeToFirstFoodImprovementPercent = 10f;

    public static SwarmFoodCoordinationAblationReport Evaluate(
        IReadOnlyList<SwarmForagingEpisodeSnapshot> noneRuns,
        IReadOnlyList<SwarmForagingEpisodeSnapshot> hiveRuns,
        IReadOnlyList<SwarmForagingEpisodeSnapshot> localRuns,
        IReadOnlyList<SwarmForagingEpisodeSnapshot> globalRuns)
    {
        var report = new SwarmFoodCoordinationAblationReport
        {
            isComplete = true,
            metricsRequired = 2,
            baselineNone = BuildStats(noneRuns),
            hiveBulletin = BuildStats(hiveRuns),
            localBroadcast = BuildStats(localRuns),
            globalBroadcast = BuildStats(globalRuns)
        };

        if (noneRuns == null || noneRuns.Count == 0)
        {
            report.verdict = SwarmFoodCoordinationVerdict.Inconclusive;
            report.verdictSummary = "Need at least one None (independent) run.";
            return report;
        }

        int bestPassed = 0;
        float bestFoodDelta = float.NegativeInfinity;
        float bestEfficiencyDelta = float.NegativeInfinity;
        SwarmFoodCoordinationMode bestMode = SwarmFoodCoordinationMode.None;

        EvaluateMode(
            report.baselineNone,
            report.hiveBulletin,
            SwarmFoodCoordinationMode.HiveBulletin,
            ref bestPassed,
            ref bestFoodDelta,
            ref bestEfficiencyDelta,
            ref bestMode);
        EvaluateMode(
            report.baselineNone,
            report.localBroadcast,
            SwarmFoodCoordinationMode.LocalBroadcast,
            ref bestPassed,
            ref bestFoodDelta,
            ref bestEfficiencyDelta,
            ref bestMode);
        EvaluateMode(
            report.baselineNone,
            report.globalBroadcast,
            SwarmFoodCoordinationMode.GlobalBroadcast,
            ref bestPassed,
            ref bestFoodDelta,
            ref bestEfficiencyDelta,
            ref bestMode);

        report.bestMode = bestMode;
        report.bestFoodReturnedDeltaPercent = bestFoodDelta;
        report.bestForagingEfficiencyDeltaPercent = bestEfficiencyDelta;
        report.metricsPassed = bestPassed;

        if (bestMode == SwarmFoodCoordinationMode.None || bestPassed < report.metricsRequired)
        {
            report.verdict = SwarmFoodCoordinationVerdict.CoordinationNotWorking;
            report.verdictSummary =
                $"Coordination FAIL: best mode={bestMode.ToReportLabel()} passed {bestPassed}/{report.metricsRequired} primary metrics " +
                $"(need {MinImprovementPercent:F0}% food or efficiency gain, or {MinTimeToFirstFoodImprovementPercent:F0}% faster first food).";
            return report;
        }

        report.verdict = SwarmFoodCoordinationVerdict.CoordinationWorking;
        report.verdictSummary =
            $"Coordination PASS: {bestMode.ToReportLabel()} improved {bestPassed}/{report.metricsRequired} primary metrics vs None.";
        return report;
    }

    static void EvaluateMode(
        SwarmFoodCoordinationConditionStats baseline,
        SwarmFoodCoordinationConditionStats treatment,
        SwarmFoodCoordinationMode mode,
        ref int bestPassed,
        ref float bestFoodDelta,
        ref float bestEfficiencyDelta,
        ref SwarmFoodCoordinationMode bestMode)
    {
        if (treatment.runCount <= 0)
            return;

        if (mode != SwarmFoodCoordinationMode.None && treatment.meanCoordinationInfluencedSteps <= 0f)
            return;

        if (treatment.meanRevisitRatio > baseline.meanRevisitRatio * 1.05f)
            return;

        int passed = CountPassedMetrics(baseline, treatment);
        float foodDelta = DeltaPercent(baseline.meanFoodReturned, treatment.meanFoodReturned);
        float efficiencyDelta = DeltaPercent(baseline.meanForagingEfficiency, treatment.meanForagingEfficiency);

        if (passed <= bestPassed)
            return;

        bestPassed = passed;
        bestFoodDelta = foodDelta;
        bestEfficiencyDelta = efficiencyDelta;
        bestMode = mode;
    }

    static int CountPassedMetrics(
        SwarmFoodCoordinationConditionStats baseline,
        SwarmFoodCoordinationConditionStats treatment)
    {
        int passed = 0;
        if (DeltaPercent(baseline.meanFoodReturned, treatment.meanFoodReturned) >= MinImprovementPercent)
            passed++;
        if (DeltaPercent(baseline.meanForagingEfficiency, treatment.meanForagingEfficiency) >= MinImprovementPercent)
            passed++;
        if (DeltaPercent(baseline.meanTimeToFirstFood, treatment.meanTimeToFirstFood, lowerIsBetter: true) >=
            MinTimeToFirstFoodImprovementPercent)
        {
            passed++;
        }

        return passed;
    }

    public static SwarmFoodCoordinationConditionStats BuildStats(IReadOnlyList<SwarmForagingEpisodeSnapshot> runs)
    {
        var stats = new SwarmFoodCoordinationConditionStats();
        if (runs == null || runs.Count == 0)
            return stats;

        stats.runCount = runs.Count;
        float foodReturned = 0f;
        float efficiency = 0f;
        float timeToFirstFood = 0f;
        float revisit = 0f;
        float signals = 0f;
        float coordSteps = 0f;
        int validFirstFood = 0;

        for (int i = 0; i < runs.Count; i++)
        {
            SwarmForagingEpisodeSnapshot run = runs[i];
            foodReturned += run.foodReturned;
            efficiency += run.foragingEfficiency;
            revisit += run.revisitRatio;
            signals += run.signalsEmitted;
            coordSteps += run.coordinationInfluencedSteps;
            if (run.timeToFirstFood >= 0)
            {
                timeToFirstFood += run.timeToFirstFood;
                validFirstFood++;
            }
        }

        stats.meanFoodReturned = foodReturned / runs.Count;
        stats.meanForagingEfficiency = efficiency / runs.Count;
        stats.meanRevisitRatio = revisit / runs.Count;
        stats.meanSignalsEmitted = signals / runs.Count;
        stats.meanCoordinationInfluencedSteps = coordSteps / runs.Count;
        stats.meanTimeToFirstFood = validFirstFood > 0 ? timeToFirstFood / validFirstFood : -1f;
        return stats;
    }

    static float DeltaPercent(float baseline, float candidate, bool lowerIsBetter = false)
    {
        if (Mathf.Approximately(baseline, 0f))
            return Mathf.Approximately(candidate, 0f) ? 0f : lowerIsBetter ? 0f : 100f;

        return lowerIsBetter
            ? (baseline - candidate) / baseline * 100f
            : (candidate - baseline) / baseline * 100f;
    }
}
