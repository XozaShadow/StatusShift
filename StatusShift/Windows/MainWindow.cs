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
    private string ruleFilter = string.Empty;
    private string jobSearch = string.Empty;
    private string worldSearch = string.Empty;
    private string zoneCustom = string.Empty;
    private string selectedFolder = "All";
    private string? selectedRuleId;
    private string? contextRuleId;
    private bool editorOpen;

    private static readonly DayOfWeek[] Week =
    [
        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday,
    ];
    private static readonly string[] DayLetters = ["S", "M", "T", "W", "T", "F", "S"];
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
        ActivityFlag.Fishing, ActivityFlag.Performing, ActivityFlag.InSanctuary,
        ActivityFlag.Carrying, ActivityFlag.UsingHousing, ActivityFlag.FashionAccessory,
    ];

    public MainWindow(Plugin plugin) : base($"Status Shift v{Plugin.AppVersion}###StatusShiftMain")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(720, 520),
            MaximumSize = new Vector2(1600, 1400),
        };
    }

    public void Dispose() { }

    public void OpenRule(string id)
    {
        selectedRuleId = id;
        editorOpen = true;
        IsOpen = true;
    }

    public override void Draw()
    {
        WindowName = $"Status Shift v{Plugin.AppVersion}###StatusShiftMain";
        var cfg = plugin.Configuration;
        DrawPinned(cfg);

        ImGui.BeginChild("body", new Vector2(0, 0), false);
        DrawToolbar(cfg);

        var avail = ImGui.GetContentRegionAvail();
        var listH = editorOpen ? Math.Max(180f, avail.Y * 0.38f) : avail.Y;

        ImGui.BeginChild("listpane", new Vector2(0, listH), false);
        DrawLists(cfg);
        ImGui.EndChild();

        if (editorOpen)
        {
            ImGui.BeginChild("editor", new Vector2(0, 0), true);
            StatusRule? remove = null;
            var selectedRule = cfg.Rules.Find(r => r.Id == selectedRuleId);
            if (selectedRule is not null)
            {
                ImGui.PushID(selectedRule.Id);
                DrawRule(cfg, selectedRule, ref remove);
                ImGui.PopID();
            }
            else editorOpen = false;
            if (remove is not null)
            {
                cfg.Rules.Remove(remove);
                selectedRuleId = null;
                editorOpen = false;
                cfg.Save();
            }
            ImGui.EndChild();
        }
        ImGui.EndChild();
    }

    private void DrawPinned(Configuration cfg)
    {
        var enabled = cfg.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled)) { cfg.Enabled = enabled; cfg.Save(); plugin.RequestEval(); }
        ImGui.SameLine();
        if (ImGui.Button("Check Now")) plugin.TryApply(force: true);
        ImGui.SameLine();
        if (ImGui.Button("Settings")) plugin.ToggleConfigUi();
        ImGui.SameLine();
        ImGui.TextColored(UiTheme.Teal, $"{ApplyModeNames.Label(cfg.ApplyMode)}  {plugin.StatusLine()}");

        if (cfg.ShowSnapshot)
        {
            var snap = plugin.Snapshot();
            ImGui.TextColored(UiTheme.Teal, $"{snap.JobAbbr}  |  {snap.WorldName} · {snap.TerritoryName} · {snap.Housing.Summary}");
        }

        var matches = plugin.CurrentMatches();
        if (matches.Count == 0)
            ImGui.TextColored(UiTheme.Amber, "Current match: none");
        else
        {
            ImGui.TextUnformatted("Current match:");
            for (var i = 0; i < matches.Count && i < 6; i++)
            {
                var rule = matches[i];
                ImGui.SameLine();
                ImGui.PushID("m" + rule.Id);
                var on = rule.Enabled;
                if (ImGui.Checkbox("##mon", ref on))
                {
                    rule.Enabled = on;
                    cfg.Save();
                    plugin.RequestEval();
                }
                ImGui.SameLine();
                var label = i == 0
                    ? $"P{rule.Priority} {rule.Name} {StatusShort(rule)}"
                    : $"| P{rule.Priority} {rule.Name}";
                if (ImGui.SmallButton(label))
                    OpenRule(rule.Id);
                ImGui.PopID();
            }
        }
        ImGui.Separator();
    }

    private void DrawToolbar(Configuration cfg)
    {
        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##newname", "New rule name", ref newRuleName, 64);
        ImGui.SameLine();
        if (ImGui.Button("Add Rule"))
        {
            var nextPrio = cfg.Rules.Count == 0 ? 10 : cfg.Rules.Max(r => r.Priority) + 10;
            var rule = new StatusRule
            {
                Name = string.IsNullOrWhiteSpace(newRuleName) ? "New rule" : newRuleName.Trim(),
                Priority = nextPrio,
                Enabled = false,
                Folder = selectedFolder is "All" or "Ungrouped" || selectedFolder.StartsWith("char:") ? string.Empty : selectedFolder,
            };
            cfg.Rules.Add(rule);
            OpenRule(rule.Id);
            newRuleName = string.Empty;
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Import Rule"))
        {
            var clip = ImGui.GetClipboardText() ?? string.Empty;
            importMsg = plugin.TryImportOneRule(clip, out var err) ? "Imported." : err;
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180);
        ImGui.InputTextWithHint("##filter", "Filter / Search Rules", ref ruleFilter, 64);
        if (!string.IsNullOrEmpty(importMsg))
        {
            ImGui.SameLine();
            ImGui.TextDisabled(importMsg);
        }
    }

    private void DrawLists(Configuration cfg)
    {
        var folders = cfg.Rules.Select(r => r.FolderKey).Where(f => f.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f => f).ToList();
        var characters = cfg.Rules.Select(r => r.CharacterKey).Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList();
        ImGui.BeginChild("folders", new Vector2(150, 0), true);
        if (ImGui.Selectable("All", selectedFolder == "All")) selectedFolder = "All";
        if (ImGui.Selectable("Ungrouped", selectedFolder == "Ungrouped")) selectedFolder = "Ungrouped";
        if (folders.Count > 0)
        {
            ImGui.Separator();
            ImGui.TextDisabled("Categories");
        }
        foreach (var folder in folders)
        {
            if (ImGui.Selectable(folder, selectedFolder.Equals(folder, StringComparison.OrdinalIgnoreCase)))
                selectedFolder = folder;
        }
        if (characters.Count > 0)
        {
            ImGui.Separator();
            ImGui.TextDisabled("Characters");
            foreach (var name in characters)
            {
                var key = "char:" + name;
                if (ImGui.Selectable(name, selectedFolder.Equals(key, StringComparison.OrdinalIgnoreCase)))
                    selectedFolder = key;
            }
        }
        ImGui.EndChild();
        ImGui.SameLine();

        StatusRule? remove = null;
        var visible = cfg.Rules.Where(FolderVisible).OrderByDescending(r => r.Priority).ToList();
        var match = plugin.CurrentRule();
        ImGui.BeginChild("rulelist", new Vector2(0, 0), true);
        if (ImGui.BeginTable("rules", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY | ImGuiTableFlags.PadOuterX))
        {
            ImGui.TableSetupColumn("On", ImGuiTableColumnFlags.WidthFixed, 36);
            ImGui.TableSetupColumn("P", ImGuiTableColumnFlags.WidthFixed, 28);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 168);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 168);
            ImGui.TableSetupColumn("Notes", ImGuiTableColumnFlags.WidthStretch);
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
                var selected = selectedRuleId == rule.Id;
                if (ImGui.Selectable(rule.Name, selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
                {
                    selectedRuleId = rule.Id;
                    editorOpen = true;
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    selectedRuleId = rule.Id;
                    contextRuleId = rule.Id;
                    ImGui.OpenPopup("rulectx");
                }
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(StatusShort(rule));
                ImGui.TableNextColumn();
                ImGui.TextDisabled(rule.Notes);
                DrawContext(cfg, rule, match, ref remove);
                ImGui.PopID();
            }
            ImGui.EndTable();
        }
        ImGui.EndChild();
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
        if (ImGui.MenuItem("Edit")) editorOpen = true;
        if (ImGui.MenuItem("Move up")) plugin.MovePriority(rule, 1);
        if (ImGui.MenuItem("Move down")) plugin.MovePriority(rule, -1);
        if (ImGui.MenuItem("Duplicate")) plugin.DuplicateRule(rule);
        if (ImGui.MenuItem("Copy JSON"))
        {
            ImGui.SetClipboardText(plugin.ExportRuleJson(rule));
            importMsg = $"Copied {rule.Name}.";
        }
        if (ImGui.MenuItem("Copy share code"))
        {
            ImGui.SetClipboardText(ChipShare.Encode(rule));
            importMsg = "Share code copied.";
        }
        if (ImGui.MenuItem("Apply") && match?.Id == rule.Id)
            plugin.TryApply(rule, force: true);
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
        if (!string.IsNullOrWhiteSpace(ruleFilter))
        {
            var q = ruleFilter.Trim();
            if (!rule.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                && !(rule.Notes ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase)
                && !(rule.Folder ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        if (selectedFolder == "All") return true;
        if (selectedFolder == "Ungrouped") return string.IsNullOrWhiteSpace(rule.Folder);
        if (selectedFolder.StartsWith("char:", StringComparison.OrdinalIgnoreCase))
            return rule.CharacterKey.Equals(selectedFolder[5..], StringComparison.OrdinalIgnoreCase);
        return rule.FolderKey.Equals(selectedFolder, StringComparison.OrdinalIgnoreCase);
    }

    private static string StatusShort(StatusRule rule) =>
        rule.OnlineStatus == OnlineStatusAction.LeaveAlone ? "-" : ChatSender.StatusLabels[(int)rule.OnlineStatus];

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
