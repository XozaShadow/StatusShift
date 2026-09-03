using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace StatusShift.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string importBuf = string.Empty;
    private string lastMsg = string.Empty;

    public ConfigWindow(Plugin plugin) : base($"Status Shift v{Plugin.AppVersion} Settings###StatusShiftConfig")
    {
        this.plugin = plugin;
        Size = new Vector2(560, 760);
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
        var mode = (int)cfg.ApplyMode;
        if (ImGui.Combo("Handling Mode", ref mode, ApplyModeNames.Labels, ApplyModeNames.Labels.Length))
        {
            cfg.ApplyMode = (ApplyMode)mode;
            cfg.Save();
            plugin.RequestEval();
        }
        ImGui.TextDisabled(mode switch
        {
            (int)ApplyMode.Auto => "Applies the highest matching rule.",
            (int)ApplyMode.Off => "No timers, popups, or notifications.",
            (int)ApplyMode.Selector => "Opens a list of matching rules. Click one to apply.",
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
        ImGui.TextColored(UiTheme.Amber, "ANALYSIS");
        var matches = plugin.CurrentMatches();
        if (matches.Count == 0) ImGui.TextDisabled("No rules match right now.");
        foreach (var rule in matches)
            ImGui.TextUnformatted($"P{rule.Priority}  {rule.Name}  {ChatSender.StatusLabels[(int)rule.OnlineStatus]}");
        ImGui.TextDisabled(plugin.ExplainMatch());
        ImGui.TextDisabled("State chips: InCombat Walking Sitting Mounted InDuty TargetingPlayer TargetedByPlayer Occupied BetweenAreas Roleplaying HelmShown WeaponShown WeaponDrawn Casting Flying Swimming Dead Crafting Gathering");

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
