using System;
using Dalamud.Bindings.ImGui;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private void DrawRule(Configuration cfg, StatusRule rule, ref StatusRule? remove)
    {
        if (rule.HasLegacy)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.35f, 0.28f, 1f), "Not compatible");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(rule.LegacySummary());
        }

        ImGui.TextColored(UiTheme.Amber, $"EDITING: P{rule.Priority}  {rule.Name}");
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
        if (ImGui.SmallButton("Duplicate")) plugin.DuplicateRule(rule);
        ImGui.SameLine();
        if (ImGui.SmallButton("X"))
        {
            editorOpen = false;
            selectedRuleId = null;
            return;
        }

        var on = rule.Enabled;
        if (ImGui.Checkbox("On", ref on)) { rule.Enabled = on; cfg.Save(); plugin.RequestEval(); }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(70);
        var prio = rule.Priority;
        if (ImGui.InputInt("##prio", ref prio)) { rule.Priority = prio; cfg.Save(); }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(160);
        var name = rule.Name;
        if (ImGui.InputTextWithHint("##name", "Name", ref name, 64)) { rule.Name = name; cfg.Save(); }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        var folder = rule.Folder ?? string.Empty;
        if (ImGui.InputTextWithHint("##folder", "Category", ref folder, 48)) { rule.Folder = folder.Trim(); cfg.Save(); }

        var notes = rule.Notes ?? string.Empty;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint("##notes", "Notes", ref notes, 120)) { rule.Notes = notes; cfg.Save(); }

        var ping = rule.NotifyIfNotApplied;
        if (ImGui.Checkbox("Notify if rule applies but is not applied", ref ping))
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

        if (rule.HasLegacy && ImGui.TreeNode("Legacy Conditions"))
        {
            ImGui.TextWrapped("This rule has older conditions. Move them into AND/OR chips, then Duplicate to get a clean copy without legacy data.");
            ImGui.TextWrapped(rule.LegacySummary());
            DrawLocation(cfg, rule);
            DrawJob(cfg, rule);
            DrawStates(cfg, rule);
            ImGui.TreePop();
        }

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Amber, "THEN SET / RUN / UPDATE");
        var status = (int)rule.OnlineStatus;
        ImGui.SetNextItemWidth(180);
        if (ImGui.Combo("Status", ref status, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
        {
            rule.OnlineStatus = (OnlineStatusAction)status;
            cfg.Save();
        }
        ImGui.SameLine();
        var cmd = rule.Command ?? string.Empty;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint("##cmd", "Command / macro", ref cmd, 192))
        {
            rule.Command = cmd;
            cfg.Save();
        }
        if (!string.IsNullOrWhiteSpace(rule.Command))
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

        var change = rule.ChangeSearchComment;
        if (ImGui.Checkbox("Change Search Comment?", ref change))
        {
            rule.ChangeSearchComment = change;
            cfg.Save();
        }
        Hint("WARNING: This will change your Character/Adventure Plate Search Info Comment to the plain text you enter here.");
        if (rule.ChangeSearchComment)
        {
            var comment = rule.SearchComment;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputTextWithHint("##cmt", "Comment while this rule matches", ref comment, 192))
            {
                rule.SearchComment = comment;
                cfg.Save();
            }
        }

        ImGui.Separator();
        var revert = rule.RevertWhenFalse;
        if (ImGui.Checkbox("UNTIL RULE ENDS REVERT TO", ref revert))
        {
            rule.RevertWhenFalse = revert;
            cfg.Save();
        }
        if (!revert)
            ImGui.TextDisabled("AND REMAIN THIS WAY AFTER THE RULE ENDS");
        else
        {
            var fb = (int)rule.FallbackStatus;
            ImGui.SetNextItemWidth(180);
            if (ImGui.Combo("##fbst", ref fb, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
            {
                rule.FallbackStatus = (OnlineStatusAction)fb;
                cfg.Save();
            }
            ImGui.SameLine();
            var fcmd = rule.FallbackCommand ?? string.Empty;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputTextWithHint("##fbcmd", "Command / macro on revert", ref fcmd, 192))
            {
                rule.FallbackCommand = fcmd;
                cfg.Save();
            }
            var fbc = rule.ChangeFallbackComment;
            if (ImGui.Checkbox("Change Search Comment on revert?", ref fbc))
            {
                rule.ChangeFallbackComment = fbc;
                cfg.Save();
            }
            if (rule.ChangeFallbackComment)
            {
                var fbct = rule.FallbackComment ?? string.Empty;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputTextWithHint("##fbcmt", "Comment after rule ends", ref fbct, 192))
                {
                    rule.FallbackComment = fbct;
                    cfg.Save();
                }
            }
        }

        var io = ImGui.GetIO();
        var canDelete = io.KeyShift || io.KeyCtrl;
        if (!canDelete) ImGui.BeginDisabled();
        if (ImGui.Button("Delete rule")) remove = rule;
        if (!canDelete) ImGui.EndDisabled();
    }
}
