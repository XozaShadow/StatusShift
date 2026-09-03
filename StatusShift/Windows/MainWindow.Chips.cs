using System;
using Dalamud.Bindings.ImGui;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private int chipKind;
    private string chipValue = string.Empty;
    private bool chipToAnd = true;
    private string shareMsg = string.Empty;

    private static readonly string[] ChipKinds =
    [
        "World", "Zone", "Region", "Zone type", "Residence", "Apartment", "Duty",
        "Job", "Nearby player", "Emote", "Mount", "State", "Contains", "Regex",
    ];

    private void DrawChips(Configuration cfg, StatusRule rule)
    {
        UiTheme.Section("AND chips");
        Hint("All of these must match. Worlds belong here.");
        DrawChipRow(cfg, rule.AndChips);

        UiTheme.Section("OR chips");
        Hint("Any one of these may match. Zones, jobs, nearby, emotes, mounts.");
        DrawChipRow(cfg, rule.OrChips);

        ImGui.SetNextItemWidth(140);
        ImGui.Combo("##chipkind", ref chipKind, ChipKinds, ChipKinds.Length);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180);
        ImGui.InputTextWithHint("##chipval", FieldHint(), ref chipValue, 64);
        ImGui.SameLine();
        if (ImGui.Button("+ AND")) AddChip(cfg, rule, true);
        ImGui.SameLine();
        if (ImGui.Button("+ OR")) AddChip(cfg, rule, false);

        if (ImGui.Button("+ Zone")) AddCurrent(cfg, rule, ChipKind.Zone, false);
        ImGui.SameLine();
        if (ImGui.Button("+ Type")) AddCurrent(cfg, rule, ChipKind.ZoneType, false);
        ImGui.SameLine();
        if (ImGui.Button("+ Job")) AddCurrent(cfg, rule, ChipKind.Job, false);
        ImGui.SameLine();
        if (ImGui.Button("+ Full")) AddFull(cfg, rule);

        var snap = plugin.Snapshot();
        if (snap.InResidence)
        {
            ImGui.SameLine();
            if (ImGui.Button(snap.Housing.Kind == ResidenceKind.Apartment ? "+ Apartment" : "+ Residence"))
                AddCurrent(cfg, rule, snap.Housing.Kind == ResidenceKind.Apartment ? ChipKind.Apartment : ChipKind.Residence, false);
        }
        if (snap.Activities.Contains(ActivityFlag.InDuty))
        {
            ImGui.SameLine();
            if (ImGui.Button("+ Duty")) AddCurrent(cfg, rule, ChipKind.Duty, false);
        }
        if (snap.Activities.Contains(ActivityFlag.Mounted))
        {
            ImGui.SameLine();
            if (ImGui.Button("+ Mount")) AddCurrent(cfg, rule, ChipKind.Mount, false);
        }

        if (ImGui.Button("Copy share code"))
        {
            ImGui.SetClipboardText(ChipShare.Encode(rule));
            shareMsg = "Share code copied.";
        }
        Hint("One-line SS1. code. Paste with Import rule.");
        if (!string.IsNullOrEmpty(shareMsg))
            ImGui.TextDisabled(shareMsg);
    }

    private void DrawChipRow(Configuration cfg, System.Collections.Generic.List<RuleChip> chips)
    {
        chips ??= [];
        for (var i = 0; i < chips.Count; i++)
        {
            var chip = chips[i];
            if (i > 0) ImGui.SameLine();
            ImGui.PushID(i + chip.Kind + chip.Value);
            ImGui.TextUnformatted($"[{chip.Label}");
            ImGui.SameLine();
            if (ImGui.SmallButton("x"))
            {
                chips.RemoveAt(i);
                cfg.Save();
                ImGui.PopID();
                break;
            }
            ImGui.SameLine();
            ImGui.TextUnformatted("]");
            ImGui.PopID();
        }
        if (chips.Count == 0)
            ImGui.TextDisabled("(none)");
    }

    private void AddChip(Configuration cfg, StatusRule rule, bool and)
    {
        var value = chipValue.Trim();
        if (value.Length == 0 && (ChipKind)chipKind is not ChipKind.Duty) return;
        var list = and ? rule.AndChips : rule.OrChips;
        list.Add(new RuleChip { Kind = (ChipKind)chipKind, Value = value.Length == 0 ? "any" : value });
        chipValue = string.Empty;
        cfg.Save();
        plugin.RequestEval();
    }

    private void AddCurrent(Configuration cfg, StatusRule rule, ChipKind kind, bool and)
    {
        var snap = plugin.Snapshot();
        var look = LiveLook.Capture(80f);
        var value = kind switch
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
            _ => snap.TerritoryName,
        };
        if (string.IsNullOrWhiteSpace(value)) return;
        var list = and || kind == ChipKind.World ? rule.AndChips : rule.OrChips;
        list.Add(new RuleChip { Kind = kind, Value = value });
        cfg.Save();
        plugin.RequestEval();
    }

    private void AddFull(Configuration cfg, StatusRule rule)
    {
        var snap = plugin.Snapshot();
        if (!string.IsNullOrWhiteSpace(snap.WorldName))
            rule.AndChips.Add(new RuleChip { Kind = ChipKind.World, Value = snap.WorldName });
        if (snap.InResidence)
            rule.OrChips.Add(new RuleChip
            {
                Kind = snap.Housing.Kind == ResidenceKind.Apartment ? ChipKind.Apartment : ChipKind.Residence,
                Value = snap.Housing.Summary,
            });
        else if (snap.Activities.Contains(ActivityFlag.InDuty))
            rule.OrChips.Add(new RuleChip { Kind = ChipKind.Duty, Value = snap.TerritoryName });
        else if (!string.IsNullOrWhiteSpace(snap.TerritoryName))
            rule.OrChips.Add(new RuleChip { Kind = ChipKind.Zone, Value = snap.TerritoryName });
        cfg.Save();
        plugin.RequestEval();
    }

    private string FieldHint() => (ChipKind)chipKind switch
    {
        ChipKind.World => "World name",
        ChipKind.Job => "Job abbr",
        ChipKind.NearbyPlayer => "First Last or First Last@World",
        ChipKind.Emote => "emote name contains",
        ChipKind.Mount => "mount name contains",
        ChipKind.State => "InCombat / Walking / Sitting",
        ChipKind.Regex => "regex pattern",
        _ => "contains",
    };
}
