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
    readonly MazeSeedConfig _config;
    readonly Dictionary<string, MazeWall> _wallsMap = new Dictionary<string, MazeWall>();
    MazeCell[,] _cells;

    public MazeSeedConfig Config => _config;
    public int MazeSeed => _config.mazeSeed;
    public IReadOnlyDictionary<string, MazeWall> WallsMap => _wallsMap;
    public int VisibleWallCount { get; private set; }
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
    }

    static string WallKey(Vector3 a, Vector3 b)
    {
        return $"{a.x}{a.z}{b.x}{b.z}";
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
            (currentId - _config.mazeWidthCells) > 0 ? currentId - _config.mazeWidthCells : currentId);
        AddOrShareWall(currentId, i, j, b, d,
            (i + 1) < _config.mazeWidthCells ? currentId + 1 : currentId);
        AddOrShareWall(currentId, i, j, d, c,
            (j + 1) < _config.mazeHeightCells ? currentId + _config.mazeWidthCells : currentId);
        AddOrShareWall(currentId, i, j, c, a,
            (i - 1) > 0 ? currentId - 1 : currentId);
    }

    void AddOrShareWall(int currentId, int i, int j, Vector3 a, Vector3 b, int neighborId)
    {
        string key = WallKey(a, b);
        string reverseKey = WallKey(b, a);

        if (_wallsMap.ContainsKey(key) || _wallsMap.ContainsKey(reverseKey))
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

    MazeCell GetCell(int id)
    {
        int i = id % _config.mazeWidthCells;
        int j = id / _config.mazeWidthCells;
        return _cells[i, j];
    }

    string ValidKey(Vector3 a, Vector3 b)
    {
        string key = WallKey(a, b);
        if (_wallsMap.ContainsKey(key))
            return key;

        string reverseKey = WallKey(b, a);
        if (_wallsMap.ContainsKey(reverseKey))
            return reverseKey;

        return "";
    }

    void AddWalls(MazeCell cell, List<string> list)
    {
        TryAddWall(list, cell.A, new Vector3(cell.A.x, 0, cell.A.z - _config.cellSize));
        TryAddWall(list, cell.A, new Vector3(cell.A.x - _config.cellSize, 0, cell.A.z));

        TryAddWall(list, cell.B, new Vector3(cell.B.x, 0, cell.B.z - _config.cellSize));
        TryAddWall(list, cell.B, new Vector3(cell.B.x + _config.cellSize, 0, cell.B.z));

        TryAddWall(list, cell.D, new Vector3(cell.D.x, 0, cell.D.z + _config.cellSize));
        TryAddWall(list, cell.D, new Vector3(cell.D.x + _config.cellSize, 0, cell.D.z));

        TryAddWall(list, cell.C, new Vector3(cell.C.x - _config.cellSize, 0, cell.C.z));
        TryAddWall(list, cell.C, new Vector3(cell.C.x, 0, cell.C.z + _config.cellSize));
    }

    void TryAddWall(List<string> list, Vector3 from, Vector3 to)
    {
        string key = ValidKey(from, to);
        if (key != "")
            list.Add(key);
    }

    void CarveMaze(System.Random rng)
    {
        int startI = rng.Next(0, _config.mazeWidthCells);
        int startJ = rng.Next(0, _config.mazeHeightCells);
        _cells[startI, startJ].bVisited = true;

        var frontier = new List<string>(_cells[startI, startJ].walls);
        int maxIterations = 10000;

        while (frontier.Count > 0 && maxIterations > 0)
        {
            int k = rng.Next(0, frontier.Count);
            string wallKey = frontier[k];
            frontier.RemoveAt(k);

            if (!_wallsMap.TryGetValue(wallKey, out MazeWall wall))
                continue;

            MazeCell cell1 = GetCell(wall.cell_id1);
            MazeCell cell2 = GetCell(wall.cell_id2);

            if (cell1.bVisited ^ cell2.bVisited)
            {
                wall.bVisible = false;
                _wallsMap[wallKey] = wall;

                int currentId = cell1.bVisited ? cell2.id : cell1.id;
                int ii = currentId % _config.mazeWidthCells;
                int jj = currentId / _config.mazeWidthCells;
                _cells[ii, jj].bVisited = true;
                AddWalls(_cells[ii, jj], frontier);
            }

            maxIterations--;
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
