using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;

namespace StatusShift.Windows;

public partial class MainWindow
{
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
            if (!rule.TerritoryNameContains.Exists(z => z.Equals(add, StringComparison.OrdinalIgnoreCase)))
                rule.TerritoryNameContains.Add(add);
            zoneCustom = string.Empty;
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("Add current zone"))
        {
            var here = plugin.Snapshot().TerritoryName;
            if (!string.IsNullOrWhiteSpace(here) && !rule.TerritoryNameContains.Exists(z => z.Equals(here, StringComparison.OrdinalIgnoreCase)))
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
                if (rule.WorldNames.Exists(n => n.Equals(wname, StringComparison.OrdinalIgnoreCase))) continue;
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
            if (!string.IsNullOrEmpty(snap.WorldName) && !rule.WorldNames.Exists(n => n.Equals(snap.WorldName, StringComparison.OrdinalIgnoreCase)))
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
}
