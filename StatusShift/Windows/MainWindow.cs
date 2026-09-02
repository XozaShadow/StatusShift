using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;

namespace StatusShift.Windows;

public partial class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string newRuleName = "New rule";
    private string importMsg = string.Empty;
    private string jobSearch = string.Empty;
    private string worldSearch = string.Empty;
    private string zoneCustom = string.Empty;

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
        ActivityFlag.InCombat, ActivityFlag.WeaponDrawn, ActivityFlag.WeaponShown, ActivityFlag.HelmShown,
        ActivityFlag.Walking, ActivityFlag.Dead,
        ActivityFlag.Crafting, ActivityFlag.Gathering, ActivityFlag.Mounted,
        ActivityFlag.Flying, ActivityFlag.Swimming, ActivityFlag.Diving,
        ActivityFlag.WatchingCutscene, ActivityFlag.InDuty, ActivityFlag.WaitingForDutyFinder,
        ActivityFlag.InParty, ActivityFlag.PartyLeader, ActivityFlag.PvP,
        ActivityFlag.InResidence, ActivityFlag.Sitting, ActivityFlag.Casting,
        ActivityFlag.Jumping, ActivityFlag.Occupied, ActivityFlag.Trading,
        ActivityFlag.BetweenAreas, ActivityFlag.Roleplaying,
        ActivityFlag.TargetingPlayer, ActivityFlag.TargetingEnemy, ActivityFlag.TargetedByPlayer,
    ];

    public MainWindow(Plugin plugin) : base("Status Shift v0.1.3###StatusShiftMain")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(560, 420),
            MaximumSize = new Vector2(780, 1040),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        var enabled = cfg.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled)) { cfg.Enabled = enabled; cfg.Save(); }
        ImGui.SameLine();
        if (ImGui.Button("Update Now")) plugin.TryApply(force: true);
        Hint("Re-check rules and apply the current match.");
        ImGui.SameLine();
        if (ImGui.Button("Settings")) plugin.ToggleConfigUi();

        if (cfg.ShowSnapshot)
        {
            var snap = plugin.Snapshot();
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Teal);
            ImGui.TextUnformatted($"{snap.WorldName} · {snap.TerritoryName} ({snap.TerritoryId}) · {snap.Housing.Summary}");
            ImGui.PopStyleColor();
            ImGui.TextDisabled($"Job {snap.JobAbbr}  ID {snap.JobId}");
        }

        var match = plugin.CurrentRule();
        ImGui.TextColored(UiTheme.Amber, match is null ? "Current match: none" : $"Current match: [{match.Name}] P{match.Priority}");

        ImGui.Separator();
        ImGui.SetNextItemWidth(180);
        ImGui.InputText("##newname", ref newRuleName, 64);
        ImGui.SameLine();
        if (ImGui.Button("Add rule"))
        {
            var nextPrio = cfg.Rules.Count == 0 ? 10 : cfg.Rules.Max(r => r.Priority) + 10;
            cfg.Rules.Add(new StatusRule
            {
                Name = string.IsNullOrWhiteSpace(newRuleName) ? "New rule" : newRuleName.Trim(),
                Priority = nextPrio,
                Enabled = false,
            });
            newRuleName = string.Empty;
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Import rule"))
        {
            importMsg = plugin.TryImportOneRule(ImGui.GetClipboardText() ?? string.Empty, out var err)
                ? "Imported from clipboard."
                : err;
        }
        Hint("Paste a rule copied with Copy rule.");
        if (!string.IsNullOrEmpty(importMsg))
            ImGui.TextDisabled(importMsg);

        StatusRule? remove = null;
        foreach (var rule in cfg.Rules.OrderByDescending(r => r.Priority).ToList())
        {
            ImGui.PushID(rule.Id);
            var row = ImGui.GetCursorPos();
            UiDots.DrawEnabled(rule.Enabled, match?.Id == rule.Id);
            ImGui.SetCursorPos(new Vector2(row.X + 18, row.Y));
            if (ImGui.CollapsingHeader($"{HeaderLeft(rule)}###hdr{rule.Id}"))
                DrawRule(cfg, rule, ref remove);
            ImGui.PopID();
        }

        if (remove is not null)
        {
            cfg.Rules.Remove(remove);
            cfg.Save();
        }
    }

    private static string HeaderLeft(StatusRule rule)
    {
        var status = rule.OnlineStatus == OnlineStatusAction.LeaveAlone
            ? "-"
            : ChatSender.StatusLabels[(int)rule.OnlineStatus];
        var cmd = rule.HasCommand ? $" [{rule.Command.Trim()}]" : string.Empty;
        var glue = rule.Sticky ? " pin" : string.Empty;
        return $"P{rule.Priority}  {rule.Name}  >  {status}{cmd}{glue}    {HeaderChips(rule)}";
    }

    private static string HeaderChips(StatusRule rule)
    {
        var parts = new List<string>();
        var sched = rule.Schedule ?? new RuleSchedule();
        if (sched.Mode != ScheduleMode.Always) parts.Add("Schd");
        var loc = rule.Location ?? new LocationFilter();
        if ((loc.Kind != LocationKind.Any && loc.Kind != LocationKind.World)
            || !string.IsNullOrWhiteSpace(rule.WorldFilter)
            || rule.WorldNames.Count > 0
            || rule.WorldIds.Count > 0
            || rule.TerritoryIds.Count > 0
            || rule.TerritoryNameContains.Count > 0)
            parts.Add("Loc");
        if (rule.JobIds.Count > 0 || rule.JobAbbrs.Count > 0) parts.Add("Job");
        var stateN = rule.States.Count(s => s.Op != MatchOp.Any);
        if (stateN == 1) parts.Add("St");
        else if (stateN > 1) parts.Add(rule.StateMatch == StateCombine.Any ? $"Stx{stateN}OR" : $"Stx{stateN}AND");
        return string.Join("  ", parts);
    }

    private static bool TryParseHm(string text, out int hour, out int minute)
    {
        hour = 0;
        minute = 0;
        text = text.Trim();
        if (TimeSpan.TryParseExact(text, ["h\\:mm", "hh\\:mm"], CultureInfo.InvariantCulture, out var span))
        {
            hour = Math.Clamp(span.Hours, 0, 23);
            minute = Math.Clamp(span.Minutes, 0, 59);
            return true;
        }
        return false;
    }

    private static void Hint(string text)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(i)");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(text);
    }
}
