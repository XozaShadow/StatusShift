using System.Collections.Generic;
using System.Linq;
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
        Size = new Vector2(380, 340);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 180),
            MaximumSize = new Vector2(560, 640),
        };
        Flags = ImGuiWindowFlags.NoCollapse;
    }

    public void Show(List<StatusRule> rules)
    {
        matches = rules;
        IsOpen = rules.Count > 0;
    }

    public void Hide() => IsOpen = false;

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        ImGui.TextColored(UiTheme.Teal, "Matching now");
        ImGui.TextDisabled("Checkbox on/off. Click the name to apply.");

        var labels = ApplyModeNames.ComboLabels(false, cfg.ApplyMode);
        var mode = ApplyModeNames.ToCombo(cfg.ApplyMode, false);
        ImGui.SetNextItemWidth(160);
        if (ImGui.Combo("Handling", ref mode, labels, labels.Length))
        {
            cfg.ApplyMode = ApplyModeNames.FromCombo(mode, false, cfg.ApplyMode);
            if (cfg.ApplyMode == ApplyMode.Auto)
                cfg.ApplyMode = ApplyMode.Selector;
            cfg.Save();
            plugin.RequestEval();
        }

        var active = matches.Where(r => r.Enabled).ToList();
        var inactive = matches.Where(r => !r.Enabled).ToList();

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Amber, "Active");
        DrawGroup(cfg, active, "No enabled match.");

        ImGui.Separator();
        ImGui.TextDisabled("Inactive");
        DrawGroup(cfg, inactive, "No disabled match.");

        if (ImGui.Button("Close"))
            IsOpen = false;
    }

    private void DrawGroup(Configuration cfg, List<StatusRule> rules, string empty)
    {
        if (rules.Count == 0)
        {
            ImGui.TextDisabled(empty);
            return;
        }

        foreach (var rule in rules)
        {
            ImGui.PushID(rule.Id);
            var on = rule.Enabled;
            if (ImGui.Checkbox("##on", ref on))
            {
                rule.Enabled = on;
                cfg.Save();
                plugin.RequestEval();
            }
            ImGui.SameLine();
            var label = $"P{rule.Priority}  {rule.Name}  {StatusShort(rule)}";
            if (ImGui.Selectable(label))
            {
                plugin.TryApply(rule, force: true);
                IsOpen = false;
            }
            ImGui.PopID();
        }
    }

    private static string StatusShort(StatusRule rule) =>
        rule.OnlineStatus == OnlineStatusAction.LeaveAlone
            ? "-"
            : ChatSender.StatusLabels[(int)rule.OnlineStatus];
}
