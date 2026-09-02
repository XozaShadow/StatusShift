using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace StatusShift.Windows;

public partial class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string newRuleName = string.Empty;
    private string importMsg = string.Empty;
    private string jobSearch = string.Empty;
    private string worldSearch = string.Empty;
    private string zoneCustom = string.Empty;
    private string selectedFolder = "All";
    private string? selectedRuleId;
    private string? contextRuleId;
    private bool editorOpen;

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

    public MainWindow(Plugin plugin) : base("Status Shift v0.1.3.1###StatusShiftMain")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(720, 460),
            MaximumSize = new Vector2(1100, 1100),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        var enabled = cfg.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled)) { cfg.Enabled = enabled; cfg.Save(); plugin.RequestEval(); }
        ImGui.SameLine();
        if (ImGui.Button("Update Now")) plugin.TryApply(force: true);
        Hint("Apply the current match now, ignoring Confirm.");
        ImGui.SameLine();
        if (ImGui.Button("Settings")) plugin.ToggleConfigUi();
        Hint("Apply mode, timers, skip-while, import.");
        ImGui.SameLine();
        ImGui.TextColored(UiTheme.Teal, plugin.StatusLine());
        Hint("Auto applies on state change. Confirm only notifies. /ss pause 120 for a timed pause.");

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
        ImGui.TextDisabled(plugin.ExplainMatch());

        ImGui.Separator();
        ImGui.SetNextItemWidth(180);
        ImGui.InputTextWithHint("##newname", "New rule name", ref newRuleName, 64);
        ImGui.SameLine();
        if (ImGui.Button("Add rule"))
        {
            var nextPrio = cfg.Rules.Count == 0 ? 10 : cfg.Rules.Max(r => r.Priority) + 10;
            var rule = new StatusRule
            {
                Name = string.IsNullOrWhiteSpace(newRuleName) ? "New rule" : newRuleName.Trim(),
                Priority = nextPrio,
                Enabled = false,
                Folder = selectedFolder is "All" or "Ungrouped" ? string.Empty : selectedFolder,
            };
            cfg.Rules.Add(rule);
            selectedRuleId = rule.Id;
            editorOpen = true;
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

        var folders = cfg.Rules.Select(r => r.FolderKey).Where(f => f.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f => f).ToList();
        ImGui.BeginChild("folders", new Vector2(160, 260), true);
        if (ImGui.Selectable("All", selectedFolder == "All")) selectedFolder = "All";
        if (ImGui.Selectable("Ungrouped", selectedFolder == "Ungrouped")) selectedFolder = "Ungrouped";
        foreach (var folder in folders)
        {
            if (ImGui.Selectable(folder, selectedFolder.Equals(folder, StringComparison.OrdinalIgnoreCase)))
                selectedFolder = folder;
        }
        ImGui.EndChild();
        ImGui.SameLine();

        StatusRule? remove = null;
        var visible = cfg.Rules.Where(r => FolderVisible(r)).OrderByDescending(r => r.Priority).ToList();
        ImGui.BeginChild("rulelist", new Vector2(0, 260), true);
        if (ImGui.BeginTable("rules", 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("On", ImGuiTableColumnFlags.WidthFixed, 36);
            ImGui.TableSetupColumn("P", ImGuiTableColumnFlags.WidthFixed, 36);
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Status");
            ImGui.TableSetupColumn("Notes");
            ImGui.TableSetupColumn("If");
            ImGui.TableHeadersRow();
            foreach (var rule in visible)
            {
                ImGui.PushID(rule.Id);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var on = rule.Enabled;
                if (ImGui.Checkbox("##on", ref on)) { rule.Enabled = on; cfg.Save(); plugin.RequestEval(); }
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(rule.Priority.ToString());
                ImGui.TableNextColumn();
                var label = rule.Name;
                if (rule.HasCharacterFilter) label += $"  {rule.CharacterFilter.Trim()}";
                var selected = selectedRuleId == rule.Id;
                if (ImGui.Selectable(label, selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
                {
                    selectedRuleId = rule.Id;
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        editorOpen = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    selectedRuleId = rule.Id;
                    contextRuleId = rule.Id;
                    ImGui.OpenPopup("rulectx");
                }
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(rule.OnlineStatus == OnlineStatusAction.LeaveAlone ? "-" : ChatSender.StatusLabels[(int)rule.OnlineStatus]);
                ImGui.TableNextColumn();
                ImGui.TextDisabled(rule.Notes);
                ImGui.TableNextColumn();
                ImGui.TextDisabled(HeaderChips(rule));
                DrawContext(cfg, rule, match, ref remove);
                ImGui.PopID();
            }
            ImGui.EndTable();
        }
        ImGui.EndChild();

        var selectedRule = cfg.Rules.Find(r => r.Id == selectedRuleId);
        if (selectedRule is not null && editorOpen)
        {
            ImGui.Separator();
            ImGui.TextColored(UiTheme.Amber, $"Edit  P{selectedRule.Priority}  {selectedRule.Name}");
            ImGui.PushID(selectedRule.Id);
            DrawRule(cfg, selectedRule, ref remove);
            ImGui.PopID();
        }

        if (remove is not null)
        {
            cfg.Rules.Remove(remove);
            if (selectedRuleId == remove.Id) { selectedRuleId = null; editorOpen = false; }
            cfg.Save();
        }
    }

    private void DrawContext(Configuration cfg, StatusRule rule, StatusRule? match, ref StatusRule? remove)
    {
        if (!ImGui.BeginPopup("rulectx")) return;
        if (ImGui.MenuItem(rule.Enabled ? "Turn off" : "Turn on"))
        {
            rule.Enabled = !rule.Enabled;
            cfg.Save();
            plugin.RequestEval();
        }
        if (ImGui.MenuItem(rule.Sticky ? "Unsticky" : "Sticky"))
        {
            rule.Sticky = !rule.Sticky;
            cfg.Save();
        }
        if (ImGui.MenuItem("Edit")) editorOpen = true;
        if (ImGui.MenuItem("Apply if matching") && match?.Id == rule.Id)
            plugin.TryApply(rule, force: true);
        if (ImGui.MenuItem("Copy rule"))
        {
            ImGui.SetClipboardText(plugin.ExportRuleJson(rule));
            importMsg = $"Copied {rule.Name}.";
        }
        var io = ImGui.GetIO();
        if (io.KeyShift || io.KeyCtrl)
        {
            if (ImGui.MenuItem("Delete")) remove = rule;
        }
        else ImGui.MenuItem("Delete (hold Shift)", false);
        ImGui.EndPopup();
    }

    private bool FolderVisible(StatusRule rule)
    {
        if (selectedFolder == "All") return true;
        if (selectedFolder == "Ungrouped") return string.IsNullOrWhiteSpace(rule.Folder);
        return rule.FolderKey.Equals(selectedFolder, StringComparison.OrdinalIgnoreCase);
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
        if (rule.HasCharacterFilter) parts.Add("Char");
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
