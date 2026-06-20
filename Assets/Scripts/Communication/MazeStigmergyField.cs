using UnityEngine;

/// <summary>
/// Per-cell environmental signal field with decay. Shared physics, not shared agent memory.
/// </summary>
public sealed class MazeStigmergyField
{
    readonly float _decayPerStep;
    readonly float _maxCellStrength;
    float[,] _strength;
    int[,] _lastDepositor;
    int _width;
    int _height;
    int _totalDeposits;

    public int TotalDeposits => _totalDeposits;
    public int Width => _width;
    public int Height => _height;

    public bool TryGetCellSignal(int cellX, int cellY, out float strength, out int depositor)
    {
        strength = 0f;
        depositor = -1;
        if (_strength == null || !IsInBounds(cellX, cellY))
            return false;

        strength = _strength[cellX, cellY];
        if (strength <= 0.001f)
            return false;

        depositor = _lastDepositor[cellX, cellY];
        return true;
    }

    public MazeStigmergyField(float decayPerStep = 0.992f, float maxCellStrength = 8f)
    {
        _decayPerStep = Mathf.Clamp(decayPerStep, 0.8f, 1f);
        _maxCellStrength = Mathf.Max(0.1f, maxCellStrength);
    }

    public void Reset(int widthCells, int heightCells)
    {
        _width = Mathf.Max(1, widthCells);
        _height = Mathf.Max(1, heightCells);
        _strength = new float[_width, _height];
        _lastDepositor = new int[_width, _height];
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
                _lastDepositor[x, y] = -1;
        }

        _totalDeposits = 0;
    }

    public void DecayStep()
    {
        if (_strength == null)
            return;

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                float value = _strength[x, y] * _decayPerStep;
                if (value < 0.001f)
                {
                    _strength[x, y] = 0f;
                    _lastDepositor[x, y] = -1;
                }
                else
                {
                    _strength[x, y] = value;
                }
            }
        }
    }

    public void Deposit(int cellX, int cellY, float amount, int agentIndex)
    {
        if (_strength == null || amount <= 0f || !IsInBounds(cellX, cellY))
            return;

        _strength[cellX, cellY] = Mathf.Min(_maxCellStrength, _strength[cellX, cellY] + amount);
        _lastDepositor[cellX, cellY] = agentIndex;
        _totalDeposits++;
    }

    public float Read(int cellX, int cellY)
    {
        if (_strength == null || !IsInBounds(cellX, cellY))
            return 0f;

        return _strength[cellX, cellY];
    }

    public bool TryFindBiasStep(
        int cellX,
        int cellY,
        int readerAgentIndex,
        bool ignoreOwnSignals,
        MazeGenerator truth,
        out int dirX,
        out int dirZ)
    {
        dirX = 0;
        dirZ = 0;

        if (_strength == null || truth == null)
            return false;

        float bestStrength = 0f;
        int bestDirX = 0;
        int bestDirZ = 0;
        bool found = false;

        int[][] directions =
        {
            new[] { 0, 1 },
            new[] { 1, 0 },
            new[] { 0, -1 },
            new[] { -1, 0 }
        };

        for (int i = 0; i < directions.Length; i++)
        {
            int dx = directions[i][0];
            int dz = directions[i][1];
            if (!truth.IsPassageOpen(cellX, cellY, dx, dz))
                continue;

            int nx = cellX + dx;
            int ny = cellY + dz;
            if (!IsInBounds(nx, ny))
                continue;

            if (ignoreOwnSignals && _lastDepositor[nx, ny] == readerAgentIndex)
                continue;

            float signal = _strength[nx, ny];
            if (signal <= bestStrength)
                continue;

            bestStrength = signal;
            bestDirX = dx;
            bestDirZ = dz;
            found = true;
        }

        if (!found || bestStrength <= 0f)
            return false;

        dirX = bestDirX;
        dirZ = bestDirZ;
        return true;
    }

    bool IsInBounds(int cellX, int cellY)
    {
        return cellX >= 0 && cellY >= 0 && cellX < _width && cellY < _height;
    }
}
