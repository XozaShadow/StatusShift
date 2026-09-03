using System;
using Dalamud.Bindings.ImGui;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private int chipKind;
    private int chipState;
    private string chipValue = string.Empty;
    private string shareMsg = string.Empty;

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
        if ((ChipKind)chipKind == ChipKind.State)
        {
            ImGui.SetNextItemWidth(160);
            ImGui.Combo("##chipstate", ref chipState, StateNames, StateNames.Length);
        }
        else
        {
            ImGui.SetNextItemWidth(160);
            ImGui.InputTextWithHint("##chipval", FieldHint(), ref chipValue, 64);
        }
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

    private void DrawChipRow(Configuration cfg, System.Collections.Generic.List<RuleChip> chips)
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

    private void FillCurrent()
    {
        var kind = (ChipKind)chipKind;
        if (kind == ChipKind.State)
        {
            chipValue = StateNames[Math.Clamp(chipState, 0, StateNames.Length - 1)];
            return;
        }
        var snap = plugin.Snapshot();
        var look = LiveLook.Capture(plugin.Configuration.NearbyRange);
        chipValue = kind switch
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
            _ => snap.TerritoryName,
        };
    }

    private void AddChip(Configuration cfg, StatusRule rule, bool and)
    {
        var kind = (ChipKind)chipKind;
        var value = kind == ChipKind.State
            ? StateNames[Math.Clamp(chipState, 0, StateNames.Length - 1)]
            : chipValue.Trim();
        if (value.Length == 0 && kind is not ChipKind.Duty) return;
        var list = and ? rule.AndChips : rule.OrChips;
        list.Add(new RuleChip { Kind = kind, Value = value.Length == 0 ? "any" : value });
        chipValue = string.Empty;
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
        ChipKind.Regex => "regex pattern",
        ChipKind.Residence => "ward / plot / district",
        ChipKind.Apartment => "apartment summary",
        _ => "contains",
    };
}
