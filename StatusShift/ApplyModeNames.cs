using System.Collections.Generic;

namespace StatusShift;

internal static class ApplyModeNames
{
    public static readonly string[] Labels = ["Off", "Notifications", "Selector", "Auto"];

    public static string Label(ApplyMode mode) => mode switch
    {
        ApplyMode.Auto => "Auto",
        ApplyMode.Off => "Off",
        ApplyMode.Selector => "Selector",
        _ => "Notifications",
    };

    public static string[] ComboLabels(bool showAuto, ApplyMode current) => Labels;

    public static ApplyMode FromCombo(int index, bool showAuto, ApplyMode current) => index switch
    {
        0 => ApplyMode.Off,
        1 => ApplyMode.Confirm,
        3 => ApplyMode.Auto,
        _ => ApplyMode.Selector,
    };

    public static int ToCombo(ApplyMode mode, bool showAuto) => mode switch
    {
        ApplyMode.Off => 0,
        ApplyMode.Confirm => 1,
        ApplyMode.Auto => 3,
        _ => 2,
    };
}
