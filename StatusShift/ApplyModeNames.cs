using System.Collections.Generic;

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

    public static string[] ComboLabels(bool showAuto, ApplyMode current)
    {
        var modes = Modes(showAuto, current);
        var labels = new string[modes.Count];
        for (var i = 0; i < modes.Count; i++) labels[i] = Label(modes[i]);
        return labels;
    }

    public static ApplyMode FromCombo(int index, bool showAuto, ApplyMode current)
    {
        var modes = Modes(showAuto, current);
        if (index < 0 || index >= modes.Count) return ApplyMode.Selector;
        return modes[index];
    }

    public static int ToCombo(ApplyMode mode, bool showAuto)
    {
        var modes = Modes(showAuto, mode);
        var i = modes.IndexOf(mode);
        return i < 0 ? 2 : i;
    }

    private static List<ApplyMode> Modes(bool showAuto, ApplyMode current)
    {
        var modes = new List<ApplyMode> { ApplyMode.Off, ApplyMode.Confirm, ApplyMode.Selector };
        if (showAuto || current == ApplyMode.Auto) modes.Add(ApplyMode.Auto);
        return modes;
    }
}
