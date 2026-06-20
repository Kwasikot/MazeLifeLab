using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct MazeWall
{
    public Vector3 A;
    public Vector3 B;
    public bool bVisible;
    public int cell_id1;
    public int cell_id2;
}

public struct MazeCell
{
    public int id;
    public List<string> walls;
    public bool bVisited;
    public Vector3 A, B, C, D;
}

public class MazeGenerator
{
    static readonly int[] CarveDx = { 1, -1, 0, 0 };
    static readonly int[] CarveDy = { 0, 0, 1, -1 };

    readonly MazeSeedConfig _config;
    readonly Dictionary<string, MazeWall> _wallsMap = new Dictionary<string, MazeWall>();
    MazeCell[,] _cells;

    public MazeSeedConfig Config => _config;
    public int MazeSeed => _config.mazeSeed;
    public IReadOnlyDictionary<string, MazeWall> WallsMap => _wallsMap;
    public int VisibleWallCount { get; private set; }
    public int VisitedCellCount { get; private set; }
    public int Fingerprint { get; private set; }

    public bool IsCellInBounds(int cellX, int cellY)
    {
        return cellX >= 0 && cellX < _config.mazeWidthCells &&
               cellY >= 0 && cellY < _config.mazeHeightCells;
    }

    public Vector3 GetCellCenterWorld(int cellX, int cellY)
    {
        float half = _config.cellSize * 0.5f;
        return new Vector3(
            cellX * _config.cellSize + half,
            0f,
            cellY * _config.cellSize + half);
    }

    public bool TryWorldToCell(Vector3 world, out int cellX, out int cellY)
    {
        cellX = Mathf.FloorToInt(world.x / _config.cellSize);
        cellY = Mathf.FloorToInt(world.z / _config.cellSize);
        return IsCellInBounds(cellX, cellY);
    }

    /// <summary>
    /// True when a visible wall blocks passage from (cellX, cellY) in direction (dirX, dirZ).
    /// dir is one of (0,1), (1,0), (0,-1), (-1,0) on the XZ grid.
    /// </summary>
    public bool HasVisibleWallBetween(int cellX, int cellY, int dirX, int dirZ)
    {
        if (!IsCellInBounds(cellX, cellY))
            return true;

        int neighborX = cellX + dirX;
        int neighborY = cellY + dirZ;
        if (!IsCellInBounds(neighborX, neighborY))
            return true;

        return IsSharedWallVisible(cellX, cellY, neighborX, neighborY);
    }

    bool IsSharedWallVisible(int cellX, int cellY, int neighborX, int neighborY)
    {
        MazeCell cell = _cells[cellX, cellY];
        Vector3 from;
        Vector3 to;

        if (neighborX == cellX + 1 && neighborY == cellY)
        {
            from = cell.B;
            to = cell.D;
        }
        else if (neighborX == cellX - 1 && neighborY == cellY)
        {
            from = cell.C;
            to = cell.A;
        }
        else if (neighborY == cellY + 1 && neighborX == cellX)
        {
            from = cell.D;
            to = cell.C;
        }
        else if (neighborY == cellY - 1 && neighborX == cellX)
        {
            from = cell.A;
            to = cell.B;
        }
        else
        {
            return true;
        }

        string key = ValidKey(from, to);
        if (key == "")
            return true;

        return _wallsMap[key].bVisible;
    }

    /// <summary>
    /// 2D raycast against visible wall center lines on the XZ plane.
    /// </summary>
    public bool TryRaycastVisibleWall(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        out float hitDistance)
    {
        hitDistance = maxDistance;
        direction.y = 0f;
        if (direction.sqrMagnitude < 1e-8f)
            return false;

        direction.Normalize();
        Vector2 rayOrigin = new Vector2(origin.x, origin.z);
        Vector2 rayDir = new Vector2(direction.x, direction.z);

        float closest = maxDistance;
        bool found = false;

        foreach (KeyValuePair<string, MazeWall> entry in _wallsMap)
        {
            if (!entry.Value.bVisible)
                continue;

            if (RayIntersectsSegment2D(
                    rayOrigin,
                    rayDir,
                    maxDistance,
                    entry.Value.A,
                    entry.Value.B,
                    out float distance) &&
                distance < closest)
            {
                closest = distance;
                found = true;
            }
        }

        hitDistance = closest;
        return found;
    }

