using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private int chipKindUi;
    private string chipValue = string.Empty;
    private string chipSearch = string.Empty;
    private string shareMsg = string.Empty;
    private int houseType;
    private int houseZone;
    private int houseWard;
    private int housePlot;
    private int houseApt;
    private static List<string>? worldOptions;
    private static List<string>? jobOptions;
    private static List<string>? zoneOptions;
    private static List<string>? zoneTypeOptions;
    private static List<string>? dcOptions;

    private static readonly (string Label, ChipKind Kind)[] ChipKindChoices =
    [
        ("Accessory", ChipKind.Accessory),
        ("Chat", ChipKind.Chat),
        ("Contains", ChipKind.Contains),
        ("Data center", ChipKind.DataCenter),
        ("Duty", ChipKind.Duty),
        ("Emote", ChipKind.Emote),
        ("Housing", ChipKind.Property),
        ("Job", ChipKind.Job),
        ("Mount", ChipKind.Mount),
        ("Nearby player", ChipKind.NearbyPlayer),
        ("Regex", ChipKind.Regex),
        ("Region", ChipKind.Region),
        ("State", ChipKind.State),
        ("Status", ChipKind.Status),
        ("Tell from", ChipKind.TellFrom),
        ("World", ChipKind.World),
        ("Zone", ChipKind.Zone),
        ("Zone type", ChipKind.ZoneType),
    ];

    private static readonly string[] ChipKindLabels = ChipKindChoices.Select(c => c.Label).ToArray();

    private static readonly string[] StateNames =
    [
        "BetweenAreas", "BoundByDuty", "Carrying", "Casting", "Crafting", "Dead", "Diving",
        "FashionAccessory", "Fishing", "Flying", "Gathering", "HelmShown", "InCombat", "InDuty",
        "InParty", "InResidence", "InSanctuary", "Jumping", "Mounted", "Occupied", "PartyLeader",
        "Performing", "PvP", "Roleplaying", "Sitting", "Swimming", "TargetedByPlayer",
        "TargetingEnemy", "TargetingPlayer", "Trading", "UsingHousing", "WaitingForDutyFinder",
        "Walking", "WatchingCutscene", "WeaponDrawn", "WeaponShown",
    ];

    private static readonly string[] HouseTypes = ["Property", "Apartment", "FC Apartment"];

    private ChipKind SelectedChipKind => ChipKindChoices[Math.Clamp(chipKindUi, 0, ChipKindChoices.Length - 1)].Kind;

    private void DrawChips(Configuration cfg, StatusRule rule)
    {
        ImGui.SetNextItemWidth(140);
        ImGui.Combo("##chipkind", ref chipKindUi, ChipKindLabels, ChipKindLabels.Length);
        var kind = SelectedChipKind;
        if (kind == ChipKind.Property)
        {
            DrawHousingBuilder(cfg, rule);
        }
        else
        {
            ImGui.SameLine();
            DrawChipPicker();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(140);
            ImGui.InputTextWithHint("##chipval", FieldHint(), ref chipValue, 64);
            ImGui.SameLine();
            if (ImGui.Button("+ Current")) FillCurrent();
            ImGui.SameLine();
            if (ImGui.Button("+AND")) AddChip(cfg, rule, 0);
            ImGui.SameLine();
            if (ImGui.Button("+OR")) AddChip(cfg, rule, 1);
            ImGui.SameLine();
            if (ImGui.Button("+NOT")) AddChip(cfg, rule, 2);
        }

        DrawChipGroup("AND:", cfg, rule.AndChips);
        DrawChipGroup("OR:", cfg, rule.OrChips);
        DrawChipGroup("NOT:", cfg, rule.NotChips);
        var why = plugin.ExplainRuleLine(rule);
        if (!string.IsNullOrWhiteSpace(why)
            && !why.Equals("Off.", StringComparison.OrdinalIgnoreCase))
            ImGui.TextDisabled(why);
        if (!string.IsNullOrEmpty(shareMsg))
            ImGui.TextDisabled(shareMsg);
    }

    private void DrawHousingBuilder(Configuration cfg, StatusRule rule)
    {
        if (ImGui.RadioButton("Property", houseType == 0)) houseType = 0;
        ImGui.SameLine();
        if (ImGui.RadioButton("Apartment", houseType == 1)) houseType = 1;
        ImGui.SameLine();
        if (ImGui.RadioButton("FC Apartment", houseType == 2)) houseType = 2;

        ImGui.SetNextItemWidth(220);
        ImGui.Combo("Zone", ref houseZone, HousingZones.Choices, HousingZones.Choices.Length);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(60);
        ImGui.InputInt("Ward", ref houseWard);
        houseWard = Math.Clamp(houseWard, 0, 30);

        if (houseType == 0 || houseType == 2)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60);
            ImGui.InputInt("Plot", ref housePlot);
            housePlot = Math.Clamp(housePlot, 0, 60);
        }
        if (houseType == 1 || houseType == 2)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(70);
            var aptLabel = houseType == 2 ? "Room" : "Apt #";
            ImGui.InputInt(aptLabel, ref houseApt);
            houseApt = Math.Max(0, houseApt);
        }

        if (ImGui.Button("Use current"))
        {
            var here = plugin.Snapshot().Housing;
            houseType = here.IsFcApartment ? 2 : here.Kind == ResidenceKind.Apartment ? 1 : 0;
            var zoneName = here.District + (here.Subdivision ? " (Subdivision)" : "");
            var zi = Array.FindIndex(HousingZones.Choices, z => z.Equals(zoneName, StringComparison.OrdinalIgnoreCase));
            if (zi < 0) zi = Array.FindIndex(HousingZones.Choices, z => z.StartsWith(here.District, StringComparison.OrdinalIgnoreCase));
            if (zi >= 0) houseZone = zi;
            houseWard = here.Ward;
            housePlot = here.Plot;
            houseApt = here.Apartment;
        }
        ImGui.SameLine();
        if (ImGui.Button("+AND")) AddHousingChip(cfg, rule, 0);
        ImGui.SameLine();
        if (ImGui.Button("+OR")) AddHousingChip(cfg, rule, 1);
        ImGui.SameLine();
        if (ImGui.Button("+NOT")) AddHousingChip(cfg, rule, 2);
        ImGui.TextDisabled("0 ward / plot / room = any of that field.");
    }

    private void AddHousingChip(Configuration cfg, StatusRule rule, int row)
    {
        var type = HouseTypes[Math.Clamp(houseType, 0, 2)];
        if (type == "Property") type = "House";
        var zone = HousingZones.Choices[Math.Clamp(houseZone, 0, HousingZones.Choices.Length - 1)];
        var value = HousingChip.Encode(type == "House" ? "House" : type, zone, houseWard, housePlot, houseApt);
        var list = row switch { 1 => rule.OrChips, 2 => rule.NotChips, _ => rule.AndChips };
        list.Add(new RuleChip { Kind = ChipKind.Property, Value = value });
        cfg.Save();
        plugin.RequestEval();
    }

    private void DrawChipPicker()
    {
        var kind = SelectedChipKind;
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
        var needLetters = NeedsThreeLetters(kind, options.Count);
        ImGui.InputTextWithHint("##chipsearch", needLetters ? "type 3+ letters" : "Search", ref chipSearch, 32);
        var q = chipSearch.Trim();
        foreach (var item in options)
        {
            if (needLetters && q.Length < 3 && !item.StartsWith("Current:", StringComparison.OrdinalIgnoreCase))
                continue;
            if (q.Length > 0 && !item.Contains(q, StringComparison.OrdinalIgnoreCase))
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

    private static bool NeedsThreeLetters(ChipKind kind, int count) =>
        kind is ChipKind.Zone or ChipKind.Status or ChipKind.Emote or ChipKind.Mount || count > 80;

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
                list.AddRange(JobRoles.Names);
                list.AddRange(JobOptions());
                break;
            case ChipKind.Role:
                list.AddRange(JobRoles.Names);
                break;
            case ChipKind.State:
                list.AddRange(StateNames);
                break;
            case ChipKind.DataCenter:
                list.AddRange(DcOptions());
                break;
            case ChipKind.Zone:
                list.AddRange(ZoneOptions());
                break;
            case ChipKind.ZoneType:
                list.AddRange(ZoneTypeOptions());
                break;
            case ChipKind.NearbyPlayer:
                list.AddRange(LiveLook.Capture(plugin.Configuration.NearbyRange).NearbyPlayers);
                break;
            case ChipKind.Chat:
                list.AddRange(ChatWatch.Channels);
                break;
            case ChipKind.Duty:
                list.Add("any");
                break;
            case ChipKind.Status:
                list.AddRange(LiveLook.Capture(plugin.Configuration.NearbyRange).Statuses);
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

    private static List<string> ZoneOptions()
    {
        if (zoneOptions is not null) return zoneOptions;
        zoneOptions = [];
        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        if (sheet is null) return zoneOptions;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in sheet)
        {
            var name = row.PlaceName.Value.Name.ToString();
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name)) continue;
            zoneOptions.Add(name);
        }
        zoneOptions.Sort(StringComparer.OrdinalIgnoreCase);
        return zoneOptions;
    }

    private static List<string> ZoneTypeOptions()
    {
        if (zoneTypeOptions is not null) return zoneTypeOptions;
        zoneTypeOptions = [];
        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        if (sheet is null) return zoneTypeOptions;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in sheet)
        {
            var name = row.PlaceNameZone.Value.Name.ToString();
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name)) continue;
            zoneTypeOptions.Add(name);
        }
        zoneTypeOptions.Sort(StringComparer.OrdinalIgnoreCase);
        return zoneTypeOptions;
    }

    private static List<string> DcOptions()
    {
        if (dcOptions is not null) return dcOptions;
        dcOptions = [];
        try
        {
            var sheet = Plugin.DataManager.GetExcelSheet<WorldDCGroupType>();
            if (sheet is not null)
            {
                foreach (var row in sheet)
                {
                    var name = row.Name.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                        dcOptions.Add(name);
                }
            }
        }
        catch { /* sheet name may differ */ }
        dcOptions.Sort(StringComparer.OrdinalIgnoreCase);
        return dcOptions;
    }

    private void DrawChipGroup(string title, Configuration cfg, List<RuleChip> chips)
    {
        ImGui.TextDisabled(title);
        ImGui.SameLine();
        chips ??= [];
        if (chips.Count == 0)
        {
            ImGui.TextDisabled("(none)");
            return;
        }

        var wrap = ImGui.GetContentRegionAvail().X;
        var used = ImGui.CalcTextSize(title).X + 8;
        for (var i = 0; i < chips.Count; i++)
        {
            var chip = chips[i];
            var label = chip.Label + "  x";
            var need = ImGui.CalcTextSize(label).X + 18;
            if (i > 0 && used + need < wrap) ImGui.SameLine();
            else
            {
                ImGui.Dummy(new System.Numerics.Vector2(18, 0));
                ImGui.SameLine();
                used = 18;
            }
            used += need;
            ImGui.PushID(title + i + chip.Kind + chip.Value);
            if (ImGui.SmallButton(label))
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
            ChipKind.ZoneType => snap.ZoneTypeName,
            ChipKind.Residence => snap.Housing.Summary,
            ChipKind.Apartment => snap.Housing.Summary,
            ChipKind.Property => snap.Housing.Summary,
            ChipKind.Duty => snap.TerritoryName,
            ChipKind.Job => snap.JobAbbr,
            ChipKind.Role => snap.JobAbbr,
            ChipKind.Mount => look.MountName,
            ChipKind.Emote => look.EmoteName,
            ChipKind.Accessory => look.AccessoryName,
            ChipKind.NearbyPlayer => look.NearbyPlayers.Count > 0 ? look.NearbyPlayers[0] : string.Empty,
            ChipKind.State => snap.Activities.Count > 0 ? snap.Activities.First().ToString() : string.Empty,
            ChipKind.DataCenter => snap.DataCenterName,
            ChipKind.TellFrom => ChatWatch.LastTellFrom,
            ChipKind.Chat => ChatWatch.LastChatChannel,
            ChipKind.Status => look.Statuses.Count > 0 ? look.Statuses[0] : string.Empty,
            _ => snap.TerritoryName,
        };
    }

    private void FillCurrent() => chipValue = CurrentValue(SelectedChipKind);

    private void AddChip(Configuration cfg, StatusRule rule, int row)
    {
        var kind = SelectedChipKind;
        var value = chipValue.Trim();
        if (value.StartsWith("Current:", StringComparison.OrdinalIgnoreCase))
            value = value[8..].Trim();
        if (value.Length == 0 && kind is not ChipKind.Duty) return;
        var list = row switch
        {
            1 => rule.OrChips,
            2 => rule.NotChips,
            _ => rule.AndChips,
        };
        list.Add(new RuleChip { Kind = kind, Value = value.Length == 0 ? "any" : value });
        chipValue = string.Empty;
        cfg.Save();
        plugin.RequestEval();
    }

    private string FieldHint() => SelectedChipKind switch
    {
        ChipKind.World => "or type a world",
        ChipKind.Job => "job or role",
        ChipKind.NearbyPlayer => "First Last or First Last@World",
        ChipKind.TellFrom => "First Last or First Last@World",
        ChipKind.Chat => "channel or channel|text",
        ChipKind.Emote => "emote name contains",
        ChipKind.Mount => "mount name contains",
        ChipKind.Accessory => "accessory name",
        ChipKind.Regex => "regex pattern",
        ChipKind.DataCenter => "data center name",
        ChipKind.State => "or pick a state",
        ChipKind.Status => "status effect name",
        ChipKind.ZoneType => "or pick a zone type",
        _ => "contains / custom",
    };
}
