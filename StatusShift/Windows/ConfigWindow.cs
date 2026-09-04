using System;
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
    private string templateBuf = string.Empty;

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
        ToggleRow(cfg, "Show Auto handling", () => cfg.ShowAutoApply, v => cfg.ShowAutoApply = v);
        var labels = ApplyModeNames.ComboLabels(cfg.ShowAutoApply, cfg.ApplyMode);
        var mode = ApplyModeNames.ToCombo(cfg.ApplyMode, cfg.ShowAutoApply);
        if (ImGui.Combo("Handling Mode", ref mode, labels, labels.Length))
        {
            cfg.ApplyMode = ApplyModeNames.FromCombo(mode, cfg.ShowAutoApply, cfg.ApplyMode);
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
        ImGui.TextColored(UiTheme.Teal, "COMMENT TEMPLATES");
        ImGui.SetNextItemWidth(-80);
        ImGui.InputTextWithHint("##tmpl", "New template text", ref templateBuf, 192);
        ImGui.SameLine();
        if (ImGui.Button("Add") && templateBuf.Trim().Length > 0)
        {
            cfg.CommentTemplates.Add(templateBuf.Trim());
            templateBuf = string.Empty;
            cfg.Save();
        }
        for (var i = 0; i < cfg.CommentTemplates.Count; i++)
        {
            ImGui.BulletText(cfg.CommentTemplates[i]);
            ImGui.SameLine();
            ImGui.PushID(i);
            if (ImGui.SmallButton("x"))
            {
                cfg.CommentTemplates.RemoveAt(i);
                cfg.Save();
                ImGui.PopID();
                break;
            }
            ImGui.PopID();
        }

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
        if (ImGui.Button("Replace all from Clipboard"))
        {
            var clip = ImGui.GetClipboardText() ?? string.Empty;
            if (LooksLikeRules(clip))
                lastMsg = plugin.TryImportRulesJson(clip, out var err) ? "Replaced all rules." : err;
            else lastMsg = "Clipboard is empty or not Status Shift JSON.";
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Replaces every rule. Needs valid Status Shift JSON. Empty clipboard is ignored.");

        ImGui.InputTextMultiline("##import", ref importBuf, 20000, new Vector2(-1, 90));
        if (ImGui.Button("Replace All With Import Box Content"))
        {
            if (LooksLikeRules(importBuf))
                lastMsg = plugin.TryImportRulesJson(importBuf, out var err2) ? "Replaced all rules." : err2;
            else lastMsg = "Box is empty or not Status Shift JSON.";
        }
        if (!string.IsNullOrEmpty(lastMsg))
            ImGui.TextWrapped(lastMsg);
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
        var matches = plugin.CurrentMatches();
        if (matches.Count == 0) ImGui.TextDisabled("No rules match right now.");
        foreach (var rule in matches)
            ImGui.TextUnformatted($"P{rule.Priority}  {rule.Name}  {ChatSender.StatusLabels[(int)rule.OnlineStatus]}");
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