    static bool RayIntersectsSegment2D(
        Vector2 rayOrigin,
        Vector2 rayDir,
        float maxDistance,
        Vector3 segA,
        Vector3 segB,
        out float hitDistance)
    {
        hitDistance = maxDistance;
        Vector2 a = new Vector2(segA.x, segA.z);
        Vector2 b = new Vector2(segB.x, segB.z);
        Vector2 seg = b - a;

        float cross = rayDir.x * seg.y - rayDir.y * seg.x;
        const float epsilon = 1e-6f;
        if (Mathf.Abs(cross) < epsilon)
            return false;

        Vector2 diff = a - rayOrigin;
        float t = (diff.x * seg.y - diff.y * seg.x) / cross;
        float u = (diff.x * rayDir.y - diff.y * rayDir.x) / cross;

        if (t < 0f || t > maxDistance || u < 0f || u > 1f)
            return false;

        hitDistance = t;
        return true;
    }

    public MazeGenerator(MazeSeedConfig config)
    {
        _config = config;
    }

    public void Generate()
    {
        _wallsMap.Clear();
        _cells = new MazeCell[_config.mazeWidthCells, _config.mazeHeightCells];
        CreateCells();
        CarveMaze(new System.Random(_config.mazeSeed));
        VisibleWallCount = _wallsMap.Values.Count(w => w.bVisible);
        Fingerprint = ComputeFingerprint();

        int totalCells = _config.mazeWidthCells * _config.mazeHeightCells;
        if (VisitedCellCount < totalCells)
        {
            Debug.LogWarning(
                $"[MazeGenerator] Carving INCOMPLETE: {VisitedCellCount}/{totalCells} cells " +
                $"(seed={_config.mazeSeed}, size={_config.mazeWidthCells}x{_config.mazeHeightCells}). " +
                "Maze will only fill a small patch until this is fixed.");
        }
        else
        {
            Debug.Log(
                $"[MazeGenerator] Carving OK: {VisitedCellCount}/{totalCells} cells " +
                $"(seed={_config.mazeSeed}, visibleWalls={VisibleWallCount}).");
        }
    }

    static string WallKey(Vector3 a, Vector3 b)
    {
        int ax = Mathf.RoundToInt(a.x);
        int az = Mathf.RoundToInt(a.z);
        int bx = Mathf.RoundToInt(b.x);
        int bz = Mathf.RoundToInt(b.z);

        // Canonical order so (a,b) and (b,a) share one key.
        if (ax > bx || (ax == bx && az > bz))
        {
            int tx = ax;
            int tz = az;
            ax = bx;
            az = bz;
            bx = tx;
            bz = tz;
        }

        // Delimiters prevent ambiguous keys on large mazes (e.g. 0,5,0,10 vs 0,50,10,0).
        return ax + "|" + az + "|" + bx + "|" + bz;
    }

    public bool IsPassageOpen(int cellX, int cellY, int dirX, int dirZ)
    {
        return !HasVisibleWallBetween(cellX, cellY, dirX, dirZ);
    }

    public int CountOpenPassages(int cellX, int cellY)
    {
        int count = 0;
        if (IsPassageOpen(cellX, cellY, 0, 1)) count++;
        if (IsPassageOpen(cellX, cellY, 1, 0)) count++;
        if (IsPassageOpen(cellX, cellY, 0, -1)) count++;
        if (IsPassageOpen(cellX, cellY, -1, 0)) count++;
        return count;
    }

    public string DescribeOpenPassages(int cellX, int cellY)
    {
        var names = new System.Collections.Generic.List<string>();
        if (IsPassageOpen(cellX, cellY, 0, 1)) names.Add("N");
        if (IsPassageOpen(cellX, cellY, 1, 0)) names.Add("E");
        if (IsPassageOpen(cellX, cellY, 0, -1)) names.Add("S");
        if (IsPassageOpen(cellX, cellY, -1, 0)) names.Add("W");
        return names.Count == 0 ? "none" : string.Join(",", names);
    }

    void CreateCells()
    {
        int x = 0;
        for (int j = 0; j < _config.mazeHeightCells; j++)
        {
            x = 0;
            for (int i = 0; i < _config.mazeWidthCells; i++)
            {
                int currentId = j * _config.mazeWidthCells + i;
                int y = j * _config.cellSize;

                _cells[i, j].A = new Vector3(x, 0, y);
                _cells[i, j].B = new Vector3(x + _config.cellSize, 0, y);
                _cells[i, j].C = new Vector3(x, 0, y + _config.cellSize);
                _cells[i, j].D = new Vector3(x + _config.cellSize, 0, y + _config.cellSize);
                _cells[i, j].id = currentId;
                CreateWalls(currentId, i, j, _cells[i, j].A, _cells[i, j].B, _cells[i, j].C, _cells[i, j].D);
                x += _config.cellSize;
            }
        }
    }

