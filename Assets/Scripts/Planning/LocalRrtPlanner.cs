using System.Collections.Generic;
using UnityEngine;

public readonly struct LocalRrtEdge
{
    public readonly Vector2Int From;
    public readonly Vector2Int To;

    public LocalRrtEdge(Vector2Int from, Vector2Int to)
    {
        From = from;
        To = to;
    }
}

/// <summary>
/// Grid RRT that plans only through passages marked open in a local discovery map.
/// </summary>
public static class LocalRrtPlanner
{
    struct Node
    {
        public int X;
        public int Y;
        public int Parent;
    }

    public static bool TryFindPath(
        MazeLocalDiscoveryMap map,
        int startX,
        int startY,
        int goalX,
        int goalY,
        int maxIterations,
        float goalBias,
        System.Random rng,
        out List<Vector2Int> path,
        out List<LocalRrtEdge> treeEdges,
        out int nodesCreated)
    {
        return TryFindPath(
            map,
            startX,
            startY,
            goalX,
            goalY,
            maxIterations,
            goalBias,
            rng,
            null,
            out path,
            out treeEdges,
            out nodesCreated);
    }

    public static bool TryFindPath(
        MazeLocalDiscoveryMap map,
        int startX,
        int startY,
        int goalX,
        int goalY,
        int maxIterations,
        float goalBias,
        System.Random rng,
        IReadOnlyList<SwarmRrtGraftSeed> graftSeeds,
        out List<Vector2Int> path,
        out List<LocalRrtEdge> treeEdges,
        out int nodesCreated)
    {
        path = null;
        treeEdges = new List<LocalRrtEdge>();
        nodesCreated = 0;

        if (map == null || rng == null || maxIterations <= 0)
            return false;

        if (!map.IsCellDiscovered(startX, startY))
            return false;

        var nodes = new List<Node>
        {
            new Node { X = startX, Y = startY, Parent = -1 }
        };

        AppendGraftSeeds(map, startX, startY, graftSeeds, nodes);

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            int sampleX;
            int sampleY;
            if (rng.NextDouble() < goalBias && map.IsCellDiscovered(goalX, goalY))
            {
                sampleX = goalX;
                sampleY = goalY;
            }
            else if (!map.TryRandomDiscoveredCell(rng, out sampleX, out sampleY))
            {
                continue;
            }

            int nearestIndex = FindNearestNode(nodes, sampleX, sampleY);
            Node nearest = nodes[nearestIndex];

            if (!TryExtendToward(map, nearest.X, nearest.Y, sampleX, sampleY, 12, out int newX, out int newY))
                continue;

            if (!map.IsCellDiscovered(newX, newY))
                continue;

            if (!IsInBounds(map, newX, newY))
                continue;

            nodes.Add(new Node { X = newX, Y = newY, Parent = nearestIndex });
            nodesCreated = nodes.Count;

            if (newX == goalX && newY == goalY)
            {
                path = ReconstructPath(nodes, nodes.Count - 1);
                treeEdges = BuildTreeEdges(nodes);
                return true;
            }
        }

