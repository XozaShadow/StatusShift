using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private void DrawRule(Configuration cfg, StatusRule rule, ref StatusRule? remove)
    {
        if (rule.HasLegacy)
        {
            ImGui.TextColored(new Vector4(1f, 0.35f, 0.28f, 1f), "Not compatible");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(rule.LegacySummary());
        }

        ImGui.TextColored(UiTheme.Amber, $"EDITING: P{rule.Priority}  {rule.Name}");
        ImGui.SameLine();
        ImGui.TextDisabled("Copy:");
        ImGui.SameLine();
        if (ImGui.SmallButton("JSON"))
        {
            ImGui.SetClipboardText(plugin.ExportRuleJson(rule));
            importMsg = "JSON copied.";
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("ShareCode"))
        {
            ImGui.SetClipboardText(ChipShare.Encode(rule));
            importMsg = "Share code copied.";
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("Duplicate"))
        {
            plugin.DuplicateRule(rule);
            var copy = cfg.Rules[^1];
            selectedRuleId = copy.Id;
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("X"))
        {
            editorOpen = false;
            selectedRuleId = null;
            return;
        }

        if (ImGui.BeginTable("edmeta", 4, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableNextColumn();
            var on = rule.Enabled;
            if (ImGui.Checkbox("On", ref on)) { rule.Enabled = on; cfg.Save(); plugin.RequestEval(); }
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var prio = rule.Priority;
            if (ImGui.InputInt("##prio", ref prio)) { rule.Priority = prio; cfg.Save(); }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Priority");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var name = rule.Name;
            if (ImGui.InputTextWithHint("##name", "Name", ref name, 64)) { rule.Name = name; cfg.Save(); }
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var folder = rule.Folder ?? string.Empty;
            if (ImGui.InputTextWithHint("##folder", "Category", ref folder, 48)) { rule.Folder = folder.Trim(); cfg.Save(); }
            ImGui.EndTable();
        }

        var notes = rule.Notes ?? string.Empty;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint("##notes", "Notes", ref notes, 120)) { rule.Notes = notes; cfg.Save(); }

        var ping = rule.NotifyIfNotApplied;
        if (ImGui.Checkbox("Notify if rule applies, but is not applied", ref ping))
        {
            rule.NotifyIfNotApplied = ping;
            cfg.Save();
        }

        ImGui.Separator();
        var character = rule.CharacterFilter ?? string.Empty;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint("##char", "Character  First Last or First Last@World  (blank = all)", ref character, 64))
        {
            rule.CharacterFilter = character.Trim();
            cfg.Save();
        }

        if (ImGui.TreeNodeEx("DURING SCHEDULE", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawSchedule(cfg, rule);
            ImGui.TreePop();
        }

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Teal, "IF THESE CONDITIONS");
        DrawChips(cfg, rule);

        if (rule.HasLegacy)
        {
            ImGui.Separator();
            if (ImGui.TreeNodeEx("Legacy Conditions", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PushTextWrapPos();
                ImGui.TextWrapped("This rule has legacy conditions that have been reworked. See the details below and apply them to the chip AND/OR options above. Once complete, Duplicate this rule — the copy has no legacy data — then delete this one.");
                ImGui.TextDisabled(rule.LegacySummary());
                ImGui.PopTextWrapPos();
                DrawLocation(cfg, rule);
                DrawJob(cfg, rule);
                DrawStates(cfg, rule);
                ImGui.TreePop();
            }
        }

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Amber, "THEN SET / RUN / UPDATE");
        DrawThen(cfg, rule, false);

        ImGui.Separator();
        var revert = rule.RevertWhenFalse;
        if (ImGui.Checkbox("UNTIL RULE ENDS REVERT TO", ref revert))
        {
            rule.RevertWhenFalse = revert;
            cfg.Save();
        }
        if (!rule.RevertWhenFalse)
        {
            var sticky = true;
            if (ImGui.Checkbox("AND REMAIN THIS WAY AFTER THE RULE ENDS", ref sticky) && !sticky)
            {
                rule.RevertWhenFalse = true;
                cfg.Save();
            }
        }
        else
            DrawThen(cfg, rule, true);

        var io = ImGui.GetIO();
        var canDelete = io.KeyShift || io.KeyCtrl;
        if (!canDelete) ImGui.BeginDisabled();
        if (ImGui.Button("Delete rule")) remove = rule;
        if (!canDelete) ImGui.EndDisabled();
        if (!canDelete)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("hold Shift");
        }
    }

    private void DrawThen(Configuration cfg, StatusRule rule, bool fallback)
    {
        var status = (int)(fallback ? rule.FallbackStatus : rule.OnlineStatus);
        ImGui.SetNextItemWidth(180);
        if (ImGui.Combo(fallback ? "##fbst" : "Status", ref status, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
        {
            if (fallback) rule.FallbackStatus = (OnlineStatusAction)status;
            else rule.OnlineStatus = (OnlineStatusAction)status;
            cfg.Save();
        }
        ImGui.SameLine();
        var cmd = fallback ? rule.FallbackCommand ?? string.Empty : rule.Command ?? string.Empty;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint(fallback ? "##fbcmd" : "##cmd", "Command / macro", ref cmd, 192))
        {
            if (fallback) rule.FallbackCommand = cmd;
            else rule.Command = cmd;
            cfg.Save();
        }

        if (!fallback && !string.IsNullOrWhiteSpace(rule.Command))
        {
            var rerun = rule.RerunCommand;
            if (ImGui.Checkbox("Rerun every interval (s)", ref rerun))
            {
                rule.RerunCommand = rerun;
                cfg.Save();
            }
            if (rule.RerunCommand)
            {
                ImGui.SameLine();
                var every = rule.CommandIntervalSeconds;
                ImGui.SetNextItemWidth(70);
                if (ImGui.InputInt("##int", ref every))
                {
                    rule.CommandIntervalSeconds = Math.Max(0, every);
                    cfg.Save();
                }
            }
        }

        var change = fallback ? rule.ChangeFallbackComment : rule.ChangeSearchComment;
        if (ImGui.Checkbox(fallback ? "Change Search Comment on revert?" : "Change Search Comment?", ref change))
        {
            if (fallback) rule.ChangeFallbackComment = change;
            else rule.ChangeSearchComment = change;
            cfg.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("WARNING: This will change your Character/Adventure Plate Search Info Comment to the plain text you enter here.");
        if (change)
        {
            var comment = fallback ? rule.FallbackComment ?? string.Empty : rule.SearchComment;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputTextWithHint(fallback ? "##fbcmt" : "##cmt", fallback ? "Comment after rule ends" : "Comment while this rule matches", ref comment, 192))
            {
                if (fallback) rule.FallbackComment = comment;
                else rule.SearchComment = comment;
                cfg.Save();
            }
        }
    }
}
