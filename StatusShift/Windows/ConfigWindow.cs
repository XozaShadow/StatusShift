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

    public ConfigWindow(Plugin plugin) : base("Status Shift v0.1.3.1 Settings###StatusShiftConfig")
    {
        this.plugin = plugin;
        Size = new Vector2(540, 640);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;

        ImGui.TextColored(UiTheme.Amber, "Apply");
        var mode = (int)cfg.ApplyMode;
        if (ImGui.Combo("Apply mode", ref mode, ["Confirm (notify only)", "Auto"], 2))
        {
            cfg.ApplyMode = (ApplyMode)mode;
            cfg.Save();
            plugin.RequestEval();
        }
        Hint("Confirm only prints the match. Auto applies when the winner or game state changes.");

        var cooldown = cfg.CooldownSeconds;
        if (ImGui.SliderInt("Auto cooldown (s)", ref cooldown, 5, 180))
        {
            cfg.CooldownSeconds = cooldown;
            cfg.Save();
        }
        Hint("Minimum time between two applies of the same kind of change. New winning rules apply immediately.");

        var poll = cfg.PollSeconds;
        if (ImGui.SliderInt("Check interval (s)", ref poll, 3, 60))
        {
            cfg.PollSeconds = poll;
            cfg.Save();
        }
        Hint("Backup scan and command rerun timer. State changes still eval immediately.");

        var hold = cfg.MinMatchSeconds;
        if (ImGui.SliderInt("Min match time (s)", ref hold, 0, 15))
        {
            cfg.MinMatchSeconds = hold;
            cfg.Save();
        }
        Hint("Rule must stay the winner this long before Auto applies. 0 = instant.");

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Teal, "Skip checks while");
        Toggle("Cutscene", () => cfg.SkipWhileCutscene, v => cfg.SkipWhileCutscene = v, "Do not eval during cutscenes.");
        Toggle("Dead", () => cfg.SkipWhileDead, v => cfg.SkipWhileDead = v, "Do not eval while knocked out.");
        Toggle("In duty", () => cfg.SkipWhileDuty, v => cfg.SkipWhileDuty = v, "Do not eval in instanced duty.");
        Toggle("Combat", () => cfg.SkipWhileCombat, v => cfg.SkipWhileCombat = v, "Do not eval in combat.");
        Toggle("Between areas", () => cfg.SkipWhileBetweenAreas, v => cfg.SkipWhileBetweenAreas = v, "Do not eval while zoning.");
        Toggle("Occupied", () => cfg.SkipWhileOccupied, v => cfg.SkipWhileOccupied = v, "Do not eval in events / occupancy.");
        Toggle("Targeting player", () => cfg.SkipWhileTargetingPlayer, v => cfg.SkipWhileTargetingPlayer = v, "Do not eval while targeting a player.");

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Teal, "Display");
        Toggle("Notify in chat", () => cfg.NotifyInChat, v => cfg.NotifyInChat = v, "Print Status Shift messages to chat.");
        Toggle("Show current location/job at top", () => cfg.ShowSnapshot, v => cfg.ShowSnapshot = v, "Main window snapshot line.");
        Toggle("Open main window on load", () => cfg.OpenUiOnLoad, v => cfg.OpenUiOnLoad = v, "Open the rule window when the plugin loads.");

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Amber, "Analysis");
        ImGui.TextWrapped(plugin.ExplainMatch());
        Hint("How the current match was chosen.");

        ImGui.Separator();
        ImGui.TextDisabled("Rules also save to pluginConfigs/StatusShift/rules.json across updates.");

        if (ImGui.Button("Copy all rules JSON"))
        {
            ImGui.SetClipboardText(plugin.ExportRulesJson());
            lastMsg = "All rules copied.";
        }
        ImGui.SameLine();
        if (ImGui.Button("Replace all from clipboard"))
        {
            var clip = ImGui.GetClipboardText();
            lastMsg = plugin.TryImportRulesJson(clip, out var err) ? "Replaced all rules." : err;
        }

        ImGui.InputTextMultiline("##import", ref importBuf, 20000, new Vector2(-1, 80));
        if (ImGui.Button("Import box as full replace"))
            lastMsg = plugin.TryImportRulesJson(importBuf, out var err2) ? "Replaced all rules." : err2;

        if (!string.IsNullOrEmpty(lastMsg))
            ImGui.TextWrapped(lastMsg);
    }

    private void Toggle(string label, Func<bool> get, Action<bool> set, string tip)
    {
        var v = get();
        if (ImGui.Checkbox(label, ref v))
        {
            set(v);
            plugin.Configuration.Save();
            plugin.RequestEval();
        }
        Hint(tip);
    }

    private static void Hint(string text)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(i)");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(text);
    }
}
