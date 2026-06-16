using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Incrementally discovered maze topology for partial-observability agents.
/// Planning code must use only data written by <see cref="Sense"/>.
/// </summary>
public class MazeLocalDiscoveryMap
{
    readonly HashSet<long> _discoveredCells = new HashSet<long>();
    readonly HashSet<long> _knownOpenPassages = new HashSet<long>();
    readonly HashSet<long> _knownBlockedPassages = new HashSet<long>();
    readonly List<Vector2Int> _discoveredCellList = new List<Vector2Int>();

    int _widthCells;
    int _heightCells;

    public int DiscoveredCellCount => _discoveredCells.Count;
    public int KnownOpenPassageCount => _knownOpenPassages.Count;
    public int WidthCells => _widthCells;
    public int HeightCells => _heightCells;

    public void Reset(int widthCells, int heightCells)
    {
        _widthCells = widthCells;
        _heightCells = heightCells;
        _discoveredCells.Clear();
        _knownOpenPassages.Clear();
        _knownBlockedPassages.Clear();
        _discoveredCellList.Clear();
    }

    /// <summary>
    /// Update discovered topology using ground-truth geometry visible from the agent.
    /// This simulates sensing; callers must not use truth data during planning.
    /// </summary>
    public void Sense(MazeGenerator truth, Vector3 worldPosition, int sensorRadiusCells)
    {
        if (truth == null || sensorRadiusCells < 0)
            return;

        if (!truth.TryWorldToCell(worldPosition, out int originX, out int originY))
            return;

        int radius = Mathf.Max(0, sensorRadiusCells);
        var queue = new Queue<(int x, int y, int depth)>();
        var visited = new HashSet<long>();

        long originKey = CellKey(originX, originY);
        queue.Enqueue((originX, originY, 0));
        visited.Add(originKey);

        while (queue.Count > 0)
        {
            (int cx, int cy, int depth) = queue.Dequeue();
            MarkCellDiscovered(cx, cy);

            foreach (int[] dir in CardinalDirections)
            {
                int dirX = dir[0];
                int dirZ = dir[1];
                RecordPassage(truth, cx, cy, dirX, dirZ);

                if (depth >= radius)
                    continue;

                if (!truth.IsPassageOpen(cx, cy, dirX, dirZ))
                    continue;

                int nx = cx + dirX;
                int ny = cy + dirZ;
                if (!truth.IsCellInBounds(nx, ny))
                    continue;

                long neighborKey = CellKey(nx, ny);
                if (visited.Add(neighborKey))
                    queue.Enqueue((nx, ny, depth + 1));
            }
        }
    }

    public bool IsCellDiscovered(int cellX, int cellY)
    {
        return _discoveredCells.Contains(CellKey(cellX, cellY));
    }

    public bool IsPassageKnownOpen(int cellX, int cellY, int dirX, int dirZ)
    {
        return _knownOpenPassages.Contains(PassageKey(cellX, cellY, dirX, dirZ));
    }

    public bool IsPassageKnownBlocked(int cellX, int cellY, int dirX, int dirZ)
    {
        return _knownBlockedPassages.Contains(PassageKey(cellX, cellY, dirX, dirZ));
    }

    /// <summary>Unknown passages are treated as blocked for conservative planning.</summary>
    public bool IsPassageTraversable(int cellX, int cellY, int dirX, int dirZ)
    {
        return IsPassageKnownOpen(cellX, cellY, dirX, dirZ);
    }

    public float CoveragePercent()
    {
        if (_widthCells <= 0 || _heightCells <= 0)
            return 0f;

        int total = _widthCells * _heightCells;
        return 100f * _discoveredCells.Count / total;
    }

    public bool TryRandomDiscoveredCell(System.Random rng, out int cellX, out int cellY)
    {
        cellX = 0;
        cellY = 0;
        if (_discoveredCellList.Count == 0 || rng == null)
            return false;

        Vector2Int cell = _discoveredCellList[rng.Next(_discoveredCellList.Count)];
        cellX = cell.x;
        cellY = cell.y;
        return true;
    }

    public bool TryFindFrontierMove(int cellX, int cellY, System.Random rng, out int dirX, out int dirZ)
    {
        dirX = 0;
        dirZ = 0;
        var candidates = new List<int[]>();

        foreach (int[] dir in CardinalDirections)
        {
            if (!IsPassageTraversable(cellX, cellY, dir[0], dir[1]))
                continue;

            int nx = cellX + dir[0];
            int ny = cellY + dir[1];
            if (!IsInBounds(nx, ny))
                continue;

            if (!IsCellDiscovered(nx, ny))
                candidates.Add(dir);
        }

        if (candidates.Count == 0)
            return false;

        int[] pick = candidates[rng.Next(candidates.Count)];
        dirX = pick[0];
        dirZ = pick[1];
        return true;
    }

