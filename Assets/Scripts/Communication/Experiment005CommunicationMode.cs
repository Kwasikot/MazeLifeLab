public enum Experiment005CommunicationMode
{
    None = 0,
    RandomNoise = 1,
    Trail = 2,
    FrontierHint = 3
}

public static class Experiment005CommunicationModeExtensions
{
    public static bool DepositsSignals(this Experiment005CommunicationMode mode)
    {
        return mode != Experiment005CommunicationMode.None;
    }

    public static bool EnablesSignalRead(this Experiment005CommunicationMode mode)
    {
        return mode == Experiment005CommunicationMode.FrontierHint ||
               mode == Experiment005CommunicationMode.Trail;
    }

    public static bool IgnoreOwnSignalsWhenReading(this Experiment005CommunicationMode mode)
    {
        return mode == Experiment005CommunicationMode.Trail;
    }
}
