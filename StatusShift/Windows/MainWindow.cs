using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace StatusShift.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string newRuleName = "New rule";

    public MainWindow(Plugin plugin) : base("StatusShift###StatusShiftMain")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 360),
            MaximumSize = new Vector2(900, 900),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        var enabled = cfg.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            cfg.Enabled = enabled;
            cfg.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("Apply now"))
            plugin.TryApply(force: true);

        ImGui.SameLine();
        if (ImGui.Button("Settings"))
            plugin.ToggleConfigUi();

        var match = plugin.CurrentRule();
        ImGui.Separator();
        ImGui.TextUnformatted(match is null
            ? "Current match: none"
            : $"Current match: [{match.Name}] P{match.Priority}");
        if (match is not null)
            ImGui.TextWrapped(plugin.PreviewComment(match));

        ImGui.Separator();
        ImGui.TextUnformatted("Rules (highest priority first)");

        ImGui.SetNextItemWidth(220);
        ImGui.InputText("##newname", ref newRuleName, 64);
        ImGui.SameLine();
        if (ImGui.Button("Add rule"))
        {
            cfg.Rules.Add(new StatusRule { Name = string.IsNullOrWhiteSpace(newRuleName) ? "New rule" : newRuleName });
            cfg.Save();
        }

        var removeAt = -1;
        for (var i = 0; i < cfg.Rules.Count; i++)
        {
            var rule = cfg.Rules[i];
            ImGui.PushID(rule.Id);
            ImGui.Separator();

            var on = rule.Enabled;
            if (ImGui.Checkbox("On", ref on))
            {
                rule.Enabled = on;
                cfg.Save();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(180);
            var name = rule.Name;
            if (ImGui.InputText("Name", ref name, 64))
            {
                rule.Name = name;
                cfg.Save();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            var prio = rule.Priority;
            if (ImGui.InputInt("Prio", ref prio))
            {
                rule.Priority = prio;
                cfg.Save();
            }

            var comment = rule.SearchComment;
            if (ImGui.InputText("Comment", ref comment, 192))
            {
                rule.SearchComment = comment;
                cfg.Save();
            }

            var status = (int)rule.OnlineStatus;
            var labels = new[] { "Leave alone", "Online", "Role-playing", "Busy", "Away", "Looking for Party" };
            if (ImGui.Combo("Status", ref status, labels, labels.Length))
            {
                rule.OnlineStatus = (OnlineStatusAction)status;
                cfg.Save();
            }

            var inDuty = rule.InDuty == true;
            if (ImGui.Checkbox("Only in duty", ref inDuty))
            {
                rule.InDuty = inDuty ? true : null;
                cfg.Save();
            }

            ImGui.SetNextItemWidth(80);
            var start = rule.TimeStart ?? string.Empty;
            if (ImGui.InputText("From HH:mm", ref start, 8))
            {
                rule.TimeStart = string.IsNullOrWhiteSpace(start) ? null : start;
                cfg.Save();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);
            var end = rule.TimeEnd ?? string.Empty;
            if (ImGui.InputText("To", ref end, 8))
            {
                rule.TimeEnd = string.IsNullOrWhiteSpace(end) ? null : end;
                cfg.Save();
            }

            var terr = string.Join(",", rule.TerritoryIds);
            if (ImGui.InputText("Territory IDs", ref terr, 128))
            {
                rule.TerritoryIds = ParseUshorts(terr);
                cfg.Save();
            }

            if (ImGui.Button("Delete"))
                removeAt = i;

            ImGui.PopID();
        }

        if (removeAt >= 0)
        {
            cfg.Rules.RemoveAt(removeAt);
            cfg.Save();
        }
    }

    private static System.Collections.Generic.List<ushort> ParseUshorts(string raw)
    {
        var list = new System.Collections.Generic.List<ushort>();
        foreach (var part in raw.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries))
        {
            if (ushort.TryParse(part, out var id))
                list.Add(id);
        }
        return list;
    }
}
