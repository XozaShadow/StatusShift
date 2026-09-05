using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace StatusShift.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string importBuf = string.Empty;
    private string lastMsg = string.Empty;
    private string templateTitleBuf = string.Empty;
    private string templateBodyBuf = string.Empty;
    private int editingTemplate = -1;

    private static readonly ActivityFlag[] LiveStates =
    [
        ActivityFlag.InCombat, ActivityFlag.WeaponDrawn, ActivityFlag.WeaponShown, ActivityFlag.HelmShown,
        ActivityFlag.Walking, ActivityFlag.Dead, ActivityFlag.Crafting, ActivityFlag.Gathering,
        ActivityFlag.Mounted, ActivityFlag.Flying, ActivityFlag.Swimming, ActivityFlag.Diving,
        ActivityFlag.WatchingCutscene, ActivityFlag.InDuty, ActivityFlag.WaitingForDutyFinder,
        ActivityFlag.InParty, ActivityFlag.PartyLeader, ActivityFlag.PvP, ActivityFlag.InResidence,
        ActivityFlag.Sitting, ActivityFlag.Casting, ActivityFlag.Jumping, ActivityFlag.Occupied,
        ActivityFlag.Trading, ActivityFlag.BetweenAreas, ActivityFlag.Roleplaying,
        ActivityFlag.TargetingPlayer, ActivityFlag.TargetingEnemy, ActivityFlag.TargetedByPlayer,
        ActivityFlag.Fishing, ActivityFlag.Performing, ActivityFlag.InSanctuary,
        ActivityFlag.Carrying, ActivityFlag.UsingHousing, ActivityFlag.FashionAccessory,
    ];

    public ConfigWindow(Plugin plugin) : base($"Status Shift v{Plugin.AppVersion} Settings###StatusShiftConfig")
    {
        this.plugin = plugin;
        Size = new Vector2(560, 800);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        WindowName = $"Status Shift v{Plugin.AppVersion} Settings###StatusShiftConfig";
        var cfg = plugin.Configuration;
        cfg.MigrateCommentTemplates();

        ImGui.TextColored(UiTheme.Teal, "SKIP CHECKS WHILE");
        ToggleRow(cfg, "In Combat", () => cfg.SkipWhileCombat, v => cfg.SkipWhileCombat = v);
        ImGui.SameLine();
        ToggleRow(cfg, "Dead", () => cfg.SkipWhileDead, v => cfg.SkipWhileDead = v);
        ImGui.SameLine();
        ToggleRow(cfg, "In Duty", () => cfg.SkipWhileDuty, v => cfg.SkipWhileDuty = v);
        ToggleRow(cfg, "In Cutscene", () => cfg.SkipWhileCutscene, v => cfg.SkipWhileCutscene = v);
        ImGui.SameLine();
        ToggleRow(cfg, "Occupied", () => cfg.SkipWhileOccupied, v => cfg.SkipWhileOccupied = v);
        ImGui.SameLine();
        ToggleRow(cfg, "Between Areas", () => cfg.SkipWhileBetweenAreas, v => cfg.SkipWhileBetweenAreas = v);
        ToggleRow(cfg, "Targeting", () => cfg.SkipWhileTargetingPlayer, v => cfg.SkipWhileTargetingPlayer = v);
        ImGui.SameLine();
        ToggleRow(cfg, "Targeted", () => cfg.SkipWhileTargeted, v => cfg.SkipWhileTargeted = v);
        ImGui.SameLine();
        ToggleRow(cfg, "Emoting", () => cfg.SkipWhileEmoting, v => cfg.SkipWhileEmoting = v);

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Amber, "TIMERS / HANDLING");
        var labels = ApplyModeNames.ComboLabels(true, cfg.ApplyMode);
        var mode = ApplyModeNames.ToCombo(cfg.ApplyMode, true);
        if (ImGui.Combo("Handling Mode", ref mode, labels, labels.Length))
        {
            cfg.ApplyMode = ApplyModeNames.FromCombo(mode, true, cfg.ApplyMode);
            cfg.Save();
            plugin.RequestEval();
        }
        ImGui.TextDisabled(cfg.ApplyMode switch
        {
            ApplyMode.Auto => "Applies the highest matching rule.",
            ApplyMode.Off => "No timers, popups, or notifications.",
            ApplyMode.Selector => "Opens a list of matching rules. Click one to apply.",
            _ => "Chat / toast / sound only. Use /ss apply to set.",
        });
        if (cfg.ApplyMode == ApplyMode.Auto)
        {
            var cooldown = cfg.CooldownSeconds;
            if (ImGui.SliderInt("Auto (s)", ref cooldown, 5, 180))
            {
                cfg.CooldownSeconds = cooldown;
                cfg.Save();
            }
        }
        if (cfg.ApplyMode != ApplyMode.Off)
        {
            var poll = cfg.PollSeconds;
            if (ImGui.SliderInt("Check Interval (s)", ref poll, 3, 120))
            {
                cfg.PollSeconds = poll;
                cfg.Save();
            }
            var hold = cfg.MinMatchSeconds;
            if (ImGui.SliderInt("Min Match Time (s)", ref hold, 0, 15))
            {
                cfg.MinMatchSeconds = hold;
                cfg.Save();
            }
        }

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Teal, "NOTIFICATION / OTHER");
        ToggleRow(cfg, "Notify in Chat", () => cfg.NotifyInChat, v => cfg.NotifyInChat = v);
        ImGui.SameLine();
        ToggleRow(cfg, "Notify with Toast", () => cfg.NotifyWithToast, v => cfg.NotifyWithToast = v);
        ToggleRow(cfg, "Notify with sound", () => cfg.ConfirmPing, v => cfg.ConfirmPing = v);
        if (cfg.ConfirmPing)
        {
            ImGui.SameLine();
            var sound = cfg.NotifySound;
            ImGui.SetNextItemWidth(80);
            if (ImGui.SliderInt("##snd", ref sound, 1, 16))
            {
                cfg.NotifySound = sound;
                cfg.Save();
            }
            ImGui.SameLine();
            if (ImGui.SmallButton("Test")) GameSounds.Play(cfg.NotifySound);
        }
        ToggleRow(cfg, "Open Main on Load", () => cfg.OpenUiOnLoad, v => cfg.OpenUiOnLoad = v);
        ImGui.SameLine();
        ToggleRow(cfg, "Show current info at top", () => cfg.ShowSnapshot, v => cfg.ShowSnapshot = v);

        ImGui.Separator();
        DrawTemplates(cfg);

        ImGui.Separator();
        DrawAnalysis();

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Teal, "FULL RULESET BACKUP");
        if (ImGui.Button("Copy All to Clipboard"))
        {
            ImGui.SetClipboardText(plugin.ExportRulesJson());
            lastMsg = "All rules copied.";
        }
        ImGui.SameLine();
        DangerButton("Replace all from Clipboard", "Hold Shift. Replaces every rule from clipboard JSON.", () =>
        {
            var clip = ImGui.GetClipboardText() ?? string.Empty;
            if (LooksLikeRules(clip))
                lastMsg = plugin.TryImportRulesJson(clip, out var err) ? "Replaced all rules." : err;
            else lastMsg = "Clipboard is empty or not Status Shift JSON.";
        });

        ImGui.InputTextMultiline("##import", ref importBuf, 20000, new Vector2(-1, 90));
        DangerButton("Replace All With Import Box Content", "Hold Shift. Replaces every rule from the box.", () =>
        {
            if (LooksLikeRules(importBuf))
                lastMsg = plugin.TryImportRulesJson(importBuf, out var err2) ? "Replaced all rules." : err2;
            else lastMsg = "Box is empty or not Status Shift JSON.";
        });
        ImGui.SameLine();
        DangerButton("Archive Current & Wipe",
            "Hold Shift. Copies the current save, then starts a blank rules list.",
            () =>
            {
                var path = RuleStore.ArchiveAndWipe(cfg);
                lastMsg = $"All rules cleared, old archived as: {Path.GetFileName(path)}";
                plugin.RequestEval();
            });

        ImGui.TextUnformatted("Save Location: " + RuleStore.FilePath);
        if (!string.IsNullOrEmpty(lastMsg))
            ImGui.TextWrapped(lastMsg);
    }

    private void DrawTemplates(Configuration cfg)
    {
        ImGui.TextColored(UiTheme.Teal, "COMMENT TEMPLATES");
        ImGui.TextDisabled("60 characters. Same specials as the in-game search comment.");
        ImGui.SetNextItemWidth(160);
        ImGui.InputTextWithHint("##ttitle", "Title", ref templateTitleBuf, 32);
        ImGui.InputTextMultiline("##tbody", ref templateBodyBuf, SearchComments.MaxLength + 1, new Vector2(-1, 54));
        templateBodyBuf = SearchComments.Clamp(templateBodyBuf);
        ImGui.TextDisabled($"{templateBodyBuf.Length}/{SearchComments.MaxLength}");
        ImGui.SameLine();
        if (ImGui.SmallButton("Add") && templateBodyBuf.Trim().Length > 0)
        {
            plugin.AddCommentTemplate(templateTitleBuf, templateBodyBuf);
            templateTitleBuf = string.Empty;
            templateBodyBuf = string.Empty;
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("Save last applied"))
        {
            var body = plugin.LastAppliedComment;
            if (string.IsNullOrWhiteSpace(body))
                lastMsg = "No applied comment yet.";
            else
                plugin.AddCommentTemplate(string.IsNullOrWhiteSpace(templateTitleBuf) ? "Last applied" : templateTitleBuf, body);
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Stores the last search comment Status Shift wrote.");

        var wrap = ImGui.GetContentRegionAvail().X;
        var used = 0f;
        for (var i = 0; i < cfg.NamedTemplates.Count; i++)
        {
            var tmpl = cfg.NamedTemplates[i];
            var label = tmpl.Title;
            var need = ImGui.CalcTextSize(label).X + 16;
            if (i > 0 && used + need < wrap) ImGui.SameLine();
            else used = 0;
            used += need;
            ImGui.PushID(tmpl.Id);
            if (ImGui.SmallButton(label))
                ImGui.OpenPopup("tmplpop");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"{tmpl.Body.Length}/{SearchComments.MaxLength}\n{tmpl.Body}\nRight-click for edit / copy last / delete");
            if (ImGui.BeginPopupContextItem("tmplpop"))
            {
                if (ImGui.MenuItem("Edit"))
                {
                    editingTemplate = i;
                    templateTitleBuf = tmpl.Title;
                    templateBodyBuf = tmpl.Body;
                }
                if (ImGui.MenuItem("Copy last applied into this"))
                {
                    var body = plugin.LastAppliedComment;
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        tmpl.Body = SearchComments.Clamp(body);
                        cfg.Save();
                    }
                }
                if (ImGui.MenuItem("Delete"))
                {
                    cfg.NamedTemplates.RemoveAt(i);
                    cfg.Save();
                    ImGui.EndPopup();
                    ImGui.PopID();
                    break;
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }

        if (editingTemplate >= 0 && editingTemplate < cfg.NamedTemplates.Count)
        {
            ImGui.Separator();
            ImGui.TextDisabled("Editing template");
            var tmpl = cfg.NamedTemplates[editingTemplate];
            var title = tmpl.Title;
            var body = tmpl.Body;
            ImGui.SetNextItemWidth(160);
            if (ImGui.InputText("Title", ref title, 32)) { tmpl.Title = title; cfg.Save(); }
            ImGui.InputTextMultiline("##editbody", ref body, SearchComments.MaxLength + 1, new Vector2(-1, 54));
            body = SearchComments.Clamp(body);
            if (body != tmpl.Body) { tmpl.Body = body; cfg.Save(); }
            if (ImGui.SmallButton("Done")) editingTemplate = -1;
        }
    }

    private void DangerButton(string label, string hover, Action act)
    {
        var shift = ImGui.GetIO().KeyShift || ImGui.GetIO().KeyCtrl;
        if (!shift) ImGui.BeginDisabled();
        if (ImGui.Button(label) && shift) act();
        if (!shift) ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(hover);
    }

    private void DrawAnalysis()
    {
        ImGui.TextColored(UiTheme.Amber, "ANALYSIS");
        var snap = plugin.Snapshot();
        var look = LiveLook.Capture(plugin.Configuration.NearbyRange);
        WrapFact($"Job {snap.JobAbbr}", snap.JobAbbr.Length > 0);
        WrapFact(snap.WorldName, snap.WorldName.Length > 0);
        WrapFact(snap.DataCenterName, snap.DataCenterName.Length > 0);
        WrapFact(snap.TerritoryName, snap.TerritoryName.Length > 0);
        if (snap.RegionName.Length > 0) WrapFact(snap.RegionName, true);
        if (snap.ZoneGroupName.Length > 0) WrapFact(snap.ZoneGroupName, true);
        WrapFact(snap.Housing.Summary, snap.InResidence);
        if (look.Mounted) WrapFact("Mount " + look.MountName, true);
        if (look.EmoteName.Length > 0) WrapFact("Emote " + look.EmoteName, true);

        ImGui.Dummy(new Vector2(1, 4));
        var wrap = ImGui.GetContentRegionAvail().X;
        var used = 0f;
        var first = true;
        foreach (var flag in LiveStates.OrderBy(f => f.ToString()))
        {
            var on = snap.Activities.Contains(flag);
            var label = flag.ToString();
            var need = ImGui.CalcTextSize(label + ", ").X;
            if (!first && used + need < wrap) ImGui.SameLine(0, 0);
            else used = 0;
            used += need;
            first = false;
            ImGui.TextColored(on ? UiTheme.Teal : UiTheme.Mute, label);
            ImGui.SameLine(0, 0);
            ImGui.TextDisabled(", ");
        }

        ImGui.Dummy(new Vector2(1, 6));
        ImGui.TextColored(UiTheme.Teal, "RULES");
        var matches = plugin.CurrentPotentialMatches();
        if (matches.Count == 0) ImGui.TextDisabled("No rules match right now.");
        foreach (var rule in matches)
            ImGui.TextUnformatted($"{(rule.Enabled ? "On" : "Off")}  P{rule.Priority}  {rule.Name}  {ChatSender.StatusLabels[(int)rule.OnlineStatus]}");
        ImGui.TextDisabled(plugin.ExplainMatch());
    }

    private static void WrapFact(string text, bool live)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        ImGui.TextColored(live ? UiTheme.Teal : UiTheme.Mute, text);
        ImGui.SameLine();
    }

    private void ToggleRow(Configuration cfg, string label, Func<bool> get, Action<bool> set)
    {
        var v = get();
        if (ImGui.Checkbox(label, ref v))
        {
            set(v);
            cfg.Save();
            plugin.RequestEval();
        }
    }

    private static bool LooksLikeRules(string text)
    {
        text = (text ?? string.Empty).Trim();
        return text.StartsWith('[') || text.StartsWith('{');
    }
}
