using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private void DrawJob(Configuration cfg, StatusRule rule)
    {
        ImGui.SetNextItemWidth(160);
        ImGui.InputTextWithHint("##jobsearch", "Search jobs...", ref jobSearch, 24);
        var sheet = Plugin.DataManager.GetExcelSheet<ClassJob>();
        if (sheet is not null && ImGui.BeginChild("jobavail", new Vector2(220, 110), true))
        {
            foreach (var row in sheet)
            {
                if (row.RowId == 0) continue;
                var abbr = row.Abbreviation.ToString();
                var jname = row.Name.ToString();
                if (string.IsNullOrWhiteSpace(abbr) || abbr.Length > 4) continue;
                if (!string.IsNullOrWhiteSpace(jobSearch)
                    && !abbr.Contains(jobSearch, StringComparison.OrdinalIgnoreCase)
                    && !jname.Contains(jobSearch, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (rule.JobAbbrs.Exists(a => a.Equals(abbr, StringComparison.OrdinalIgnoreCase))) continue;
                if (ImGui.Selectable($"{abbr}  {jname}"))
                {
                    rule.JobAbbrs.Add(abbr);
                    if (!rule.JobIds.Contains(row.RowId)) rule.JobIds.Add(row.RowId);
                    cfg.Save();
                }
            }
            ImGui.EndChild();
        }
        ImGui.SameLine();
        if (ImGui.BeginChild("jobsel", new Vector2(140, 110), true))
        {
            ImGui.TextDisabled("Included");
            foreach (var abbr in rule.JobAbbrs.ToList())
            {
                if (ImGui.Selectable(abbr))
                {
                    rule.JobAbbrs.RemoveAll(a => a.Equals(abbr, StringComparison.OrdinalIgnoreCase));
                    cfg.Save();
                }
            }
            ImGui.EndChild();
        }
        if (ImGui.Button("Add current job"))
        {
            var snap = plugin.Snapshot();
            if (!string.IsNullOrEmpty(snap.JobAbbr) && !rule.JobAbbrs.Exists(a => a.Equals(snap.JobAbbr, StringComparison.OrdinalIgnoreCase)))
                rule.JobAbbrs.Add(snap.JobAbbr);
            if (snap.JobId != 0 && !rule.JobIds.Contains(snap.JobId)) rule.JobIds.Add(snap.JobId);
            cfg.Save();
        }
        Hint("Empty list = any job. Click a selected abbr to remove.");
    }
}
