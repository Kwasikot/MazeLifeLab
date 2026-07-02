using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EXP-SWARM-005b explicit food signals: hive bulletin, local broadcast, global oracle.
/// </summary>
public sealed class SwarmFoodCoordinationSystem
{
    readonly SwarmFoodSignalBuffer _buffer = new SwarmFoodSignalBuffer();
    readonly List<SwarmFoodSignal> _queryBuffer = new List<SwarmFoodSignal>();

    SwarmFoodCoordinationMode _mode;
    float _broadcastRadius;
    int _signalTtlSteps;
    int _signalsEmitted;
    int _signalsReceived;
    int _staleSignalRejects;

    public int SignalsEmitted => _signalsEmitted;
    public int SignalsReceived => _signalsReceived;
    public int StaleSignalRejects => _staleSignalRejects;

    public void Configure(SwarmFoodCoordinationMode mode, float broadcastRadius, int signalTtlSteps)
    {
        _mode = mode;
        _broadcastRadius = Mathf.Max(0.1f, broadcastRadius);
        _signalTtlSteps = Mathf.Max(1, signalTtlSteps);
    }

    public void Reset()
    {
        _buffer.Clear();
        _signalsEmitted = 0;
        _signalsReceived = 0;
        _staleSignalRejects = 0;
    }

    public void Emit(
        in SwarmFoodSignal signal,
        IReadOnlyList<Vector3> agentPositions)
    {
        if (_mode == SwarmFoodCoordinationMode.None)
            return;

        _buffer.Upsert(signal);
        _signalsEmitted++;

        if (agentPositions == null)
            return;

        if (_mode == SwarmFoodCoordinationMode.GlobalBroadcast)
        {
            _signalsReceived += agentPositions.Count;
            return;
        }

        if (_mode != SwarmFoodCoordinationMode.LocalBroadcast)
            return;

        float radiusSq = _broadcastRadius * _broadcastRadius;
        Vector3 emitter = signal.SenderPosition;
        for (int i = 0; i < agentPositions.Count; i++)
        {
            Vector3 delta = agentPositions[i] - emitter;
            delta.y = 0f;
            if (delta.sqrMagnitude <= radiusSq)
                _signalsReceived++;
        }
    }

    public bool TrySelectBestSignal(
        int currentStep,
        Vector3 agentPosition,
        Func<int, bool> isFoodSiteAvailable,
        out SwarmFoodSignal best,
        out Vector3 direction)
    {
        best = default;
        direction = Vector3.zero;

        if (_mode == SwarmFoodCoordinationMode.None)
            return false;

        _buffer.CopyTo(_queryBuffer);
        if (_queryBuffer.Count == 0)
            return false;

        float bestScore = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < _queryBuffer.Count; i++)
        {
            SwarmFoodSignal candidate = _queryBuffer[i];
            if (!PassesReadFilter(candidate, agentPosition))
            {
                _staleSignalRejects++;
                continue;
            }

            if (candidate.IsExpired(currentStep))
            {
                _staleSignalRejects++;
                continue;
            }

            if (!candidate.IsActionable())
            {
                _staleSignalRejects++;
                continue;
            }

            if (isFoodSiteAvailable != null && !isFoodSiteAvailable(candidate.FoodSiteIndex))
            {
                _staleSignalRejects++;
                continue;
            }

            float freshness = 1f / (1f + (currentStep - candidate.StepCreated));
            float kindBoost = candidate.Kind == SwarmFoodSignalKind.PickedUp ? 1.35f : 1f;
            float distance = HorizontalDistance(agentPosition, candidate.Position);
            float score = kindBoost * freshness * 100f - distance;

            if (score <= bestScore)
                continue;

            bestScore = score;
            best = candidate;
            found = true;
        }

        if (!found)
            return false;

        direction = best.Position - agentPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();
        return true;
    }

    bool PassesReadFilter(in SwarmFoodSignal signal, Vector3 agentPosition)
    {
        switch (_mode)
        {
            case SwarmFoodCoordinationMode.HiveBulletin:
            case SwarmFoodCoordinationMode.GlobalBroadcast:
                return true;
            case SwarmFoodCoordinationMode.LocalBroadcast:
            {
                float toSender = HorizontalDistance(agentPosition, signal.SenderPosition);
                float toFood = HorizontalDistance(agentPosition, signal.Position);
                return toSender <= _broadcastRadius || toFood <= _broadcastRadius;
            }
            default:
                return false;
        }
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