        treeEdges = BuildTreeEdges(nodes);
        return false;
    }

    static List<LocalRrtEdge> BuildTreeEdges(List<Node> nodes)
    {
        var edges = new List<LocalRrtEdge>(Mathf.Max(0, nodes.Count - 1));
        for (int i = 1; i < nodes.Count; i++)
        {
            Node node = nodes[i];
            if (node.Parent < 0)
                continue;

            Node parent = nodes[node.Parent];
            int manhattan = Mathf.Abs(node.X - parent.X) + Mathf.Abs(node.Y - parent.Y);
            if (manhattan <= 0 || manhattan > 12)
                continue;

            edges.Add(new LocalRrtEdge(
                new Vector2Int(parent.X, parent.Y),
                new Vector2Int(node.X, node.Y)));
        }

        return edges;
    }

    static int FindNearestNode(List<Node> nodes, int sampleX, int sampleY)
    {
        int bestIndex = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i < nodes.Count; i++)
        {
            float dx = nodes[i].X - sampleX;
            float dy = nodes[i].Y - sampleY;
            float dist = dx * dx + dy * dy;
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    static bool TryExtendToward(
        MazeLocalDiscoveryMap map,
        int fromX,
        int fromY,
        int towardX,
        int towardY,
        int maxSteps,
        out int newX,
        out int newY)
    {
        newX = fromX;
        newY = fromY;

        int steps = Mathf.Max(1, maxSteps);
        for (int i = 0; i < steps; i++)
        {
            int dx = towardX - newX;
            int dz = towardY - newY;
            if (dx == 0 && dz == 0)
                break;

            int stepX = Mathf.Clamp(dx, -1, 1);
            int stepZ = Mathf.Clamp(dz, -1, 1);

            if (!map.IsPassageTraversable(newX, newY, stepX, stepZ))
                break;

            int nextX = newX + stepX;
            int nextY = newY + stepZ;
            if (!map.IsCellDiscovered(nextX, nextY))
                break;

            newX = nextX;
            newY = nextY;
        }

        return newX != fromX || newY != fromY;
    }

    static bool IsInBounds(MazeLocalDiscoveryMap map, int cellX, int cellY)
    {
        return cellX >= 0 && cellY >= 0 &&
               cellX < map.WidthCells && cellY < map.HeightCells;
    }

    static List<Vector2Int> ReconstructPath(List<Node> nodes, int goalIndex)
    {
        var reversed = new List<Vector2Int>();
        int index = goalIndex;

        while (index >= 0)
        {
            Node node = nodes[index];
            reversed.Add(new Vector2Int(node.X, node.Y));
            index = node.Parent;
        }

        reversed.Reverse();
        return reversed;
    }

    static void AppendGraftSeeds(
        MazeLocalDiscoveryMap map,
        int startX,
        int startY,
        IReadOnlyList<SwarmRrtGraftSeed> graftSeeds,
        List<Node> nodes)
    {
        if (graftSeeds == null || graftSeeds.Count == 0)
            return;

        var seen = new HashSet<long> { CellKey(startX, startY) };
        var pending = new List<SwarmRrtGraftSeed>(graftSeeds);
        int maxPasses = pending.Count + 1;

        while (pending.Count > 0 && maxPasses-- > 0)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                SwarmRrtGraftSeed seed = pending[i];
                if (seed.X == startX && seed.Y == startY)
                {
                    pending.RemoveAt(i);
                    continue;
                }

                if (!map.IsCellDiscovered(seed.X, seed.Y))
                    continue;

                long key = CellKey(seed.X, seed.Y);
                if (seen.Contains(key))
                {
                    pending.RemoveAt(i);
                    continue;
                }

                int parentIndex = FindNodeIndex(nodes, seed.ParentX, seed.ParentY);
                if (parentIndex < 0)
                    continue;

                if (!IsTraversableStep(map, seed.ParentX, seed.ParentY, seed.X, seed.Y))
                    continue;

                seen.Add(key);
                nodes.Add(new Node
                {
                    X = seed.X,
                    Y = seed.Y,
                    Parent = parentIndex
                });
                pending.RemoveAt(i);
            }
        }
    }

    static bool IsTraversableStep(MazeLocalDiscoveryMap map, int fromX, int fromY, int toX, int toY)
    {
        int dx = toX - fromX;
        int dy = toY - fromY;
        if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1)
            return false;

        return map.IsPassageTraversable(fromX, fromY, dx, dy);
    }

    static int FindNodeIndex(List<Node> nodes, int cellX, int cellY)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].X == cellX && nodes[i].Y == cellY)
                return i;
        }

        return -1;
    }

    static long CellKey(int cellX, int cellY)
    {
        return ((long)cellX << 32) | (uint)cellY;
    }
}
