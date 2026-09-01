using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace StatusShift.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string newRuleName = "New rule";

    private static readonly DayOfWeek[] Week =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    ];

    private static readonly string[] DayLetters = ["M", "T", "W", "T", "F", "S", "S"];

    public MainWindow(Plugin plugin) : base("StatusShift###StatusShiftMain")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 420),
            MaximumSize = new Vector2(980, 1100),
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
        if (ImGui.Button("Apply now")) plugin.TryApply(force: true);
        ImGui.SameLine();
        if (ImGui.Button("Settings")) plugin.ToggleConfigUi();

        var snap = plugin.Snapshot();
        ImGui.TextDisabled($"Now: {snap.TerritoryId} {snap.TerritoryName} | {snap.JobAbbr} | {snap.WorldName}");

        var match = plugin.CurrentRule();
        ImGui.Separator();
        ImGui.TextUnformatted(match is null ? "Current match: none" : $"Current match: [{match.Name}] P{match.Priority}");
        if (match is not null) ImGui.TextWrapped(plugin.PreviewComment(match));

        ImGui.Separator();
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
            if (ImGui.CollapsingHeader($"{rule.Name}  (P{rule.Priority})###hdr{rule.Id}"))
                DrawRule(cfg, rule, ref removeAt, i);
            ImGui.PopID();
        }

        if (removeAt >= 0)
        {
            cfg.Rules.RemoveAt(removeAt);
            cfg.Save();
        }
    }

    private void DrawRule(Configuration cfg, StatusRule rule, ref int removeAt, int index)
    {
        var on = rule.Enabled;
        if (ImGui.Checkbox("On", ref on)) { rule.Enabled = on; cfg.Save(); }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(180);
        var name = rule.Name;
        if (ImGui.InputText("Name", ref name, 64)) { rule.Name = name; cfg.Save(); }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(80);
        var prio = rule.Priority;
        if (ImGui.InputInt("Prio", ref prio)) { rule.Priority = prio; cfg.Save(); }

        var comment = rule.SearchComment;
        if (ImGui.InputText("Comment", ref comment, 192)) { rule.SearchComment = comment; cfg.Save(); }

        var status = (int)rule.OnlineStatus;
        var labels = new[] { "Leave alone", "Online", "Role-playing", "Busy", "Away", "Looking for Party" };
        if (ImGui.Combo("Status", ref status, labels, labels.Length))
        {
            rule.OnlineStatus = (OnlineStatusAction)status;
            cfg.Save();
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Schedule");
        DrawSchedule(cfg, rule);

        ImGui.Separator();
        ImGui.TextUnformatted("Activity (all selected must match; none = any)");
        DrawActivities(cfg, rule);

        ImGui.Separator();
        var terr = string.Join(",", rule.TerritoryIds);
        if (ImGui.InputText("Territory IDs", ref terr, 128))
        {
            rule.TerritoryIds = ParseUints(terr);
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("+ zone"))
        {
            var id = plugin.Snapshot().TerritoryId;
            if (!rule.TerritoryIds.Contains(id)) rule.TerritoryIds.Add(id);
            cfg.Save();
        }

        var names = string.Join(",", rule.TerritoryNameContains);
        if (ImGui.InputText("Zone name contains", ref names, 200))
        {
            rule.TerritoryNameContains =
            [
                .. names.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            ];
            cfg.Save();
        }

        var jobs = string.Join(",", rule.JobIds);
        if (ImGui.InputText("Job IDs", ref jobs, 128))
        {
            rule.JobIds = ParseUints(jobs);
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("+ job"))
        {
            var id = plugin.Snapshot().JobId;
            if (id != 0 && !rule.JobIds.Contains(id)) rule.JobIds.Add(id);
            cfg.Save();
        }

        var worlds = string.Join(",", rule.WorldIds);
        if (ImGui.InputText("World IDs", ref worlds, 128))
        {
            rule.WorldIds = ParseUints(worlds);
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("+ world"))
        {
            var id = plugin.Snapshot().WorldId;
            if (id != 0 && !rule.WorldIds.Contains(id)) rule.WorldIds.Add(id);
            cfg.Save();
        }

        if (ImGui.Button("Delete")) removeAt = index;
    }

    private static void DrawSchedule(Configuration cfg, StatusRule rule)
    {
        var sched = rule.Schedule ??= new RuleSchedule();
        var mode = (int)sched.Mode;
        var modes = new[] { "Always", "Daily", "Weekly", "One Time", "Custom" };
        if (ImGui.Combo("##schedmode", ref mode, modes, modes.Length))
        {
            sched.Mode = (ScheduleMode)mode;
            cfg.Save();
        }

        if (sched.Mode is ScheduleMode.OneTime or ScheduleMode.Custom)
        {
            var start = sched.DateStart ?? string.Empty;
            var end = sched.DateEnd ?? string.Empty;
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputText("Date start yyyy-MM-dd", ref start, 12))
            {
                sched.DateStart = string.IsNullOrWhiteSpace(start) ? null : start;
                cfg.Save();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputText("Date end", ref end, 12))
            {
                sched.DateEnd = string.IsNullOrWhiteSpace(end) ? null : end;
                cfg.Save();
            }
        }

        if (sched.Mode is ScheduleMode.Weekly or ScheduleMode.Custom)
        {
            ImGui.TextUnformatted("Repeat on");
            for (var d = 0; d < Week.Length; d++)
            {
                if (d > 0) ImGui.SameLine();
                var selected = sched.Days.Contains(Week[d]);
                if (ImGui.Checkbox(DayLetters[d] + "##d" + d, ref selected))
                {
                    if (selected && !sched.Days.Contains(Week[d])) sched.Days.Add(Week[d]);
                    if (!selected) sched.Days.Remove(Week[d]);
                    cfg.Save();
                }
            }
        }

        if (sched.Mode != ScheduleMode.Always)
        {
            var allDay = sched.AllDay;
            if (ImGui.Checkbox("All Day", ref allDay))
            {
                sched.AllDay = allDay;
                cfg.Save();
            }

            if (!sched.AllDay)
            {
                var sh = sched.StartHour;
                var sm = sched.StartMinute;
                var eh = sched.EndHour;
                var em = sched.EndMinute;
                ImGui.SetNextItemWidth(50);
                if (ImGui.InputInt("Start h", ref sh)) { sched.StartHour = Math.Clamp(sh, 0, 23); cfg.Save(); }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50);
                if (ImGui.InputInt("m##sm", ref sm)) { sched.StartMinute = Math.Clamp(sm, 0, 59); cfg.Save(); }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50);
                if (ImGui.InputInt("End h", ref eh)) { sched.EndHour = Math.Clamp(eh, 0, 23); cfg.Save(); }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50);
                if (ImGui.InputInt("m##em", ref em)) { sched.EndMinute = Math.Clamp(em, 0, 59); cfg.Save(); }
            }
        }
    }

    private static void DrawActivities(Configuration cfg, StatusRule rule)
    {
        foreach (ActivityFlag flag in Enum.GetValues<ActivityFlag>())
        {
            var on = rule.Activities.Contains(flag);
            if (ImGui.Checkbox(flag.ToString(), ref on))
            {
                if (on && !rule.Activities.Contains(flag)) rule.Activities.Add(flag);
                if (!on) rule.Activities.Remove(flag);
                cfg.Save();
            }
            ImGui.SameLine();
        }
        ImGui.NewLine();
    }

    private static List<uint> ParseUints(string raw)
    {
        var list = new List<uint>();
        foreach (var part in raw.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries))
        {
            if (uint.TryParse(part, out var id))
                list.Add(id);
        }
        return list;
    }
}