    void CreateWalls(int currentId, int i, int j, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        _cells[i, j].walls = new List<string>();

        AddOrShareWall(currentId, i, j, a, b,
            j > 0 ? currentId - _config.mazeWidthCells : currentId);
        AddOrShareWall(currentId, i, j, b, d,
            (i + 1) < _config.mazeWidthCells ? currentId + 1 : currentId);
        AddOrShareWall(currentId, i, j, d, c,
            (j + 1) < _config.mazeHeightCells ? currentId + _config.mazeWidthCells : currentId);
        AddOrShareWall(currentId, i, j, c, a,
            i > 0 ? currentId - 1 : currentId);
    }

    void AddOrShareWall(int currentId, int i, int j, Vector3 a, Vector3 b, int neighborId)
    {
        string key = WallKey(a, b);

        if (_wallsMap.ContainsKey(key))
        {
            _cells[i, j].walls.Add(key);
            return;
        }

        var wall = new MazeWall
        {
            A = a,
            B = b,
            bVisible = true,
            cell_id1 = currentId,
            cell_id2 = neighborId
        };
        _wallsMap[key] = wall;
        _cells[i, j].walls.Add(key);
    }

    string ValidKey(Vector3 a, Vector3 b)
    {
        string key = WallKey(a, b);
        return _wallsMap.ContainsKey(key) ? key : "";
    }

    string FindWallKeyBetween(int cellX, int cellY, int neighborX, int neighborY)
    {
        int neighborId = neighborY * _config.mazeWidthCells + neighborX;
        List<string> walls = _cells[cellX, cellY].walls;

        for (int i = 0; i < walls.Count; i++)
        {
            if (!_wallsMap.TryGetValue(walls[i], out MazeWall wall))
                continue;

            if (wall.cell_id1 == neighborId || wall.cell_id2 == neighborId)
                return walls[i];
        }

        return null;
    }

    void CarveMaze(System.Random rng)
    {
        int width = _config.mazeWidthCells;
        int height = _config.mazeHeightCells;
        int totalCells = width * height;

        int startI = rng.Next(0, width);
        int startJ = rng.Next(0, height);
        _cells[startI, startJ].bVisited = true;
        VisitedCellCount = 1;

        var stack = new Stack<(int x, int y)>();
        stack.Push((startI, startJ));

        var candidates = new List<(int x, int y, string key)>(4);

        while (stack.Count > 0)
        {
            (int cx, int cy) = stack.Peek();
            candidates.Clear();

            for (int d = 0; d < 4; d++)
            {
                int nbrX = cx + CarveDx[d];
                int nbrY = cy + CarveDy[d];
                if (nbrX < 0 || nbrX >= width || nbrY < 0 || nbrY >= height)
                    continue;
                if (_cells[nbrX, nbrY].bVisited)
                    continue;

                string wallKey = FindWallKeyBetween(cx, cy, nbrX, nbrY);
                if (wallKey == null)
                    continue;

                candidates.Add((nbrX, nbrY, wallKey));
            }

            if (candidates.Count == 0)
            {
                stack.Pop();
                continue;
            }

            var chosen = candidates[rng.Next(candidates.Count)];

            if (_wallsMap.TryGetValue(chosen.key, out MazeWall wall))
            {
                wall.bVisible = false;
                _wallsMap[chosen.key] = wall;
            }

            _cells[chosen.x, chosen.y].bVisited = true;
            VisitedCellCount++;
            stack.Push((chosen.x, chosen.y));
        }

        if (VisitedCellCount < totalCells)
        {
            Debug.LogWarning(
                $"[MazeGenerator] DFS carving stopped early: {VisitedCellCount}/{totalCells} cells.");
        }
    }

    int ComputeFingerprint()
    {
        int hash = 17;
        hash = hash * 31 + _config.mazeSeed;
        hash = hash * 31 + VisibleWallCount;

        foreach (KeyValuePair<string, MazeWall> kv in _wallsMap.OrderBy(entry => entry.Key))
        {
            if (!kv.Value.bVisible)
                continue;

            hash = hash * 31 + kv.Key.GetHashCode();
        }

        return hash;
    }
}
