using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace StatusShift.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string newRuleName = "New rule";
    private string importMsg = string.Empty;

    private static readonly DayOfWeek[] Week =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    ];
    private static readonly string[] DayLetters = ["M", "T", "W", "T", "F", "S", "S"];
    private static readonly string[] LocationKinds = ["Any place", "Territory ID", "Zone name", "Region", "Zone group", "Residence"];
    private static readonly string[] MatchOps = ["-", "Yes", "No"];
    private static readonly ActivityFlag[] StateChoices =
    [
        ActivityFlag.InCombat, ActivityFlag.WeaponDrawn, ActivityFlag.Dead,
        ActivityFlag.Crafting, ActivityFlag.Gathering, ActivityFlag.Mounted,
        ActivityFlag.Flying, ActivityFlag.Swimming, ActivityFlag.Diving,
        ActivityFlag.WatchingCutscene, ActivityFlag.InDuty, ActivityFlag.WaitingForDutyFinder,
        ActivityFlag.InParty, ActivityFlag.PvP, ActivityFlag.InResidence,
    ];

    public MainWindow(Plugin plugin) : base("Status Shift v0.1.0###StatusShiftMain")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 420),
            MaximumSize = new Vector2(740, 980),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        var enabled = cfg.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled)) { cfg.Enabled = enabled; cfg.Save(); }
        ImGui.SameLine();
        if (ImGui.Button("Apply now")) plugin.TryApply(force: true);
        ImGui.SameLine();
        if (ImGui.Button("Settings")) plugin.ToggleConfigUi();

        var snap = plugin.Snapshot();
        ImGui.TextDisabled($"{snap.WorldName} · {snap.TerritoryName} · i{snap.Instance} · {snap.JobAbbr} ({snap.JobId})");

        var match = plugin.CurrentRule();
        ImGui.Separator();
        ImGui.TextUnformatted(match is null ? "Current match: none" : $"Current match: [{match.Name}] P{match.Priority}");

        ImGui.Separator();
        ImGui.SetNextItemWidth(180);
        ImGui.InputText("##newname", ref newRuleName, 64);
        ImGui.SameLine();
        if (ImGui.Button("Add rule"))
        {
            var nextPrio = cfg.Rules.Count == 0 ? 10 : cfg.Rules.Max(r => r.Priority) + 10;
            cfg.Rules.Add(new StatusRule
            {
                Name = string.IsNullOrWhiteSpace(newRuleName) ? "New rule" : newRuleName,
                Priority = nextPrio,
            });
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Import rule"))
        {
            importMsg = plugin.TryImportOneRule(ImGui.GetClipboardText() ?? string.Empty, out var err)
                ? "Imported from clipboard."
                : err;
        }
        Hint("Paste a rule copied with Copy rule. Does not replace your other rules.");
        if (!string.IsNullOrEmpty(importMsg))
            ImGui.TextDisabled(importMsg);

        StatusRule? remove = null;
        foreach (var rule in cfg.Rules.OrderByDescending(r => r.Priority).ToList())
        {
            ImGui.PushID(rule.Id);
            if (ImGui.CollapsingHeader($"P{rule.Priority}  {rule.Name}###hdr{rule.Id}"))
                DrawRule(cfg, rule, ref remove);
            ImGui.PopID();
        }

        if (remove is not null)
        {
            cfg.Rules.Remove(remove);
            cfg.Save();
        }
    }

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
            Hint("If you are outside this window, nothing else in this rule is checked.");
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
            Hint("Yes = must be true. No = must be false. Dash = ignore.");
            DrawStates(cfg, rule);
            ImGui.TreePop();
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Then set");
        var status = (int)rule.OnlineStatus;
        if (ImGui.Combo("Status", ref status, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
        {
            rule.OnlineStatus = (OnlineStatusAction)status;
            cfg.Save();
        }
        Hint("Online status only. Search comment stays untouched unless you opt in below.");

        var change = rule.ChangeSearchComment;
        if (ImGui.Checkbox("Also change search comment", ref change))
        {
            rule.ChangeSearchComment = change;
            if (!change) rule.SearchComment = string.Empty;
            cfg.Save();
        }
        Hint("Off by default. Search comment is your character intro; leave this unchecked unless you really want it overwritten.");
        if (rule.ChangeSearchComment)
        {
            var comment = rule.SearchComment;
            if (ImGui.InputText("Search comment", ref comment, 192)) { rule.SearchComment = comment; cfg.Save(); }
            ImGui.TextDisabled("Tokens: {zone} {region} {job} {world} {home} {instance} {time}");
        }

        var revert = rule.RevertWhenFalse;
        if (ImGui.Checkbox("Maintain while true; otherwise revert to", ref revert))
        {
            rule.RevertWhenFalse = revert;
            cfg.Save();
        }
        Hint("When this rule stops matching, apply the fallback status. Comment fallback is a separate opt-in.");
        if (rule.RevertWhenFalse)
        {
            var fb = (int)rule.FallbackStatus;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.Combo("Fallback status", ref fb, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
            {
                rule.FallbackStatus = (OnlineStatusAction)fb;
                cfg.Save();
            }
            var fbComment = rule.ChangeFallbackComment;
            if (ImGui.Checkbox("Also change search comment on revert", ref fbComment))
            {
                rule.ChangeFallbackComment = fbComment;
                if (!fbComment) rule.FallbackComment = string.Empty;
                cfg.Save();
            }
            if (rule.ChangeFallbackComment)
            {
                var fbc = rule.FallbackComment;
                if (ImGui.InputText("Fallback comment", ref fbc, 192)) { rule.FallbackComment = fbc; cfg.Save(); }
            }
        }

        if (ImGui.Button("Copy rule"))
        {
            ImGui.SetClipboardText(plugin.ExportRuleJson(rule));
            importMsg = $"Copied {rule.Name}.";
        }
        Hint("Copies this rule as JSON for Import rule or sharing.");
        ImGui.SameLine();
        var io = ImGui.GetIO();
        var canDelete = io.KeyShift || io.KeyCtrl;
        if (!canDelete) ImGui.BeginDisabled();
        if (ImGui.Button("Delete rule")) remove = rule;
        if (!canDelete) ImGui.EndDisabled();
        Hint("Hold Shift or Ctrl, then click Delete.");
    }

    private void DrawLocation(Configuration cfg, StatusRule rule)
    {
        var snap = plugin.Snapshot();
        ImGui.TextWrapped($"Current: {snap.WorldName} (id {snap.WorldId}) · {snap.TerritoryName} (id {snap.TerritoryId}) · instance {snap.Instance} · map {snap.MapId} · {snap.RegionName} · residence {(snap.InResidence ? "yes" : "no")}");
        Hint("Read-only snapshot. Cheap to show on the open card only.");

        var loc = rule.Location ??= new LocationFilter();
        var world = rule.WorldFilter ?? string.Empty;
        ImGui.SetNextItemWidth(160);
        if (ImGui.InputText("World", ref world, 32)) { rule.WorldFilter = world; cfg.Save(); }
        ImGui.SameLine();
        if (ImGui.Button("Use world")) { rule.WorldFilter = snap.WorldName; cfg.Save(); }
        Hint("Blank = any world. Can combine with the place filter below.");

        var inst = (int)(loc.Instance ?? 0);
        ImGui.SetNextItemWidth(80);
        if (ImGui.InputInt("Instance #", ref inst))
        {
            loc.Instance = inst <= 0 ? null : (uint)inst;
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Use instance")) { loc.Instance = snap.Instance == 0 ? null : snap.Instance; cfg.Save(); }
        Hint("0 = any instance. Use the number from Current above when a zone is instanced.");

        var kindUi = loc.Kind == LocationKind.World ? 0 : loc.Kind == LocationKind.Residence ? 5 : (int)loc.Kind;
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
                ImGui.TextDisabled("Any place on the world above.");
                break;
            case LocationKind.TerritoryId:
            {
                var value = loc.Value;
                ImGui.SetNextItemWidth(120);
                if (ImGui.InputText("Territory ID", ref value, 16)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use place")) { loc.Value = snap.TerritoryId.ToString(); cfg.Save(); }
                break;
            }
            case LocationKind.ZoneName:
            {
                var value = loc.Value;
                ImGui.SetNextItemWidth(180);
                if (ImGui.InputText("Name contains", ref value, 64)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use place")) { loc.Value = snap.TerritoryName; cfg.Save(); }
                break;
            }
            case LocationKind.Region:
            {
                var value = loc.Value;
                ImGui.SetNextItemWidth(180);
                if (ImGui.InputText("Region contains", ref value, 64)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use place")) { loc.Value = snap.RegionName; cfg.Save(); }
                break;
            }
            case LocationKind.ZoneGroup:
            {
                var value = loc.Value;
                ImGui.SetNextItemWidth(180);
                if (ImGui.InputText("Zone group", ref value, 64)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use place")) { loc.Value = snap.ZoneGroupName; cfg.Save(); }
                break;
            }
            case LocationKind.Residence:
                ImGui.TextWrapped("Housing wards, apartments, chambers, cottages, houses, mansions.");
                var filter = loc.Value;
                if (ImGui.InputText("Optional name filter", ref filter, 64)) { loc.Value = filter; cfg.Save(); }
                break;
        }
    }

    private void DrawJob(Configuration cfg, StatusRule rule)
    {
        var snap = plugin.Snapshot();
        ImGui.TextDisabled($"Current: {snap.JobAbbr}  ID {snap.JobId}");
        Hint("Empty fields = any job. ID or abbreviation may match.");

        if (ImGui.BeginTable("jobrow", 2, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableNextColumn();
            var ids = string.Join(",", rule.JobIds);
            if (ImGui.InputText("IDs", ref ids, 48))
            {
                rule.JobIds = ids.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => uint.TryParse(s, out var n) ? n : 0).Where(n => n != 0).ToList();
                cfg.Save();
            }
            ImGui.TableNextColumn();
            var abbrs = string.Join(",", rule.JobAbbrs);
            if (ImGui.InputText("Abbr", ref abbrs, 48))
            {
                rule.JobAbbrs = abbrs.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToUpperInvariant()).ToList();
                cfg.Save();
            }
            ImGui.EndTable();
        }
        if (ImGui.Button("Add current job"))
        {
            if (snap.JobId != 0 && !rule.JobIds.Contains(snap.JobId)) rule.JobIds.Add(snap.JobId);
            if (!string.IsNullOrEmpty(snap.JobAbbr) && !rule.JobAbbrs.Contains(snap.JobAbbr, StringComparer.OrdinalIgnoreCase))
                rule.JobAbbrs.Add(snap.JobAbbr);
            cfg.Save();
        }
    }

    private static void DrawStates(Configuration cfg, StatusRule rule)
    {
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
        ActivityFlag.WaitingForDutyFinder => "Duty Finder",
        ActivityFlag.WatchingCutscene => "Cutscene",
        ActivityFlag.InResidence => "In residence",
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
            if (ImGui.InputText("Start date", ref start, 12))
            {
                sched.DateStart = string.IsNullOrWhiteSpace(start) ? null : start;
                cfg.Save();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(110);
            if (ImGui.InputText("End date", ref end, 12))
            {
                sched.DateEnd = string.IsNullOrWhiteSpace(end) ? null : end;
                cfg.Save();
            }
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

        var sh = sched.StartHour;
        var sm = sched.StartMinute;
        var eh = sched.EndHour;
        var em = sched.EndMinute;
        ImGui.SetNextItemWidth(50);
        if (ImGui.InputInt("Start h", ref sh)) { sched.StartHour = Math.Clamp(sh, 0, 23); cfg.Save(); }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(50);
        if (ImGui.InputInt("m##sm", ref sm)) { sched.StartMinute = Math.Clamp(sm, 0, 59); cfg.Save(); }
        ImGui.SetNextItemWidth(50);
        if (ImGui.InputInt("End h", ref eh)) { sched.EndHour = Math.Clamp(eh, 0, 23); cfg.Save(); }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(50);
        if (ImGui.InputInt("m##em", ref em)) { sched.EndMinute = Math.Clamp(em, 0, 59); cfg.Save(); }
    }

    private static void Hint(string text)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(i)");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(text);
    }
}
