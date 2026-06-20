using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deposits environmental signals based on EXP-005 communication mode.
/// </summary>
public class AgentStigmergyController : MonoBehaviour
{
    [SerializeField] float trailDepositStrength = 1f;
    [SerializeField] float frontierDepositStrength = 1.5f;
    [SerializeField] float randomNoiseDepositStrength = 0.75f;
    [SerializeField] float randomNoiseChance = 0.15f;

    readonly HashSet<long> _trailCells = new HashSet<long>();

    MazeStigmergyField _field;
    MazeGenerator _truth;
    Experiment005CommunicationMode _mode;
    System.Random _rng;
    int _agentIndex;
    int _deposits;
    float _depositNeighborSpread;
    int _depositSpreadRadiusCells = 1;

    public int AgentIndex => _agentIndex;
    public int Deposits => _deposits;
    public int TrailCellCount => _trailCells.Count;

    public void Configure(float depositNeighborSpread)
    {
        Configure(depositNeighborSpread, 1);
    }

    public void Configure(float depositNeighborSpread, int depositSpreadRadiusCells)
    {
        _depositNeighborSpread = Mathf.Clamp01(depositNeighborSpread);
        _depositSpreadRadiusCells = Mathf.Max(1, depositSpreadRadiusCells);
    }

    public void BeginEpisode(
        MazeStigmergyField field,
        Experiment005CommunicationMode mode,
        int agentIndex,
        int mazeSeed,
        MazeGenerator truth)
    {
        _field = field;
        _mode = mode;
        _agentIndex = agentIndex;
        _truth = truth;
        _rng = new System.Random(mazeSeed + 902501 + agentIndex * 3571);
        _deposits = 0;
        _trailCells.Clear();
    }

    public void EndEpisode()
    {
        _field = null;
        _truth = null;
        _rng = null;
        _deposits = 0;
        _trailCells.Clear();
    }

    public void CopyTrailCells(List<Vector2Int> buffer)
    {
        buffer.Clear();
        foreach (long key in _trailCells)
            buffer.Add(new Vector2Int((int)(key >> 32), (int)(key & 0xFFFFFFFF)));
    }

    public void NotifyAtCell(int cellX, int cellY, bool isFrontierCell = false)
    {
        if (_field == null || _truth == null || _mode == Experiment005CommunicationMode.None)
            return;

        switch (_mode)
        {
            case Experiment005CommunicationMode.Trail:
                Deposit(cellX, cellY, trailDepositStrength);
                break;
            case Experiment005CommunicationMode.FrontierClaim:
                if (_trailCells.Add(CellKey(cellX, cellY)))
                    Deposit(cellX, cellY, trailDepositStrength);
                break;
            case Experiment005CommunicationMode.FrontierHint:
                if (isFrontierCell)
                    Deposit(cellX, cellY, frontierDepositStrength);
                break;
            case Experiment005CommunicationMode.RandomNoise:
                if (_rng.NextDouble() < randomNoiseChance)
                {
                    int[][] directions =
                    {
                        new[] { 0, 1 },
                        new[] { 1, 0 },
                        new[] { 0, -1 },
                        new[] { -1, 0 }
                    };
                    int[] pick = directions[_rng.Next(directions.Length)];
                    int nx = cellX + pick[0];
                    int ny = cellY + pick[1];
                    if (_truth.IsCellInBounds(nx, ny))
                        Deposit(nx, ny, randomNoiseDepositStrength);
                    else
                        Deposit(cellX, cellY, randomNoiseDepositStrength);
                }
                break;
        }
    }

    void Deposit(int cellX, int cellY, float amount)
    {
        _field.Deposit(cellX, cellY, amount, _agentIndex);
        _trailCells.Add(CellKey(cellX, cellY));
        _deposits++;

        if (_depositNeighborSpread <= 0f || _truth == null)
            return;

        DiffuseDeposit(cellX, cellY, amount);
    }

    void DiffuseDeposit(int cellX, int cellY, float amount)
    {
        var queue = new Queue<(int x, int y, int depth)>();
        var visited = new HashSet<long>();
        long originKey = CellKey(cellX, cellY);
        queue.Enqueue((cellX, cellY, 0));
        visited.Add(originKey);

        int[][] directions =
        {
            new[] { 0, 1 },
            new[] { 1, 0 },
            new[] { 0, -1 },
            new[] { -1, 0 }
        };

        while (queue.Count > 0)
        {
            (int x, int y, int depth) = queue.Dequeue();
            if (depth >= _depositSpreadRadiusCells)
                continue;

            for (int i = 0; i < directions.Length; i++)
            {
                int dirX = directions[i][0];
                int dirZ = directions[i][1];
                if (!_truth.IsPassageOpen(x, y, dirX, dirZ))
                    continue;

                int nx = x + dirX;
                int ny = y + dirZ;
                if (!_truth.IsCellInBounds(nx, ny))
                    continue;

                long key = CellKey(nx, ny);
                if (!visited.Add(key))
                    continue;

                int nextDepth = depth + 1;
                float falloff = 1f - (float)nextDepth / (_depositSpreadRadiusCells + 1);
                float spreadAmount = amount * _depositNeighborSpread * falloff;
                if (spreadAmount > 0.001f)
                    _field.Deposit(nx, ny, spreadAmount, _agentIndex);

                queue.Enqueue((nx, ny, nextDepth));
            }
        }
    }

    static long CellKey(int cellX, int cellY)
    {
        return ((long)cellX << 32) | (uint)cellY;
    }
}
