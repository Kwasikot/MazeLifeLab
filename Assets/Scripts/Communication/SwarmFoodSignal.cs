using UnityEngine;

public enum SwarmFoodSignalKind
{
    Discovered = 0,
    PickedUp = 1,
    Depleted = 2
}

public struct SwarmFoodSignal
{
    public int FoodSiteIndex;
    public Vector3 Position;
    public Vector3 SenderPosition;
    public int StepCreated;
    public int StepExpires;
    public SwarmFoodSignalKind Kind;
    public int SenderAgentIndex;

    public bool IsExpired(int currentStep)
    {
        return currentStep > StepExpires;
    }

    public bool IsActionable()
    {
        return Kind != SwarmFoodSignalKind.Depleted;
    }
}
