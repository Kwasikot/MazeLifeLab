using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Environmental responsibility map for EXP-005 frontier coordination.
/// Agents publish only their chosen target frontier, not their internal map.
/// </summary>
public sealed class MazeFrontierClaimField
{
    readonly Dictionary<int, FrontierClaimRecord> _claims = new Dictionary<int, FrontierClaimRecord>();
    int _width;
    int _height;
    int _totalClaimsCreated;
    int _claimConflicts;

    public int TotalClaimsCreated => _totalClaimsCreated;
    public int ClaimConflicts => _claimConflicts;
    public int ActiveClaimCount => _claims.Count;

    public void Reset(int widthCells, int heightCells)
    {
        _width = Mathf.Max(1, widthCells);
        _height = Mathf.Max(1, heightCells);
        _claims.Clear();
        _totalClaimsCreated = 0;
        _claimConflicts = 0;
    }

    public void Step()
    {
        if (_claims.Count == 0)
            return;

        var expired = new List<int>();
        var keys = new List<int>(_claims.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            int agentIndex = keys[i];
            FrontierClaimRecord claim = _claims[agentIndex];
            claim.TtlSteps--;
            if (claim.TtlSteps <= 0)
                expired.Add(agentIndex);
            else
                _claims[agentIndex] = claim;
        }

        for (int i = 0; i < expired.Count; i++)
            _claims.Remove(expired[i]);
    }

    public bool PublishClaim(int agentIndex, int cellX, int cellY, int radiusCells, int ttlSteps)
    {
        if (!IsInBounds(cellX, cellY))
            return false;

        radiusCells = Mathf.Max(1, radiusCells);
        ttlSteps = Mathf.Max(1, ttlSteps);

        if (TryGetStrongestOtherClaim(agentIndex, cellX, cellY, out _, out float conflict) && conflict > 0f)
            _claimConflicts++;

        _claims[agentIndex] = new FrontierClaimRecord
        {
            AgentIndex = agentIndex,
            X = cellX,
            Y = cellY,
            RadiusCells = radiusCells,
            TtlSteps = ttlSteps
        };
        _totalClaimsCreated++;
        return true;
    }

    public bool TryGetClaim(int agentIndex, out FrontierClaimRecord claim)
    {
        return _claims.TryGetValue(agentIndex, out claim);
    }

    public bool TryGetStrongestOtherClaim(
        int readerAgentIndex,
        int cellX,
        int cellY,
        out FrontierClaimRecord strongest,
        out float strength)
    {
        strongest = new FrontierClaimRecord();
        strength = 0f;

        foreach (FrontierClaimRecord claim in _claims.Values)
        {
            if (claim.AgentIndex == readerAgentIndex)
                continue;

            float candidate = claim.StrengthAt(cellX, cellY);
            if (candidate <= strength)
                continue;

            strongest = claim;
            strength = candidate;
        }

        return strength > 0f;
    }

    public float OtherClaimStrength(int readerAgentIndex, int cellX, int cellY)
    {
        TryGetStrongestOtherClaim(readerAgentIndex, cellX, cellY, out _, out float strength);
        return strength;
    }

    public float OwnClaimStrength(int agentIndex, int cellX, int cellY)
    {
        return _claims.TryGetValue(agentIndex, out FrontierClaimRecord claim)
            ? claim.StrengthAt(cellX, cellY)
            : 0f;
    }

    public void CopyActiveClaims(List<FrontierClaimRecord> buffer)
    {
        buffer.Clear();
        foreach (FrontierClaimRecord claim in _claims.Values)
            buffer.Add(claim);
    }

    bool IsInBounds(int cellX, int cellY)
    {
        return cellX >= 0 && cellY >= 0 && cellX < _width && cellY < _height;
    }
}

public struct FrontierClaimRecord
{
    public int AgentIndex;
    public int X;
    public int Y;
    public int RadiusCells;
    public int TtlSteps;

    public float StrengthAt(int cellX, int cellY)
    {
        int distance = Mathf.Abs(cellX - X) + Mathf.Abs(cellY - Y);
        if (distance > RadiusCells)
            return 0f;

        return 1f - (float)distance / Mathf.Max(1, RadiusCells);
    }
}
