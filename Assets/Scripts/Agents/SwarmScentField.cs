using UnityEngine;

/// <summary>
/// Decaying XZ scent grid for swarm foraging trails (EXP-SWARM-005).
/// </summary>
public sealed class SwarmScentField
{
    float[] _strength;
    int _resolution;
    Vector3 _origin;
    Vector3 _arenaSize;
    float _decayPerStep;
    float _maxStrength;
    int _totalDeposits;

    public int Resolution => _resolution;
    public int TotalDeposits => _totalDeposits;

    public SwarmScentField(float decayPerStep = 0.985f, float maxStrength = 12f)
    {
        _decayPerStep = Mathf.Clamp(decayPerStep, 0.8f, 1f);
        _maxStrength = Mathf.Max(0.5f, maxStrength);
    }

    public void Reset(Vector3 arenaCenter, Vector3 arenaSize, int resolution)
    {
        _resolution = Mathf.Clamp(resolution, 4, 128);
        _arenaSize = arenaSize;
        _origin = arenaCenter - arenaSize * 0.5f;
        _strength = new float[_resolution * _resolution];
        _totalDeposits = 0;
    }

    public void DecayStep()
    {
        if (_strength == null)
            return;

        for (int i = 0; i < _strength.Length; i++)
        {
            float value = _strength[i] * _decayPerStep;
            _strength[i] = value < 0.001f ? 0f : value;
        }
    }

    public void DepositAt(Vector3 worldPosition, float amount)
    {
        if (_strength == null || amount <= 0f)
            return;

        int index = IndexAt(worldPosition);
        if (index < 0)
            return;

        float next = Mathf.Min(_maxStrength, _strength[index] + amount);
        if (next > _strength[index] + 0.001f)
            _totalDeposits++;

        _strength[index] = next;
    }

    public float SampleStrength(Vector3 worldPosition)
    {
        int index = IndexAt(worldPosition);
        if (index < 0 || _strength == null)
            return 0f;

        return _strength[index];
    }

    public bool TrySampleGradientDirection(Vector3 worldPosition, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (_strength == null)
            return false;

        if (!TryCellCoords(worldPosition, out int cx, out int cz))
            return false;

        float center = _strength[cx + cz * _resolution];
        float left = cx > 0 ? _strength[cx - 1 + cz * _resolution] : center;
        float right = cx < _resolution - 1 ? _strength[cx + 1 + cz * _resolution] : center;
        float down = cz > 0 ? _strength[cx + (cz - 1) * _resolution] : center;
        float up = cz < _resolution - 1 ? _strength[cx + (cz + 1) * _resolution] : center;

        direction = new Vector3(right - left, 0f, up - down);
        if (direction.sqrMagnitude < 0.0001f)
        {
            if (center <= 0.001f)
                return false;

            direction = Vector3.forward;
            return true;
        }

        return true;
    }

    int IndexAt(Vector3 worldPosition)
    {
        if (!TryCellCoords(worldPosition, out int cx, out int cz))
            return -1;

        return cx + cz * _resolution;
    }

    bool TryCellCoords(Vector3 worldPosition, out int cellX, out int cellZ)
    {
        cellX = 0;
        cellZ = 0;
        if (_arenaSize.x <= 0.001f || _arenaSize.z <= 0.001f)
            return false;

        Vector3 local = worldPosition - _origin;
        cellX = Mathf.Clamp(Mathf.FloorToInt(local.x / _arenaSize.x * _resolution), 0, _resolution - 1);
        cellZ = Mathf.Clamp(Mathf.FloorToInt(local.z / _arenaSize.z * _resolution), 0, _resolution - 1);
        return true;
    }
}
