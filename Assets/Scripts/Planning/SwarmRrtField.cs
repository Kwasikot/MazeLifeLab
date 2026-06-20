using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared environmental store of RRT tree edges deposited by agents during an episode.
/// This is branch geometry, not a shared discovery map.
/// </summary>
public sealed class SwarmRrtField
{
    struct EdgeRecord
    {
        public int FromX;
        public int FromY;
        public int ToX;
        public int ToY;
        public int DepositorAgent;
    }

    readonly List<EdgeRecord> _edges = new List<EdgeRecord>();
    readonly HashSet<long> _edgeKeys = new HashSet<long>();
    int _totalDeposits;

    bool[,] _reachableBuffer;
    int[,] _parentXBuffer;
    int[,] _parentYBuffer;
    int _bufferWidth;
    int _bufferHeight;
    readonly HashSet<long> _graftSeen = new HashSet<long>();
    readonly Queue<Vector2Int> _bfsQueue = new Queue<Vector2Int>();

    const int MaxEdgesScannedForGraft = 256;
    const int MaxEdgesDepositedPerReplan = 64;

    public int TotalEdgeDeposits => _totalDeposits;
    public int SharedEdgeCount => _edges.Count;

    public void Reset()
    {
        _edges.Clear();
        _edgeKeys.Clear();
        _totalDeposits = 0;
        _reachableBuffer = null;
        _parentXBuffer = null;
        _parentYBuffer = null;
        _bufferWidth = 0;
        _bufferHeight = 0;
        _graftSeen.Clear();
        _bfsQueue.Clear();
    }

    public void DepositTreeEdges(int agentIndex, IReadOnlyList<LocalRrtEdge> edges)
    {
        if (edges == null || edges.Count == 0)
            return;

        int start = Mathf.Max(0, edges.Count - MaxEdgesDepositedPerReplan);
        for (int i = start; i < edges.Count; i++)
        {
            LocalRrtEdge edge = edges[i];
            long key = EdgeKey(edge.From.x, edge.From.y, edge.To.x, edge.To.y, agentIndex);
            if (!_edgeKeys.Add(key))
                continue;

            _edges.Add(new EdgeRecord
            {
                FromX = edge.From.x,
                FromY = edge.From.y,
                ToX = edge.To.x,
                ToY = edge.To.y,
                DepositorAgent = agentIndex
            });
            _totalDeposits++;
        }
    }

    /// <summary>
    /// Collect foreign tree endpoints reachable in the reader's discovered map,
    /// connected back toward start via known passages.
    /// </summary>
    public int CollectGraftSeeds(
        MazeLocalDiscoveryMap map,
        int readerAgentIndex,
        int startX,
        int startY,
        List<SwarmRrtGraftSeed> outSeeds)
    {
        outSeeds.Clear();
        if (map == null || _edges.Count == 0)
            return 0;

        BuildReachableParentGrid(
            map,
            startX,
            startY,
            out bool[,] reachable,
            out int[,] bfsParentX,
            out int[,] bfsParentY);

        var seen = _graftSeen;
        seen.Clear();
        int startIndex = Mathf.Max(0, _edges.Count - MaxEdgesScannedForGraft);
        for (int i = startIndex; i < _edges.Count; i++)
        {
            EdgeRecord edge = _edges[i];
            if (edge.DepositorAgent == readerAgentIndex)
                continue;

            TryAddGraftSeed(map, reachable, bfsParentX, bfsParentY, edge.ToX, edge.ToY, seen, outSeeds);
            TryAddGraftSeed(map, reachable, bfsParentX, bfsParentY, edge.FromX, edge.FromY, seen, outSeeds);
        }

        return outSeeds.Count;
    }

    static void TryAddGraftSeed(
        MazeLocalDiscoveryMap map,
        bool[,] reachable,
        int[,] bfsParentX,
        int[,] bfsParentY,
        int cellX,
        int cellY,
        HashSet<long> seen,
        List<SwarmRrtGraftSeed> outSeeds)
    {
        if (!map.IsCellDiscovered(cellX, cellY))
            return;

        if (!IsInBounds(map, cellX, cellY) || !reachable[cellX, cellY])
            return;

        long key = CellKey(cellX, cellY);
        if (!seen.Add(key))
            return;

        int parentX = bfsParentX[cellX, cellY];
        int parentY = bfsParentY[cellX, cellY];
        if (parentX < 0 || parentY < 0)
            return;

        outSeeds.Add(new SwarmRrtGraftSeed(cellX, cellY, parentX, parentY));
    }

    void EnsureReachableBuffers(int width, int height)
    {
        if (_reachableBuffer != null && _bufferWidth == width && _bufferHeight == height)
            return;

        _bufferWidth = width;
        _bufferHeight = height;
        _reachableBuffer = new bool[width, height];
        _parentXBuffer = new int[width, height];
        _parentYBuffer = new int[width, height];
    }

    void BuildReachableParentGrid(
        MazeLocalDiscoveryMap map,
        int startX,
        int startY,
        out bool[,] reachable,
        out int[,] parentX,
        out int[,] parentY)
    {
        EnsureReachableBuffers(map.WidthCells, map.HeightCells);
        reachable = _reachableBuffer;
        parentX = _parentXBuffer;
        parentY = _parentYBuffer;

        for (int y = 0; y < map.HeightCells; y++)
        {
            for (int x = 0; x < map.WidthCells; x++)
            {
                reachable[x, y] = false;
                parentX[x, y] = -1;
                parentY[x, y] = -1;
            }
        }

        if (!map.IsCellDiscovered(startX, startY))
            return;

        var queue = _bfsQueue;
        queue.Clear();
        reachable[startX, startY] = true;
        queue.Enqueue(new Vector2Int(startX, startY));

        int[][] directions =
        {
            new[] { 0, 1 },
            new[] { 1, 0 },
            new[] { 0, -1 },
            new[] { -1, 0 }
        };

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            for (int i = 0; i < directions.Length; i++)
            {
                int dirX = directions[i][0];
                int dirZ = directions[i][1];
                if (!map.IsPassageTraversable(cell.x, cell.y, dirX, dirZ))
                    continue;

                int nx = cell.x + dirX;
                int ny = cell.y + dirZ;
                if (!IsInBounds(map, nx, ny) || !map.IsCellDiscovered(nx, ny))
                    continue;

                if (reachable[nx, ny])
                    continue;

                reachable[nx, ny] = true;
                parentX[nx, ny] = cell.x;
                parentY[nx, ny] = cell.y;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }
    }

    static bool IsInBounds(MazeLocalDiscoveryMap map, int cellX, int cellY)
    {
        return cellX >= 0 && cellY >= 0 &&
               cellX < map.WidthCells && cellY < map.HeightCells;
    }

    static long CellKey(int cellX, int cellY)
    {
        return ((long)cellX << 32) | (uint)cellY;
    }

    static long EdgeKey(int fromX, int fromY, int toX, int toY, int agentIndex)
    {
        return CellKey(fromX, fromY) ^ (CellKey(toX, toY) << 1) ^ ((long)agentIndex << 62);
    }
}

public readonly struct SwarmRrtGraftSeed
{
    public readonly int X;
    public readonly int Y;
    public readonly int ParentX;
    public readonly int ParentY;

    public SwarmRrtGraftSeed(int x, int y, int parentX, int parentY)
    {
        X = x;
        Y = y;
        ParentX = parentX;
        ParentY = parentY;
    }
}
