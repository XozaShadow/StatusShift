namespace StatusShift;

internal static class ApplyModeNames
{
    public static readonly string[] Labels = ["Notifications", "Auto", "Off", "Selector"];

    public static string Label(ApplyMode mode) => mode switch
    {
        ApplyMode.Auto => "Auto",
        ApplyMode.Off => "Off",
        ApplyMode.Selector => "Selector",
        _ => "Notifications",
    };
}
