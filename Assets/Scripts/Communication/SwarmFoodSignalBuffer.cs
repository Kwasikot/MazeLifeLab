using System.Collections.Generic;

/// <summary>
/// Bounded store of food coordination signals (max one slot per food site, plus overflow ring).
/// </summary>
public sealed class SwarmFoodSignalBuffer
{
    const int MaxSignals = 8;

    readonly SwarmFoodSignal[] _signals = new SwarmFoodSignal[MaxSignals];
    int _count;

    public int Count => _count;

    public void Clear()
    {
        _count = 0;
    }

    public void Upsert(in SwarmFoodSignal signal)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_signals[i].FoodSiteIndex != signal.FoodSiteIndex)
                continue;

            _signals[i] = signal;
            return;
        }

        if (_count < MaxSignals)
        {
            _signals[_count++] = signal;
            return;
        }

        int oldestIndex = 0;
        int oldestStep = _signals[0].StepCreated;
        for (int i = 1; i < MaxSignals; i++)
        {
            if (_signals[i].StepCreated >= oldestStep)
                continue;

            oldestStep = _signals[i].StepCreated;
            oldestIndex = i;
        }

        _signals[oldestIndex] = signal;
    }

    public int CopyTo(List<SwarmFoodSignal> destination)
    {
        destination.Clear();
        for (int i = 0; i < _count; i++)
            destination.Add(_signals[i]);

        return destination.Count;
    }
}
