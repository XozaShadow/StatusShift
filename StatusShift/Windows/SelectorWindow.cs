using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace StatusShift.Windows;

public sealed class SelectorWindow : Window
{
    private readonly Plugin plugin;
    private List<StatusRule> matches = [];

    public SelectorWindow(Plugin plugin)
        : base("Status Shift · pick a rule###StatusShiftSelector")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(280, 120),
            MaximumSize = new Vector2(520, 420),
        };
        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;
    }

    public void Show(List<StatusRule> rules)
    {
        matches = rules;
        IsOpen = rules.Count > 0;
    }

    public void Hide() => IsOpen = false;

    public override void Draw()
    {
        ImGui.TextColored(UiTheme.Teal, "Matching rules");
        ImGui.TextDisabled("Click one to apply. Window closes after.");
        if (matches.Count == 0)
        {
            ImGui.TextDisabled("None.");
            return;
        }

        foreach (var rule in matches)
        {
            ImGui.PushID(rule.Id);
            var label = $"P{rule.Priority}  {rule.Name}  {StatusShort(rule)}";
            if (ImGui.Selectable(label))
            {
                plugin.TryApply(rule, force: true);
                IsOpen = false;
            }
            ImGui.PopID();
        }

        if (ImGui.Button("Close"))
            IsOpen = false;
    }

    private static string StatusShort(StatusRule rule) =>
        rule.OnlineStatus == OnlineStatusAction.LeaveAlone
            ? "-"
            : ChatSender.StatusLabels[(int)rule.OnlineStatus];
}
