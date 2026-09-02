using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace StatusShift.Windows;

internal static class UiDots
{
    public static void DrawEnabled(bool enabled, bool active)
    {
        var h = ImGui.GetFrameHeight();
        var pos = ImGui.GetCursorScreenPos();
        var center = new Vector2(pos.X + 7, pos.Y + h * 0.5f);
        var dl = ImGui.GetWindowDrawList();
        var fill = enabled
            ? new Vector4(0.30f, 0.82f, 0.40f, 1f)
            : new Vector4(0.78f, 0.28f, 0.30f, 1f);
        dl.AddCircleFilled(center, 5.5f, ImGui.GetColorU32(fill));
        if (active)
            dl.AddCircle(center, 8.2f, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.75f)), 16, 1.4f);
        ImGui.Dummy(new Vector2(16, h));
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(enabled ? (active ? "Enabled · currently matching" : "Enabled") : "Disabled");
    }
}
