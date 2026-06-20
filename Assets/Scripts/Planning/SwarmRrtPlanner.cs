using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Local RRT with optional grafting of foreign swarm tree nodes from <see cref="SwarmRrtField"/>.
/// </summary>
public static class SwarmRrtPlanner
{
    static readonly List<SwarmRrtGraftSeed> GraftSeedBuffer = new List<SwarmRrtGraftSeed>();

    public static bool TryFindPath(
        MazeLocalDiscoveryMap map,
        int startX,
        int startY,
        int goalX,
        int goalY,
        int maxIterations,
        float goalBias,
        System.Random rng,
        SwarmRrtField swarmField,
        int agentIndex,
        out List<Vector2Int> path,
        out List<LocalRrtEdge> treeEdges,
        out int nodesCreated,
        out int graftSeedCount)
    {
        graftSeedCount = 0;
        IReadOnlyList<SwarmRrtGraftSeed> graftSeeds = null;

        if (swarmField != null)
        {
            graftSeedCount = swarmField.CollectGraftSeeds(map, agentIndex, startX, startY, GraftSeedBuffer);
            if (graftSeedCount > 0)
                graftSeeds = GraftSeedBuffer;
        }

        return LocalRrtPlanner.TryFindPath(
            map,
            startX,
            startY,
            goalX,
            goalY,
            maxIterations,
            goalBias,
            rng,
            graftSeeds,
            out path,
            out treeEdges,
            out nodesCreated);
    }
}
