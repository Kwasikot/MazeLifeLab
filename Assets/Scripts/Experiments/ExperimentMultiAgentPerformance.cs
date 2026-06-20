using UnityEngine;

public static class ExperimentMultiAgentPerformance
{
    public struct LocalRrtTuning
    {
        public int RrtIterationsPerStep;
        public int ReplanIntervalSteps;
        public bool DrawTree;
        public bool DrawPath;
    }

    public static LocalRrtTuning ResolveLocalRrtTuning(
        int agentIndex,
        int agentCount,
        int mazeWidthCells,
        int mazeHeightCells,
        bool drawTreeForFirstAgentOnly)
    {
        int iterations = 128;
        int replan = 4;

        if (agentCount > 2)
        {
            iterations = 96;
            replan = 6;
        }

        if (agentCount > 4)
        {
            iterations = 64;
            replan = 8;
        }

        if (agentCount > 8)
        {
            iterations = 48;
            replan = 10;
        }

        int mazeCells = mazeWidthCells * mazeHeightCells;
        if (mazeCells >= 2500)
        {
            iterations = Mathf.Max(32, iterations - 16);
            replan += 2;
        }

        if (mazeCells >= 10000)
        {
            iterations = Mathf.Max(32, iterations - 16);
            replan += 2;
        }

        bool drawTree = !drawTreeForFirstAgentOnly || agentIndex == 0;
        bool drawPath = drawTreeForFirstAgentOnly
            ? drawTree
            : agentIndex < Mathf.Min(3, agentCount);

        return new LocalRrtTuning
        {
            RrtIterationsPerStep = iterations,
            ReplanIntervalSteps = replan,
            DrawTree = drawTree,
            DrawPath = drawPath
        };
    }
}
