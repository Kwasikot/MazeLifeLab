public enum Experiment006SwarmMode
{
    Independent = 0,
    DepositOnly = 1,
    SwarmRrt = 2
}

public static class Experiment006SwarmModeExtensions
{
    public static bool DepositsTreeEdges(this Experiment006SwarmMode mode)
    {
        return mode != Experiment006SwarmMode.Independent;
    }

    public static bool UsesSwarmGraft(this Experiment006SwarmMode mode)
    {
        return mode == Experiment006SwarmMode.SwarmRrt;
    }
}
