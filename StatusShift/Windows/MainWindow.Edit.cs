using System;
using Dalamud.Bindings.ImGui;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private void DrawRule(Configuration cfg, StatusRule rule, ref StatusRule? remove)
    {
        if (ImGui.Button("X"))
        {
            editorOpen = false;
            selectedRuleId = null;
            return;
        }
        Hint("Close editor.");
        DrawRuleMeta(cfg, rule);
        DrawChips(cfg, rule);

        if (ImGui.TreeNode("At schedule"))
        {
            Hint("Outside this window, the rule is skipped.");
            DrawSchedule(cfg, rule);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Legacy location / job / state"))
        {
            DrawLocation(cfg, rule);
            DrawJob(cfg, rule);
            DrawStates(cfg, rule);
            ImGui.TreePop();
        }

        ImGui.Separator();
        UiTheme.Section("Then set", action: true);
        var status = (int)rule.OnlineStatus;
        if (ImGui.Combo("Status", ref status, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
        {
            rule.OnlineStatus = (OnlineStatusAction)status;
            cfg.Save();
        }
        Hint("Leave alone = do not touch online status.");

        var sticky = rule.Sticky;
        if (ImGui.Checkbox("Sticky (do not revert when rule ends)", ref sticky))
        {
            rule.Sticky = sticky;
            cfg.Save();
        }
        if (!rule.Sticky)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted("Else revert to:");
            ImGui.SameLine();
            var fb = (int)rule.FallbackStatus;
            ImGui.SetNextItemWidth(160);
            if (ImGui.Combo("##revert", ref fb, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
            {
                rule.FallbackStatus = (OnlineStatusAction)fb;
                cfg.Save();
            }
        }

        var cmd = rule.Command ?? string.Empty;
        if (ImGui.InputText("Command / macro", ref cmd, 192))
        {
            rule.Command = cmd;
            cfg.Save();
        }
        Hint("Optional. /sit is fine. /ss apply runs once when this rule starts matching.");
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
                var every = rule.CommandIntervalSeconds;
                ImGui.SetNextItemWidth(80);
                if (ImGui.InputInt("Interval s", ref every))
                {
                    rule.CommandIntervalSeconds = Math.Max(0, every);
                    cfg.Save();
                }
            }
        }

        var change = rule.ChangeSearchComment;
        if (ImGui.Checkbox("Also change search comment", ref change))
        {
            rule.ChangeSearchComment = change;
            if (!change)
            {
                rule.SearchComment = string.Empty;
                rule.ChangeFallbackComment = false;
                rule.FallbackComment = string.Empty;
            }
            cfg.Save();
        }
        if (rule.ChangeSearchComment)
        {
            var comment = rule.SearchComment;
            if (ImGui.InputText("While this rule matches", ref comment, 192)) { rule.SearchComment = comment; cfg.Save(); }
        }

        if (ImGui.Button("Copy rule JSON"))
        {
            ImGui.SetClipboardText(plugin.ExportRuleJson(rule));
            importMsg = $"Copied {rule.Name}.";
        }
        ImGui.SameLine();
        var io = ImGui.GetIO();
        var canDelete = io.KeyShift || io.KeyCtrl;
        if (!canDelete) ImGui.BeginDisabled();
        if (ImGui.Button("Delete rule")) remove = rule;
        if (!canDelete) ImGui.EndDisabled();
        Hint("Hold Shift or Ctrl to delete.");
    }
}
