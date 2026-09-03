using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private int chipKind;
    private string chipValue = string.Empty;
    private string chipSearch = string.Empty;
    private string shareMsg = string.Empty;
    private static List<string>? worldOptions;
    private static List<string>? jobOptions;

    private static readonly string[] ChipKinds =
    [
        "World", "Zone", "Region", "Zone type", "Residence", "Apartment", "Duty",
        "Job", "Nearby player", "Emote", "Mount", "State", "Contains", "Regex",
    ];

    private static readonly string[] StateNames =
    [
        "InDuty", "InCombat", "Crafting", "Gathering", "Mounted", "Flying", "Swimming",
        "WatchingCutscene", "Dead", "InParty", "BoundByDuty", "Diving", "WeaponDrawn",
        "WaitingForDutyFinder", "PvP", "PartyLeader", "InResidence", "Sitting", "Casting",
        "Jumping", "Occupied", "Trading", "BetweenAreas", "Roleplaying",
        "TargetingPlayer", "TargetingEnemy", "TargetedByPlayer", "HelmShown", "WeaponShown", "Walking",
    ];

    private void DrawChips(Configuration cfg, StatusRule rule)
    {
        ImGui.SetNextItemWidth(130);
        ImGui.Combo("##chipkind", ref chipKind, ChipKinds, ChipKinds.Length);
        ImGui.SameLine();
        DrawChipPicker();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(140);
        ImGui.InputTextWithHint("##chipval", FieldHint(), ref chipValue, 64);
        ImGui.SameLine();
        if (ImGui.Button("+ Current")) FillCurrent();
        ImGui.SameLine();
        if (ImGui.Button("+AND")) AddChip(cfg, rule, true);
        ImGui.SameLine();
        if (ImGui.Button("+OR")) AddChip(cfg, rule, false);

        ImGui.TextDisabled("AND");
        DrawChipRow(cfg, rule.AndChips);
        ImGui.TextDisabled("OR");
        DrawChipRow(cfg, rule.OrChips);
        if (!string.IsNullOrEmpty(shareMsg))
            ImGui.TextDisabled(shareMsg);
    }

    private void DrawChipPicker()
    {
        var kind = (ChipKind)chipKind;
        var options = OptionsFor(kind);
        ImGui.SetNextItemWidth(170);
        if (options.Count == 0)
        {
            ImGui.TextDisabled("type or + Current");
            return;
        }

        var preview = string.IsNullOrEmpty(chipValue) ? "(pick)" : chipValue;
        if (!ImGui.BeginCombo("##chipopt", preview))
            return;

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##chipsearch", "Search", ref chipSearch, 32);
        foreach (var item in options)
        {
            if (!string.IsNullOrWhiteSpace(chipSearch)
                && !item.Contains(chipSearch, StringComparison.OrdinalIgnoreCase))
                continue;
            if (ImGui.Selectable(item, item.Equals(chipValue, StringComparison.OrdinalIgnoreCase)))
            {
                if (item.StartsWith("Current:", StringComparison.OrdinalIgnoreCase))
                    FillCurrent();
                else
                    chipValue = item;
            }
        }
        ImGui.EndCombo();
    }

    private List<string> OptionsFor(ChipKind kind)
    {
        var current = CurrentValue(kind);
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(current))
            list.Add("Current: " + current);

        switch (kind)
        {
            case ChipKind.World:
                list.AddRange(WorldOptions());
                break;
            case ChipKind.Job:
                list.AddRange(JobOptions());
                break;
            case ChipKind.State:
                list.AddRange(StateNames);
                break;
            case ChipKind.NearbyPlayer:
                list.AddRange(LiveLook.Capture(plugin.Configuration.NearbyRange).NearbyPlayers);
                break;
            case ChipKind.Duty:
                list.Add("any");
                break;
        }
        return list;
    }

    private static List<string> WorldOptions()
    {
        if (worldOptions is not null) return worldOptions;
        worldOptions = [];
        var sheet = Plugin.DataManager.GetExcelSheet<World>();
        if (sheet is null) return worldOptions;
        foreach (var row in sheet)
        {
            if (row.RowId == 0) continue;
            var name = row.Name.ToString();
            if (string.IsNullOrWhiteSpace(name) || name.StartsWith("Dev", StringComparison.Ordinal)) continue;
            worldOptions.Add(name);
        }
        worldOptions.Sort(StringComparer.OrdinalIgnoreCase);
        return worldOptions;
    }

    private static List<string> JobOptions()
    {
        if (jobOptions is not null) return jobOptions;
        jobOptions = [];
        var sheet = Plugin.DataManager.GetExcelSheet<ClassJob>();
        if (sheet is null) return jobOptions;
        foreach (var row in sheet)
        {
            if (row.RowId == 0) continue;
            var abbr = row.Abbreviation.ToString();
            if (string.IsNullOrWhiteSpace(abbr) || abbr.Length > 4) continue;
            jobOptions.Add(abbr);
        }
        jobOptions.Sort(StringComparer.OrdinalIgnoreCase);
        return jobOptions;
    }

    private void DrawChipRow(Configuration cfg, List<RuleChip> chips)
    {
        chips ??= [];
        if (chips.Count == 0)
        {
            ImGui.TextDisabled("(none)");
            return;
        }

        var wrap = ImGui.GetContentRegionAvail().X;
        var used = 0f;
        for (var i = 0; i < chips.Count; i++)
        {
            var chip = chips[i];
            var label = chip.Label + " x";
            var need = ImGui.CalcTextSize(label).X + 18;
            if (i > 0 && used + need < wrap) ImGui.SameLine();
            else used = 0;
            used += need;
            ImGui.PushID(i + chip.Kind + chip.Value);
            if (ImGui.SmallButton(chip.Label + "  x"))
            {
                chips.RemoveAt(i);
                cfg.Save();
                plugin.RequestEval();
                ImGui.PopID();
                break;
            }
            ImGui.PopID();
        }
    }

    private string CurrentValue(ChipKind kind)
    {
        var snap = plugin.Snapshot();
        var look = LiveLook.Capture(plugin.Configuration.NearbyRange);
        return kind switch
        {
            ChipKind.World => snap.WorldName,
            ChipKind.Zone => snap.TerritoryName,
            ChipKind.Region => snap.RegionName,
            ChipKind.ZoneType => snap.ZoneGroupName,
            ChipKind.Residence => snap.Housing.Summary,
            ChipKind.Apartment => snap.Housing.Summary,
            ChipKind.Duty => snap.TerritoryName,
            ChipKind.Job => snap.JobAbbr,
            ChipKind.Mount => look.MountName,
            ChipKind.Emote => look.EmoteName,
            ChipKind.NearbyPlayer => look.NearbyPlayers.Count > 0 ? look.NearbyPlayers[0] : string.Empty,
            ChipKind.State => snap.Activities.Count > 0 ? snap.Activities.First().ToString() : string.Empty,
            _ => snap.TerritoryName,
        };
    }

    private void FillCurrent() => chipValue = CurrentValue((ChipKind)chipKind);

    private void AddChip(Configuration cfg, StatusRule rule, bool and)
    {
        var kind = (ChipKind)chipKind;
        var value = chipValue.Trim();
        if (value.StartsWith("Current:", StringComparison.OrdinalIgnoreCase))
            value = value[8..].Trim();
        if (value.Length == 0 && kind is not ChipKind.Duty) return;
        var list = and ? rule.AndChips : rule.OrChips;
        list.Add(new RuleChip { Kind = kind, Value = value.Length == 0 ? "any" : value });
        chipValue = string.Empty;
        cfg.Save();
        plugin.RequestEval();
    }

    private string FieldHint() => (ChipKind)chipKind switch
    {
        ChipKind.World => "or type a world",
        ChipKind.Job => "or type a job abbr",
        ChipKind.NearbyPlayer => "First Last or First Last@World",
        ChipKind.Emote => "emote name contains",
        ChipKind.Mount => "mount name contains",
        ChipKind.Regex => "regex pattern",
        ChipKind.Residence => "ward / plot / district",
        ChipKind.Apartment => "apartment summary",
        ChipKind.State => "or pick a state",
        _ => "contains / custom",
    };
}
