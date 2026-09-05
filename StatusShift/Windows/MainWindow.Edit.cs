using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace StatusShift.Windows;

public partial class MainWindow
{
    private string templateTitleBuf = string.Empty;
    private string testMsg = string.Empty;

    private void DrawRule(Configuration cfg, StatusRule rule, ref StatusRule? remove)
    {
        if (rule.HasLegacy)
        {
            ImGui.TextColored(new Vector4(1f, 0.35f, 0.28f, 1f), "Not compatible");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(rule.LegacySummary());
        }

        ImGui.TextColored(UiTheme.Amber, $"EDITING: P{rule.Priority}: {rule.Name}");
        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();
        if (TestButton("hdr", rule, RuleTestStage.Full,
                "Runs character, schedule, conditions, then set. After 5 seconds reverts as if the rule stopped matching. Ignores timers."))
            return;
        ImGui.SameLine();
        if (ImGui.SmallButton("Duplicate"))
        {
            plugin.DuplicateRule(rule);
            var copy = cfg.Rules[^1];
            selectedRuleId = copy.Id;
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("Close"))
        {
            editorOpen = false;
            selectedRuleId = null;
            return;
        }
        ImGui.SameLine();
        var ioDel = ImGui.GetIO();
        var canDelete = ioDel.KeyShift || ioDel.KeyCtrl;
        if (!canDelete) ImGui.BeginDisabled();
        if (ImGui.SmallButton("Delete")) remove = rule;
        if (!canDelete) ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("Hold Shift to delete this rule.");
        ImGui.SameLine();
        ImGui.TextDisabled("|  Copy Share:");
        ImGui.SameLine();
        if (ImGui.SmallButton("Code"))
        {
            ImGui.SetClipboardText(ChipShare.Encode(rule));
            importMsg = "Share code copied.";
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("JSON"))
        {
            ImGui.SetClipboardText(plugin.ExportRuleJson(rule));
            importMsg = "JSON copied.";
        }
        if (!string.IsNullOrEmpty(testMsg))
            ImGui.TextDisabled(testMsg);

        if (ImGui.BeginTable("edmeta", 4, ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("on", ImGuiTableColumnFlags.WidthFixed, 44);
            ImGui.TableSetupColumn("prio", ImGuiTableColumnFlags.WidthFixed, 52);
            ImGui.TableSetupColumn("name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("cat", ImGuiTableColumnFlags.WidthFixed, 168);
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

        var chat = rule.NotifyChat;
        var audible = rule.NotifyAudible;
        if (ImGui.Checkbox("Chat", ref chat))
        {
            rule.NotifyChat = chat;
            rule.NotifyIfNotApplied = rule.NotifyChat || rule.NotifyAudible;
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Sound", ref audible))
        {
            rule.NotifyAudible = audible;
            rule.NotifyIfNotApplied = rule.NotifyChat || rule.NotifyAudible;
            if (audible && rule.NotifySound <= 0) rule.NotifySound = cfg.NotifySound;
            cfg.Save();
        }
        if (rule.NotifyAudible)
        {
            ImGui.SameLine();
            var sound = rule.NotifySound <= 0 ? cfg.NotifySound : rule.NotifySound;
            ImGui.SetNextItemWidth(80);
            if (ImGui.SliderInt("##rsnd", ref sound, 1, 16))
            {
                rule.NotifySound = sound;
                cfg.Save();
            }
            ImGui.SameLine();
            if (ImGui.SmallButton("Test##snd")) GameSounds.Play(rule.NotifySound);
        }
        ImGui.SameLine();
        ImGui.TextDisabled("Notify if this rule matches but is not applied");

        ImGui.Separator();
        var character = rule.CharacterFilter ?? string.Empty;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint("##char", "Character  First Last or First Last@World  (blank = all)", ref character, 64))
        {
            rule.CharacterFilter = character.Trim();
            cfg.Save();
        }

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Teal, "DURING SCHEDULE");
        ImGui.SameLine();
        TestButton("sched", rule, RuleTestStage.FromSchedule,
            "Starts at schedule. Skips the character filter. Then runs conditions and Then Set. Reverts after 5 seconds.");
        DrawSchedule(cfg, rule);

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Teal, "IF THESE CONDITIONS");
        ImGui.SameLine();
        TestButton("cond", rule, RuleTestStage.FromConditions,
            "Only checks conditions, then Then Set. Reverts after 5 seconds.");
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
        ImGui.TextColored(UiTheme.Teal, "THEN SET / RUN / UPDATE");
        ImGui.SameLine();
        TestButton("then", rule, RuleTestStage.ThenOnly,
            "Runs Then Set only. After 5 seconds continues to WHEN RULE STOPS MATCHING.");
        DrawThen(cfg, rule, false);

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Teal, "WHEN THIS RULE STOPS MATCHING");
        ImGui.SameLine();
        TestButton("stop", rule, RuleTestStage.RevertNow,
            "Runs the revert / keep path immediately. No wait.");
        var revert = rule.RevertWhenFalse;
        if (ImGui.RadioButton("Revert to the values below", revert))
        {
            rule.RevertWhenFalse = true;
            cfg.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Keep what this rule set", !revert))
        {
            rule.RevertWhenFalse = false;
            cfg.Save();
        }
        if (rule.RevertWhenFalse)
            DrawThen(cfg, rule, true);
        else
            ImGui.TextDisabled("Status, command, and comment stay until another rule changes them.");
    }

    private bool TestButton(string id, StatusRule rule, RuleTestStage stage, string hover)
    {
        ImGui.PushID("test" + id);
        if (ImGui.SmallButton("Test"))
            testMsg = plugin.TestRule(rule, stage);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(hover);
        ImGui.PopID();
        return false;
    }

    private void DrawThen(Configuration cfg, StatusRule rule, bool fallback)
    {
        cfg.MigrateCommentTemplates();
        var lockStatus = !fallback && rule.UsesStatusCondition;
        if (lockStatus && rule.OnlineStatus != OnlineStatusAction.LeaveAlone)
        {
            rule.OnlineStatus = OnlineStatusAction.LeaveAlone;
            cfg.Save();
        }

        var status = (int)(fallback ? rule.FallbackStatus : rule.OnlineStatus);
        ImGui.SetNextItemWidth(180);
        if (lockStatus) ImGui.BeginDisabled();
        if (ImGui.Combo(fallback ? "##fbst" : "Status", ref status, ChatSender.StatusLabels, ChatSender.StatusLabels.Length))
        {
            if (fallback) rule.FallbackStatus = (OnlineStatusAction)status;
            else rule.OnlineStatus = (OnlineStatusAction)status;
            cfg.Save();
        }
        if (lockStatus)
        {
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("Status is a condition on this rule, so Then Set status stays Leave alone.");
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
        if (!fallback)
            ImGui.TextDisabled("{teller} {targeter} {target} {zone} {job} {world} {home} {dc} {ward} {plot} {time}");

        if (!fallback && !string.IsNullOrWhiteSpace(rule.Command))
        {
            var delay = rule.CommandDelaySeconds;
            ImGui.SetNextItemWidth(60);
            if (ImGui.InputInt("Wait before command (s)", ref delay))
            {
                rule.CommandDelaySeconds = Math.Max(0, delay);
                cfg.Save();
            }
            var rerun = rule.RerunCommand;
            if (ImGui.Checkbox("Repeat this /command every", ref rerun))
            {
                rule.RerunCommand = rerun;
                cfg.Save();
            }
            if (rule.RerunCommand)
            {
                ImGui.SameLine();
                var every = rule.CommandIntervalSeconds;
                ImGui.SetNextItemWidth(60);
                if (ImGui.InputInt("##int", ref every))
                {
                    rule.CommandIntervalSeconds = Math.Max(0, every);
                    cfg.Save();
                }
                ImGui.SameLine();
                ImGui.TextUnformatted("s   (0 = check interval)");
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
            ImGui.SetTooltip("WARNING: This will change your Character/Adventure Plate Search Info Comment to the text you enter here (60 characters).");
        if (change)
            DrawCommentPicker(cfg, rule, fallback);
    }

    private void DrawCommentPicker(Configuration cfg, StatusRule rule, bool fallback)
    {
        var tmplId = fallback ? rule.FallbackCommentTemplate : rule.CommentTemplate;
        var useTmpl = !string.IsNullOrWhiteSpace(tmplId);
        if (ImGui.RadioButton(fallback ? "Type comment##fbc" : "Type comment", !useTmpl))
        {
            if (fallback) rule.FallbackCommentTemplate = string.Empty;
            else rule.CommentTemplate = string.Empty;
            cfg.Save();
            useTmpl = false;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton(fallback ? "Use template##fbt" : "Use template", useTmpl))
        {
            if (cfg.NamedTemplates.Count > 0 && string.IsNullOrWhiteSpace(tmplId))
            {
                if (fallback) rule.FallbackCommentTemplate = cfg.NamedTemplates[0].Id;
                else rule.CommentTemplate = cfg.NamedTemplates[0].Id;
                cfg.Save();
                tmplId = fallback ? rule.FallbackCommentTemplate : rule.CommentTemplate;
            }
            useTmpl = true;
        }

        if (useTmpl)
        {
            var titles = cfg.NamedTemplates.Select(t => t.Title).ToArray();
            var idx = cfg.NamedTemplates.FindIndex(t => t.Id == tmplId);
            if (idx < 0) idx = 0;
            if (titles.Length == 0)
            {
                ImGui.TextDisabled("No templates yet. Type a comment and Save as template.");
            }
            else
            {
                ImGui.SetNextItemWidth(220);
                if (ImGui.Combo(fallback ? "##fbtmpl" : "##tmpl", ref idx, titles, titles.Length))
                {
                    var id = cfg.NamedTemplates[idx].Id;
                    if (fallback) rule.FallbackCommentTemplate = id;
                    else rule.CommentTemplate = id;
                    cfg.Save();
                }
                var body = idx >= 0 && idx < cfg.NamedTemplates.Count ? cfg.NamedTemplates[idx].Body : string.Empty;
                ImGui.TextDisabled($"{body.Length}/{SearchComments.MaxLength}  {body}");
            }
        }
        else
        {
            var comment = fallback ? rule.FallbackComment ?? string.Empty : rule.SearchComment;
            comment = SearchComments.Clamp(comment);
            ImGui.InputTextMultiline(fallback ? "##fbcmt" : "##cmt", ref comment, SearchComments.MaxLength + 1, new Vector2(-1, 54));
            comment = SearchComments.Clamp(comment);
            if (fallback)
            {
                if (comment != (rule.FallbackComment ?? string.Empty))
                {
                    rule.FallbackComment = comment;
                    cfg.Save();
                }
            }
            else if (comment != rule.SearchComment)
            {
                rule.SearchComment = comment;
                cfg.Save();
            }
            ImGui.TextDisabled($"{comment.Length}/{SearchComments.MaxLength}");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(140);
            ImGui.InputTextWithHint(fallback ? "##fbtitle" : "##svtitle", "Template title", ref templateTitleBuf, 32);
            ImGui.SameLine();
            if (ImGui.SmallButton(fallback ? "Save as template##fbs" : "Save as template"))
            {
                var title = string.IsNullOrWhiteSpace(templateTitleBuf) ? rule.Name : templateTitleBuf.Trim();
                var created = plugin.AddCommentTemplate(title, comment);
                if (fallback) rule.FallbackCommentTemplate = created.Id;
                else rule.CommentTemplate = created.Id;
                templateTitleBuf = string.Empty;
                cfg.Save();
            }
        }
    }
}
