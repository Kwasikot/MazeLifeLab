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
        return TrySampleBiasStep(
            cellX,
            cellY,
            readerAgentIndex,
            ignoreOwnSignals,
            truth,
            null,
            0f,
            _maxCellStrength,
            0f,
            out dirX,
            out dirZ);
    }

    /// <summary>
    /// Stochastic step toward neighboring signals. Uses temperature sampling and optional
    /// crowding penalty so saturated trails do not collapse every agent onto one corridor.
    /// </summary>
    public bool TrySampleBiasStep(
        int cellX,
        int cellY,
        int readerAgentIndex,
        bool ignoreOwnSignals,
        MazeGenerator truth,
        System.Random rng,
        float temperature,
        float crowdingPenalty,
        out int dirX,
        out int dirZ)
    {
        return TrySampleBiasStep(
            cellX,
            cellY,
            readerAgentIndex,
            ignoreOwnSignals,
            truth,
            rng,
            temperature,
            _maxCellStrength,
            crowdingPenalty,
            out dirX,
            out dirZ);
    }

    public bool TrySampleBiasStep(
        int cellX,
        int cellY,
        int readerAgentIndex,
        bool ignoreOwnSignals,
        MazeGenerator truth,
        System.Random rng,
        float temperature,
        float maxStrength,
        float crowdingPenalty,
        out int dirX,
        out int dirZ)
    {
        dirX = 0;
        dirZ = 0;

        if (_strength == null || truth == null || maxStrength <= 0f)
            return false;

        float bestStrength = 0f;
        int bestDirX = 0;
        int bestDirZ = 0;

        float totalWeight = 0f;
        int candidateCount = 0;
        int pickDirX = 0;
        int pickDirY = 0;
        float pickWeight = 0f;

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
            if (signal <= 0.001f)
                continue;

            if (signal > bestStrength)
            {
                bestStrength = signal;
                bestDirX = dx;
                bestDirZ = dz;
            }

            float normalized = Mathf.Clamp01(signal / maxStrength);
            float crowded = Mathf.Clamp01(crowdingPenalty) * normalized * normalized;
            float weight = signal * (1f - crowded);
            if (weight <= 0.001f)
                continue;

            if (temperature > 0.01f)
                weight = Mathf.Pow(weight, 1f / temperature);

            candidateCount++;
            pickDirX = dx;
            pickDirY = dz;
            pickWeight = weight;
            totalWeight += weight;
        }

        if (candidateCount == 0 || totalWeight <= 0f)
            return false;

        if (rng == null || temperature <= 0.01f)
        {
            dirX = bestDirX;
            dirZ = bestDirZ;
            return bestStrength > 0f;
        }

        double roll = rng.NextDouble() * totalWeight;
        float cumulative = 0f;

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
            if (signal <= 0.001f)
                continue;

            float normalized = Mathf.Clamp01(signal / maxStrength);
            float crowded = Mathf.Clamp01(crowdingPenalty) * normalized * normalized;
            float weight = signal * (1f - crowded);
            if (weight <= 0.001f)
                continue;

            weight = Mathf.Pow(weight, 1f / temperature);
            cumulative += weight;
            if (roll <= cumulative)
            {
                dirX = dx;
                dirZ = dz;
                return true;
            }
        }

        dirX = pickDirX;
        dirZ = pickDirY;
        return pickWeight > 0f;
    }

    bool IsInBounds(int cellX, int cellY)
    {
        return cellX >= 0 && cellY >= 0 && cellX < _width && cellY < _height;
    }
}
