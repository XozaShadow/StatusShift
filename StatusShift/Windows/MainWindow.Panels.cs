using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private void DrawRule(Configuration cfg, StatusRule rule, ref StatusRule? remove)
    {
        if (ImGui.BeginTable("hdr", 3, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableNextColumn();
            var on = rule.Enabled;
            if (ImGui.Checkbox("On", ref on)) { rule.Enabled = on; cfg.Save(); }
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var name = rule.Name;
            if (ImGui.InputText("##name", ref name, 64)) { rule.Name = name; cfg.Save(); }
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(80);
            var prio = rule.Priority;
            if (ImGui.InputInt("Prio", ref prio)) { rule.Priority = prio; cfg.Save(); }
            ImGui.EndTable();
        }

        if (ImGui.TreeNode("At schedule"))
        {
            Hint("Outside this window, the rule is skipped.");
            DrawSchedule(cfg, rule);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("If location"))
        {
            DrawLocation(cfg, rule);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("If job"))
        {
            DrawJob(cfg, rule);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("If state"))
        {
            DrawStates(cfg, rule);
            ImGui.TreePop();
        }

        ImGui.Separator();
        UiTheme.Section("Then set", action: true);
        var status = (int)rule.OnlineStatus;
        if (ImGui.Combo("Status", ref status, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
        {
            rule.OnlineStatus = (OnlineStatusAction)status;
            cfg.Save();
        }
        Hint("Leave alone = do not touch online status.");

        var sticky = rule.Sticky;
        if (ImGui.Checkbox("Sticky (do not revert when rule ends)", ref sticky))
        {
            rule.Sticky = sticky;
            cfg.Save();
        }
        if (!rule.Sticky)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted("Else revert to:");
            ImGui.SameLine();
            var fb = (int)rule.FallbackStatus;
            ImGui.SetNextItemWidth(160);
            if (ImGui.Combo("##revert", ref fb, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
            {
                rule.FallbackStatus = (OnlineStatusAction)fb;
                cfg.Save();
            }
        }

        var cmd = rule.Command ?? string.Empty;
        if (ImGui.InputText("Command / macro", ref cmd, 192))
        {
            rule.Command = cmd;
            cfg.Save();
        }
        Hint("Optional. Runs even if status is Leave alone. Example: /sit");
        if (!string.IsNullOrWhiteSpace(rule.Command))
        {
            var rerun = rule.RerunCommand;
            if (ImGui.Checkbox("Rerun every interval (s)", ref rerun))
            {
                rule.RerunCommand = rerun;
                cfg.Save();
            }
            Hint("Off or 0 = run once when the rule starts matching.");
            if (rule.RerunCommand)
            {
                var every = rule.CommandIntervalSeconds;
                ImGui.SetNextItemWidth(80);
                if (ImGui.InputInt("Interval s", ref every))
                {
                    rule.CommandIntervalSeconds = Math.Max(0, every);
                    cfg.Save();
                }
                Hint("0 = use the Settings check interval.");
            }
        }

        var change = rule.ChangeSearchComment;
        if (ImGui.Checkbox("Also change search comment", ref change))
        {
            rule.ChangeSearchComment = change;
            if (!change)
            {
                rule.SearchComment = string.Empty;
                rule.ChangeFallbackComment = false;
                rule.FallbackComment = string.Empty;
            }
            cfg.Save();
        }
        Hint("Off by default. Your search comment is a character intro.");
        if (rule.ChangeSearchComment)
        {
            var comment = rule.SearchComment;
            if (ImGui.InputText("While this rule matches", ref comment, 192)) { rule.SearchComment = comment; cfg.Save(); }
            if (!rule.Sticky)
            {
                var fbc = rule.FallbackComment;
                if (ImGui.InputText("When it no longer matches", ref fbc, 192))
                {
                    rule.FallbackComment = fbc;
                    rule.ChangeFallbackComment = !string.IsNullOrWhiteSpace(fbc);
                    cfg.Save();
                }
            }
            ImGui.TextDisabled("Tokens: {zone} {region} {job} {world} {home} {ward} {plot} {time}");
        }

        if (ImGui.Button("Copy rule"))
        {
            ImGui.SetClipboardText(plugin.ExportRuleJson(rule));
            importMsg = $"Copied {rule.Name}.";
        }
        ImGui.SameLine();
        var io = ImGui.GetIO();
        var canDelete = io.KeyShift || io.KeyCtrl;
        if (!canDelete) ImGui.BeginDisabled();
        if (ImGui.Button("Delete rule")) remove = rule;
        if (!canDelete) ImGui.EndDisabled();
        Hint("Hold Shift or Ctrl to delete.");
    }

    private void DrawLocation(Configuration cfg, StatusRule rule)
    {
        UiTheme.Section("Worlds");
        DrawWorldPicker(cfg, rule);

        var loc = rule.Location ??= new LocationFilter();
        var kindUi = loc.Kind == LocationKind.Residence ? 5 : loc.Kind == LocationKind.World ? 0 : (int)loc.Kind;
        if (kindUi < 0 || kindUi > 5) kindUi = 0;
        if (ImGui.Combo("Place", ref kindUi, LocationKinds, LocationKinds.Length))
        {
            loc.Kind = kindUi switch
            {
                1 => LocationKind.TerritoryId,
                2 => LocationKind.ZoneName,
                3 => LocationKind.Region,
                4 => LocationKind.ZoneGroup,
                5 => LocationKind.Residence,
                _ => LocationKind.Any,
            };
            cfg.Save();
        }

        switch (loc.Kind)
        {
            case LocationKind.Any:
            case LocationKind.World:
                ImGui.TextDisabled("Any place on the selected worlds.");
                break;
            case LocationKind.TerritoryId:
            {
                var value = loc.Value;
                ImGui.SetNextItemWidth(120);
                if (ImGui.InputText("Territory ID", ref value, 16)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use place")) { loc.Value = plugin.Snapshot().TerritoryId.ToString(); cfg.Save(); }
                break;
            }
            case LocationKind.ZoneName:
            {
                var value = loc.Value;
                ImGui.SetNextItemWidth(180);
                if (ImGui.InputText("Name contains", ref value, 64)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use place")) { loc.Value = plugin.Snapshot().TerritoryName; cfg.Save(); }
                break;
            }
            case LocationKind.Region:
            {
                var value = loc.Value;
                ImGui.SetNextItemWidth(180);
                if (ImGui.InputText("Region contains", ref value, 64)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use place")) { loc.Value = plugin.Snapshot().RegionName; cfg.Save(); }
                break;
            }
            case LocationKind.ZoneGroup:
            {
                var value = loc.Value;
                ImGui.SetNextItemWidth(180);
                if (ImGui.InputText("Zone group", ref value, 64)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use place")) { loc.Value = plugin.Snapshot().ZoneGroupName; cfg.Save(); }
                break;
            }
            case LocationKind.Residence:
                DrawResidence(cfg, loc);
                break;
        }

        ImGui.TextDisabled("Also match any of these zone names");
        foreach (var zname in rule.TerritoryNameContains.ToList())
        {
            if (ImGui.SmallButton($"x##zn{zname}"))
            {
                rule.TerritoryNameContains.Remove(zname);
                cfg.Save();
            }
            ImGui.SameLine();
            ImGui.TextUnformatted(zname);
        }
        ImGui.SetNextItemWidth(180);
        ImGui.InputText("##zonecustom", ref zoneCustom, 64);
        ImGui.SameLine();
        if (ImGui.Button("Add zone") && !string.IsNullOrWhiteSpace(zoneCustom))
        {
            var add = zoneCustom.Trim();
            if (!rule.TerritoryNameContains.Contains(add, StringComparer.OrdinalIgnoreCase))
                rule.TerritoryNameContains.Add(add);
            zoneCustom = string.Empty;
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Add current zone"))
        {
            var here = plugin.Snapshot().TerritoryName;
            if (!string.IsNullOrWhiteSpace(here) && !rule.TerritoryNameContains.Contains(here, StringComparer.OrdinalIgnoreCase))
                rule.TerritoryNameContains.Add(here);
            cfg.Save();
        }
    }

    private void DrawWorldPicker(Configuration cfg, StatusRule rule)
    {
        ImGui.SetNextItemWidth(160);
        ImGui.InputTextWithHint("##worldsearch", "Search worlds...", ref worldSearch, 32);
        var sheet = Plugin.DataManager.GetExcelSheet<World>();
        if (sheet is not null && ImGui.BeginChild("worldavail", new Vector2(220, 90), true))
        {
            foreach (var row in sheet)
            {
                if (row.RowId == 0) continue;
                var wname = row.Name.ToString();
                if (string.IsNullOrWhiteSpace(wname) || wname.StartsWith("Dev", StringComparison.Ordinal)) continue;
                if (!string.IsNullOrWhiteSpace(worldSearch) && !wname.Contains(worldSearch, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (rule.WorldNames.Contains(wname, StringComparer.OrdinalIgnoreCase)) continue;
                if (ImGui.Selectable(wname))
                {
                    rule.WorldNames.Add(wname);
                    if (!rule.WorldIds.Contains(row.RowId)) rule.WorldIds.Add(row.RowId);
                    cfg.Save();
                }
            }
            ImGui.EndChild();
        }
        ImGui.SameLine();
        if (ImGui.BeginChild("worldsel", new Vector2(180, 90), true))
        {
            ImGui.TextDisabled("Included");
            foreach (var wname in rule.WorldNames.ToList())
            {
                if (ImGui.Selectable(wname))
                {
                    rule.WorldNames.RemoveAll(n => n.Equals(wname, StringComparison.OrdinalIgnoreCase));
                    cfg.Save();
                }
            }
            ImGui.EndChild();
        }
        if (ImGui.Button("Add current world"))
        {
            var snap = plugin.Snapshot();
            if (!string.IsNullOrEmpty(snap.WorldName) && !rule.WorldNames.Contains(snap.WorldName, StringComparer.OrdinalIgnoreCase))
                rule.WorldNames.Add(snap.WorldName);
            if (snap.WorldId != 0 && !rule.WorldIds.Contains(snap.WorldId))
                rule.WorldIds.Add(snap.WorldId);
            cfg.Save();
        }
        Hint("Empty list = any world. Click a name on the right to remove.");
    }

    private void DrawResidence(Configuration cfg, LocationFilter loc)
    {
        var here = plugin.Snapshot().Housing;
        ImGui.TextDisabled($"Current residence: {here.Summary}");
        var kind = loc.ResidenceKind == ResidenceKind.Apartment ? 1 : 0;
        if (ImGui.Combo("Type", ref kind, ["House", "Apartment"], 2))
        {
            loc.ResidenceKind = kind == 1 ? ResidenceKind.Apartment : ResidenceKind.House;
            cfg.Save();
        }
        var district = loc.District ?? string.Empty;
        ImGui.SetNextItemWidth(180);
        if (ImGui.InputText("Zone / district", ref district, 48)) { loc.District = district; cfg.Save(); }
        var ward = loc.Ward;
        ImGui.SetNextItemWidth(70);
        if (ImGui.InputInt("Ward", ref ward)) { loc.Ward = Math.Clamp(ward, 0, 30); cfg.Save(); }
        var sub = loc.Subdivision;
        if (ImGui.Checkbox("Subdivision", ref sub)) { loc.Subdivision = sub; cfg.Save(); }
        if (loc.ResidenceKind == ResidenceKind.House)
        {
            var plot = loc.Plot;
            ImGui.SetNextItemWidth(70);
            if (ImGui.InputInt("Plot", ref plot)) { loc.Plot = Math.Clamp(plot, 0, 60); cfg.Save(); }
        }
        else
        {
            var apt = loc.Apartment;
            ImGui.SetNextItemWidth(70);
            if (ImGui.InputInt("Apartment #", ref apt)) { loc.Apartment = Math.Max(0, apt); cfg.Save(); }
        }
        if (ImGui.Button("Use current residence"))
        {
            loc.District = here.District;
            loc.Ward = here.Ward;
            loc.Plot = here.Plot;
            loc.Apartment = here.Apartment;
            loc.Subdivision = here.Subdivision;
            loc.ResidenceKind = here.Kind == ResidenceKind.Apartment ? ResidenceKind.Apartment : ResidenceKind.House;
            cfg.Save();
        }
        Hint("0 ward/plot/apt = any. Subdivision checked = only subdivision.");
    }

    private void DrawJob(Configuration cfg, StatusRule rule)
    {
        ImGui.SetNextItemWidth(160);
        ImGui.InputTextWithHint("##jobsearch", "Search jobs...", ref jobSearch, 24);
        var sheet = Plugin.DataManager.GetExcelSheet<ClassJob>();
        if (sheet is not null && ImGui.BeginChild("jobavail", new Vector2(220, 110), true))
        {
            foreach (var row in sheet)
            {
                if (row.RowId == 0) continue;
                var abbr = row.Abbreviation.ToString();
                var jname = row.Name.ToString();
                if (string.IsNullOrWhiteSpace(abbr) || abbr.Length > 4) continue;
                if (!string.IsNullOrWhiteSpace(jobSearch)
                    && !abbr.Contains(jobSearch, StringComparison.OrdinalIgnoreCase)
                    && !jname.Contains(jobSearch, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (rule.JobAbbrs.Contains(abbr, StringComparer.OrdinalIgnoreCase)) continue;
                if (ImGui.Selectable($"{abbr}  {jname}"))
                {
                    rule.JobAbbrs.Add(abbr);
                    if (!rule.JobIds.Contains(row.RowId)) rule.JobIds.Add(row.RowId);
                    cfg.Save();
                }
            }
            ImGui.EndChild();
        }
        ImGui.SameLine();
        if (ImGui.BeginChild("jobsel", new Vector2(140, 110), true))
        {
            ImGui.TextDisabled("Included");
            foreach (var abbr in rule.JobAbbrs.ToList())
            {
                if (ImGui.Selectable(abbr))
                {
                    rule.JobAbbrs.RemoveAll(a => a.Equals(abbr, StringComparison.OrdinalIgnoreCase));
                    cfg.Save();
                }
            }
            ImGui.EndChild();
        }
        if (ImGui.Button("Add current job"))
        {
            var snap = plugin.Snapshot();
            if (!string.IsNullOrEmpty(snap.JobAbbr) && !rule.JobAbbrs.Contains(snap.JobAbbr, StringComparer.OrdinalIgnoreCase))
                rule.JobAbbrs.Add(snap.JobAbbr);
            if (snap.JobId != 0 && !rule.JobIds.Contains(snap.JobId)) rule.JobIds.Add(snap.JobId);
            cfg.Save();
        }
        Hint("Empty list = any job. Click a selected abbr to remove.");
    }

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
            ImGui.TextUnformatted("Days");
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
