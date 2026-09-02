using Dalamud.Bindings.ImGui;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private void DrawRuleMeta(Configuration cfg, StatusRule rule)
    {
        if (ImGui.BeginTable("hdr", 4, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableNextColumn();
            var on = rule.Enabled;
            if (ImGui.Checkbox("On", ref on)) { rule.Enabled = on; cfg.Save(); plugin.RequestEval(); }
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(70);
            var prio = rule.Priority;
            if (ImGui.InputInt("Prio", ref prio)) { rule.Priority = prio; cfg.Save(); }
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var name = rule.Name;
            if (ImGui.InputText("Name", ref name, 64)) { rule.Name = name; cfg.Save(); }
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var notes = rule.Notes ?? string.Empty;
            if (ImGui.InputText("Notes", ref notes, 96)) { rule.Notes = notes; cfg.Save(); }
            ImGui.EndTable();
        }

        var folder = rule.Folder ?? string.Empty;
        ImGui.SetNextItemWidth(160);
        if (ImGui.InputText("Folder", ref folder, 48))
        {
            rule.Folder = folder.Trim();
            cfg.Save();
        }
        Hint("Left-list group. Blank = Ungrouped.");
        ImGui.SameLine();
        var character = rule.CharacterFilter ?? string.Empty;
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("Character", ref character, 64))
        {
            rule.CharacterFilter = character.Trim();
            cfg.Save();
        }
        Hint("Blank = all. First Last or First Last@World.");
    }
}