    /// <summary>
    /// BFS through discovered cells to reach a frontier (cell adjacent to undiscovered space).
    /// Returns the first step from (startX, startY) toward the best frontier.
    /// </summary>
    public bool TryFindStepTowardFrontier(
        int startX,
        int startY,
        int goalX,
        int goalY,
        out int dirX,
        out int dirZ)
    {
        dirX = 0;
        dirZ = 0;

        if (TryFindImmediateFrontierStep(startX, startY, out dirX, out dirZ))
            return true;

        var queue = new Queue<Vector2Int>();
        var visited = new HashSet<long>();
        var firstStep = new Dictionary<long, Vector2Int>();

        long startKey = CellKey(startX, startY);
        queue.Enqueue(new Vector2Int(startX, startY));
        visited.Add(startKey);

        int bestX = -1;
        int bestY = -1;
        int bestScore = int.MaxValue;

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            if (HasUndiscoveredNeighbor(cell.x, cell.y))
            {
                int score = ManhattanDistance(cell.x, cell.y, goalX, goalY);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestX = cell.x;
                    bestY = cell.y;
                }
            }

            foreach (int[] dir in CardinalDirections)
            {
                if (!IsPassageTraversable(cell.x, cell.y, dir[0], dir[1]))
                    continue;

                int nx = cell.x + dir[0];
                int ny = cell.y + dir[1];
                if (!IsInBounds(nx, ny) || !IsCellDiscovered(nx, ny))
                    continue;

                long key = CellKey(nx, ny);
                if (!visited.Add(key))
                    continue;

                Vector2Int step = cell.x == startX && cell.y == startY
                    ? new Vector2Int(dir[0], dir[1])
                    : firstStep[CellKey(cell.x, cell.y)];

                firstStep[key] = step;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        if (bestX < 0)
            return false;

        Vector2Int first = firstStep[CellKey(bestX, bestY)];
        dirX = first.x;
        dirZ = first.y;
        return dirX != 0 || dirZ != 0;
    }

    /// <summary>
    /// Greedy step through known-open passages that reduces Manhattan distance to goal.
    /// </summary>
    public bool TryFindGreedyGoalStep(int cellX, int cellY, int goalX, int goalY, out int dirX, out int dirZ)
    {
        dirX = 0;
        dirZ = 0;
        int bestDist = ManhattanDistance(cellX, cellY, goalX, goalY);
        bool found = false;

        foreach (int[] dir in CardinalDirections)
        {
            if (!IsPassageTraversable(cellX, cellY, dir[0], dir[1]))
                continue;

            int nx = cellX + dir[0];
            int ny = cellY + dir[1];
            if (!IsInBounds(nx, ny))
                continue;

            int dist = ManhattanDistance(nx, ny, goalX, goalY);
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            dirX = dir[0];
            dirZ = dir[1];
            found = true;
        }

        return found;
    }

    bool TryFindImmediateFrontierStep(int cellX, int cellY, out int dirX, out int dirZ)
    {
        dirX = 0;
        dirZ = 0;

        foreach (int[] dir in CardinalDirections)
        {
            if (!IsPassageTraversable(cellX, cellY, dir[0], dir[1]))
                continue;

            int nx = cellX + dir[0];
            int ny = cellY + dir[1];
            if (!IsInBounds(nx, ny))
                continue;

            if (!IsCellDiscovered(nx, ny))
            {
                dirX = dir[0];
                dirZ = dir[1];
                return true;
            }
        }

        return false;
    }

    bool HasUndiscoveredNeighbor(int cellX, int cellY)
    {
        foreach (int[] dir in CardinalDirections)
        {
            if (!IsPassageTraversable(cellX, cellY, dir[0], dir[1]))
                continue;

            int nx = cellX + dir[0];
            int ny = cellY + dir[1];
            if (!IsInBounds(nx, ny))
                continue;

            if (!IsCellDiscovered(nx, ny))
                return true;
        }

        return false;
    }

    static int ManhattanDistance(int ax, int ay, int bx, int by)
    {
        return Mathf.Abs(ax - bx) + Mathf.Abs(ay - by);
    }

    void RecordPassage(MazeGenerator truth, int cellX, int cellY, int dirX, int dirZ)
    {
        long key = PassageKey(cellX, cellY, dirX, dirZ);
        if (_knownOpenPassages.Contains(key) || _knownBlockedPassages.Contains(key))
            return;

        if (truth.HasVisibleWallBetween(cellX, cellY, dirX, dirZ))
            _knownBlockedPassages.Add(key);
        else
            _knownOpenPassages.Add(key);
    }

    void MarkCellDiscovered(int cellX, int cellY)
    {
        long key = CellKey(cellX, cellY);
        if (!_discoveredCells.Add(key))
            return;

        _discoveredCellList.Add(new Vector2Int(cellX, cellY));
    }

    bool IsInBounds(int cellX, int cellY)
    {
        return cellX >= 0 && cellX < _widthCells &&
               cellY >= 0 && cellY < _heightCells;
    }

    static long CellKey(int cellX, int cellY)
    {
        return ((long)cellX << 32) | (uint)cellY;
    }

    static long PassageKey(int cellX, int cellY, int dirX, int dirZ)
    {
        return (CellKey(cellX, cellY) << 4) | (uint)((dirX + 1) << 2 | (dirZ + 1));
    }

    static readonly int[][] CardinalDirections =
    {
        new[] { 0, 1 },
        new[] { 1, 0 },
        new[] { 0, -1 },
        new[] { -1, 0 }
    };
}
