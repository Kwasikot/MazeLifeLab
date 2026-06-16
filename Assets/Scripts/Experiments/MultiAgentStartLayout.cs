using System;
using System.Collections.Generic;

public static class MultiAgentStartLayout
{
    public static List<MazeCellIndex> ResolveStartCells(
        int agentCount,
        int mazeWidth,
        int mazeHeight,
        MazeCellIndex goalCell)
    {
        var starts = new List<MazeCellIndex>();
        if (agentCount <= 0 || mazeWidth <= 0 || mazeHeight <= 0)
            return starts;

        List<MazeCellIndex> corners = CollectCorners(mazeWidth, mazeHeight, goalCell);
        if (corners.Count == 0)
            return starts;

        corners.Sort((a, b) =>
            ManhattanDistance(b, goalCell).CompareTo(ManhattanDistance(a, goalCell)));

        for (int i = 0; i < corners.Count && starts.Count < agentCount; i++)
            starts.Add(corners[i]);

        while (starts.Count < agentCount)
        {
            MazeCellIndex best = default;
            int bestSpread = -1;
            bool found = false;

            for (int i = 0; i < corners.Count; i++)
            {
                MazeCellIndex candidate = corners[i];
                if (ContainsCell(starts, candidate))
                    continue;

                int spread = MinDistanceToStarts(candidate, starts);
                if (spread > bestSpread)
                {
                    bestSpread = spread;
                    best = candidate;
                    found = true;
                }
            }

            if (found)
            {
                starts.Add(best);
                continue;
            }

            var candidates = CollectExtendedCandidates(mazeWidth, mazeHeight, goalCell);
            bestSpread = -1;
            found = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                MazeCellIndex candidate = candidates[i];
                if (ContainsCell(starts, candidate))
                    continue;

                int spread = MinDistanceToStarts(candidate, starts);
                if (spread > bestSpread)
                {
                    bestSpread = spread;
                    best = candidate;
                    found = true;
                }
            }

            if (!found || ContainsCell(starts, best))
                break;

            starts.Add(best);
        }

        return starts;
    }

    static List<MazeCellIndex> CollectCorners(int mazeWidth, int mazeHeight, MazeCellIndex goalCell)
    {
        int lastX = mazeWidth - 1;
        int lastY = mazeHeight - 1;

        var corners = new[]
        {
            new MazeCellIndex(0, 0),
            new MazeCellIndex(lastX, 0),
            new MazeCellIndex(0, lastY),
            new MazeCellIndex(lastX, lastY)
        };

        var result = new List<MazeCellIndex>(corners.Length);
        for (int i = 0; i < corners.Length; i++)
        {
            if (!IsSameCell(corners[i], goalCell))
                AddUnique(result, corners[i]);
        }

        return result;
    }

    static List<MazeCellIndex> CollectExtendedCandidates(int mazeWidth, int mazeHeight, MazeCellIndex goalCell)
    {
        var candidates = CollectCorners(mazeWidth, mazeHeight, goalCell);

        int perimeterSlots = mazeWidth * 2 + mazeHeight * 2 - 4;
        for (int slot = 1; slot < perimeterSlots; slot++)
        {
            MazeCellIndex candidate = PerimeterCell(slot, mazeWidth, mazeHeight);
            if (!IsSameCell(candidate, goalCell))
                AddUnique(candidates, candidate);
        }

        for (int y = 0; y < mazeHeight; y++)
        {
            for (int x = 0; x < mazeWidth; x++)
            {
                var candidate = new MazeCellIndex(x, y);
                if (!IsSameCell(candidate, goalCell))
                    AddUnique(candidates, candidate);
            }
        }

        return candidates;
    }

    static int MinDistanceToStarts(MazeCellIndex candidate, List<MazeCellIndex> starts)
    {
        int min = int.MaxValue;
        for (int i = 0; i < starts.Count; i++)
            min = Math.Min(min, ManhattanDistance(candidate, starts[i]));

        return min;
    }

    static MazeCellIndex PerimeterCell(int slotIndex, int width, int height)
    {
        int perimeter = width * 2 + height * 2 - 4;
        int slot = slotIndex % perimeter;
        int lastX = width - 1;
        int lastY = height - 1;

        if (slot < width)
            return new MazeCellIndex(slot, 0);
        slot -= width;

        if (slot < height - 1)
            return new MazeCellIndex(lastX, slot + 1);
        slot -= height - 1;

        if (slot < width - 1)
            return new MazeCellIndex(lastX - slot - 1, lastY);
        slot -= width - 1;

        return new MazeCellIndex(0, lastY - slot - 1);
    }

    static bool IsSameCell(MazeCellIndex a, MazeCellIndex b)
    {
        return a.x == b.x && a.y == b.y;
    }

    static bool ContainsCell(List<MazeCellIndex> cells, MazeCellIndex candidate)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].x == candidate.x && cells[i].y == candidate.y)
                return true;
        }

        return false;
    }

    static int ManhattanDistance(MazeCellIndex a, MazeCellIndex b)
    {
        return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
    }

    static void AddUnique(List<MazeCellIndex> cells, MazeCellIndex candidate)
    {
        if (!ContainsCell(cells, candidate))
            cells.Add(candidate);
    }
}
