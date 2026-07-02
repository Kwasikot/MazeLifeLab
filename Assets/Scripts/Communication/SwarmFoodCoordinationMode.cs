public enum SwarmFoodCoordinationMode
{
    None = 0,
    HiveBulletin = 1,
    LocalBroadcast = 2,
    GlobalBroadcast = 3
}

public static class SwarmFoodCoordinationModeExtensions
{
    public static bool UsesCoordination(this SwarmFoodCoordinationMode mode)
    {
        return mode != SwarmFoodCoordinationMode.None;
    }

    public static string ToReportLabel(this SwarmFoodCoordinationMode mode)
    {
        return mode switch
        {
            SwarmFoodCoordinationMode.HiveBulletin => "HiveBulletin",
            SwarmFoodCoordinationMode.LocalBroadcast => "LocalBroadcast",
            SwarmFoodCoordinationMode.GlobalBroadcast => "GlobalBroadcast",
            _ => "None"
        };
    }
}
