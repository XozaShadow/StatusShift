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

    private static readonly DayOfWeek[] Week =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    ];

    private static readonly string[] DayLetters = ["M", "T", "W", "T", "F", "S", "S"];
    private static readonly string[] LocationKinds = ["Any", "Territory ID", "Zone name", "Region", "Zone group", "World", "Residence"];
    private static readonly string[] MatchOps = ["-", "Yes", "No"];

    private static readonly ActivityFlag[] StateChoices =
    [
        ActivityFlag.InCombat, ActivityFlag.Dead, ActivityFlag.Crafting, ActivityFlag.Gathering,
        ActivityFlag.Mounted, ActivityFlag.Flying, ActivityFlag.Swimming, ActivityFlag.Diving,
        ActivityFlag.WatchingCutscene, ActivityFlag.InDuty, ActivityFlag.WaitingForDutyFinder,
        ActivityFlag.InParty, ActivityFlag.PartyLeader, ActivityFlag.PvP, ActivityFlag.InResidence,
    ];

    public MainWindow(Plugin plugin) : base("StatusShift###StatusShiftMain")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(560, 460),
            MaximumSize = new Vector2(980, 1200),
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
        ImGui.TextDisabled($"{snap.TerritoryName}  ·  {snap.RegionName}  ·  {snap.JobAbbr}  ·  {snap.WorldName}");

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
            var nextPrio = cfg.Rules.Count == 0 ? 10 : cfg.Rules.Max(r => r.Priority) + 10;
            cfg.Rules.Add(new StatusRule
            {
                Name = string.IsNullOrWhiteSpace(newRuleName) ? "New rule" : newRuleName,
                Priority = nextPrio,
            });
            cfg.Save();
        }

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
            ImGui.SetNextItemWidth(90);
            var prio = rule.Priority;
            if (ImGui.InputInt("Prio", ref prio)) { rule.Priority = prio; cfg.Save(); }
            ImGui.EndTable();
        }

        var comment = rule.SearchComment;
        if (ImGui.InputText("Search comment", ref comment, 192)) { rule.SearchComment = comment; cfg.Save(); }
        ImGui.TextDisabled("Tokens: {zone} {region} {job} {world} {home} {time}");

        var status = (int)rule.OnlineStatus;
        if (ImGui.Combo("Status", ref status, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
        {
            rule.OnlineStatus = (OnlineStatusAction)status;
            cfg.Save();
        }

        var revert = rule.RevertWhenFalse;
        if (ImGui.Checkbox("Only while true; otherwise revert to", ref revert))
        {
            rule.RevertWhenFalse = revert;
            cfg.Save();
        }
        if (rule.RevertWhenFalse)
        {
            ImGui.SameLine();
            var fb = (int)rule.FallbackStatus;
            ImGui.SetNextItemWidth(180);
            if (ImGui.Combo("##fallback", ref fb, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
            {
                rule.FallbackStatus = (OnlineStatusAction)fb;
                cfg.Save();
            }
            var fbc = rule.FallbackComment;
            if (ImGui.InputText("Fallback comment", ref fbc, 192))
            {
                rule.FallbackComment = fbc;
                cfg.Save();
            }
        }

        if (ImGui.TreeNode("Schedule — skipped if outside this window"))
        {
            DrawSchedule(cfg, rule);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Location — one place at a time"))
        {
            DrawLocation(cfg, rule);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("State — Yes / No / ignore"))
        {
            DrawStates(cfg, rule);
            ImGui.TreePop();
        }

        if (ImGui.Button("Delete rule")) remove = rule;
    }

    private void DrawLocation(Configuration cfg, StatusRule rule)
    {
        var loc = rule.Location ??= new LocationFilter();
        var kind = (int)loc.Kind;
        if (ImGui.Combo("Match", ref kind, LocationKinds, LocationKinds.Length))
        {
            loc.Kind = (LocationKind)kind;
            cfg.Save();
        }

        var snap = plugin.Snapshot();
        switch (loc.Kind)
        {
            case LocationKind.Any:
                ImGui.TextDisabled("Any zone. Location is skipped.");
                break;
            case LocationKind.TerritoryId:
                var value = loc.Value;
                if (ImGui.InputText("Territory ID", ref value, 16)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use current")) { loc.Value = snap.TerritoryId.ToString(); cfg.Save(); }
                ImGui.TextDisabled($"Current: {snap.TerritoryId} {snap.TerritoryName}");
                break;
            case LocationKind.ZoneName:
                value = loc.Value;
                if (ImGui.InputText("Name contains", ref value, 64)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use current")) { loc.Value = snap.TerritoryName; cfg.Save(); }
                break;
            case LocationKind.Region:
                value = loc.Value;
                if (ImGui.InputText("Region contains", ref value, 64)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use current")) { loc.Value = snap.RegionName; cfg.Save(); }
                ImGui.TextDisabled($"Current region: {snap.RegionName}");
                break;
            case LocationKind.ZoneGroup:
                value = loc.Value;
                if (ImGui.InputText("Zone group contains", ref value, 64)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use current")) { loc.Value = snap.ZoneGroupName; cfg.Save(); }
                ImGui.TextDisabled($"Current group: {snap.ZoneGroupName}");
                break;
            case LocationKind.World:
                value = loc.Value;
                if (ImGui.InputText("World name or ID", ref value, 32)) { loc.Value = value; cfg.Save(); }
                ImGui.SameLine();
                if (ImGui.Button("Use current")) { loc.Value = snap.WorldName; cfg.Save(); }
                break;
            case LocationKind.Residence:
                ImGui.TextWrapped("Matches housing wards, apartments, private chambers, cottages, houses, mansions.");
                value = loc.Value;
                if (ImGui.InputText("Optional name filter", ref value, 64)) { loc.Value = value; cfg.Save(); }
                break;
        }
    }

    private static void DrawStates(Configuration cfg, StatusRule rule)
    {
        ImGui.TextDisabled("Ignore (-) skips the check. Yes requires it. No forbids it.");
        foreach (var flag in StateChoices)
        {
            var existing = rule.States.Find(s => s.Flag == flag);
            var op = existing?.Op ?? MatchOp.Any;
            var idx = (int)op;
            ImGui.SetNextItemWidth(70);
            if (ImGui.Combo(flag.ToString(), ref idx, MatchOps, MatchOps.Length))
            {
                if (idx == 0)
                    rule.States.RemoveAll(s => s.Flag == flag);
                else if (existing is null)
                    rule.States.Add(new StateFilter { Flag = flag, Op = (MatchOp)idx });
                else
                    existing.Op = (MatchOp)idx;
                cfg.Save();
            }
        }
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
            if (ImGui.InputText("Date start", ref start, 12))
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
}
