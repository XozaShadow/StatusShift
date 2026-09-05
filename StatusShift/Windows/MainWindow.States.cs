using System;
using Dalamud.Bindings.ImGui;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private static void DrawStates(Configuration cfg, StatusRule rule)
    {
        var any = rule.StateMatch == StateCombine.Any;
        if (ImGui.RadioButton("Match any (OR)", any))
        {
            rule.StateMatch = StateCombine.Any;
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Match all (AND)", !any))
        {
            rule.StateMatch = StateCombine.All;
            cfg.Save();
        }
        Hint("Yes rows use OR or AND. No is always a veto.");

        if (!ImGui.BeginTable("states", 2, ImGuiTableFlags.SizingStretchProp)) return;
        foreach (var flag in StateChoices)
        {
            ImGui.TableNextColumn();
            var existing = rule.States.Find(s => s.Flag == flag);
            var idx = (int)(existing?.Op ?? MatchOp.Any);
            ImGui.SetNextItemWidth(56);
            if (ImGui.Combo(Label(flag), ref idx, MatchOps, MatchOps.Length))
            {
                if (idx == 0) rule.States.RemoveAll(s => s.Flag == flag);
                else if (existing is null) rule.States.Add(new StateFilter { Flag = flag, Op = (MatchOp)idx });
                else existing.Op = (MatchOp)idx;
                cfg.Save();
            }
        }
        ImGui.EndTable();
    }

    private static string Label(ActivityFlag flag) => flag switch
    {
        ActivityFlag.WeaponDrawn => "Weapon drawn",
        ActivityFlag.WeaponShown => "Weapon shown sheathed",
        ActivityFlag.HelmShown => "Helm shown",
        ActivityFlag.Walking => "Walking",
        ActivityFlag.WaitingForDutyFinder => "Duty Finder",
        ActivityFlag.WatchingCutscene => "Cutscene",
        ActivityFlag.InResidence => "In residence",
        ActivityFlag.PartyLeader => "Party leader",
        ActivityFlag.Sitting => "Sitting / emote",
        ActivityFlag.BetweenAreas => "Between areas",
        ActivityFlag.Roleplaying => "RP status on",
        ActivityFlag.TargetingPlayer => "Targeting player",
        ActivityFlag.TargetingEnemy => "Targeting NPC/enemy",
        ActivityFlag.TargetedByPlayer => "Targeted by player",
        _ => flag.ToString(),
    };

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
            ImGui.SetNextItemWidth(110);
            if (ImGui.InputText("Start date", ref start, 16))
            {
                sched.DateStart = string.IsNullOrWhiteSpace(start) ? null : start;
                cfg.Save();
            }
            Hint("YYYY-MM-DD  e.g. 2026-09-01");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(110);
            if (ImGui.InputText("End date", ref end, 16))
            {
                sched.DateEnd = string.IsNullOrWhiteSpace(end) ? null : end;
                cfg.Save();
            }
            Hint("YYYY-MM-DD. Blank end = no end.");
        }

        if (sched.Mode is ScheduleMode.Weekly or ScheduleMode.Custom)
        {
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

        if (sched.Mode == ScheduleMode.Always) return;
        var allDay = sched.AllDay;
        if (sched.Mode is ScheduleMode.Weekly or ScheduleMode.Custom) ImGui.SameLine();
        if (ImGui.Checkbox("All Day", ref allDay)) { sched.AllDay = allDay; cfg.Save(); }
        if (sched.AllDay) return;

        var startText = $"{sched.StartHour:D2}:{sched.StartMinute:D2}";
        var endText = $"{sched.EndHour:D2}:{sched.EndMinute:D2}";
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputText("Start", ref startText, 6) && TryParseHm(startText, out var sh, out var sm))
        {
            sched.StartHour = sh;
            sched.StartMinute = sm;
            cfg.Save();
        }
        Hint("24-hour HH:mm  e.g. 00:00 or 20:30");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputText("End", ref endText, 6) && TryParseHm(endText, out var eh, out var em))
        {
            sched.EndHour = eh;
            sched.EndMinute = em;
            cfg.Save();
        }
        Hint("24-hour HH:mm. End before start = overnight.");
    }
}
