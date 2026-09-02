using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace StatusShift.Windows;

internal static class UiTheme
{
    public static readonly Vector4 Amber = new(0.96f, 0.62f, 0.22f, 1f);
    public static readonly Vector4 Teal = new(0.22f, 0.74f, 0.70f, 1f);
    public static readonly Vector4 Mute = new(0.62f, 0.64f, 0.68f, 1f);

    public static void Section(string label, bool action = false)
        => ImGui.TextColored(action ? Amber : Teal, label);
}
