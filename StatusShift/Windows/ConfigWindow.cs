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

    public ConfigWindow(Plugin plugin) : base("Status Shift v0.1.3 Settings###StatusShiftConfig")
    {
        this.plugin = plugin;
        Size = new Vector2(520, 560);
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
        }

        var cooldown = cfg.CooldownSeconds;
        if (ImGui.SliderInt("Auto cooldown (s)", ref cooldown, 10, 180))
        {
            cfg.CooldownSeconds = cooldown;
            cfg.Save();
        }

        var poll = cfg.PollSeconds;
        if (ImGui.SliderInt("Check interval (s)", ref poll, 3, 60))
        {
            cfg.PollSeconds = poll;
            cfg.Save();
        }

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Teal, "Skip checks while");
        Toggle("Cutscene", () => cfg.SkipWhileCutscene, v => cfg.SkipWhileCutscene = v);
        Toggle("Dead", () => cfg.SkipWhileDead, v => cfg.SkipWhileDead = v);
        Toggle("In duty", () => cfg.SkipWhileDuty, v => cfg.SkipWhileDuty = v);

        ImGui.Separator();
        ImGui.TextColored(UiTheme.Teal, "Display");
        Toggle("Notify in chat", () => cfg.NotifyInChat, v => cfg.NotifyInChat = v);
        Toggle("Show current location/job at top", () => cfg.ShowSnapshot, v => cfg.ShowSnapshot = v);
        Toggle("Open main window on load", () => cfg.OpenUiOnLoad, v => cfg.OpenUiOnLoad = v);

        ImGui.Separator();
        ImGui.TextDisabled("Rules are stored in the plugin config folder as rules.json.");
        ImGui.TextDisabled("That file is kept across plugin updates.");

        ImGui.Separator();
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

        ImGui.InputTextMultiline("##import", ref importBuf, 20000, new Vector2(-1, 100));
        if (ImGui.Button("Import box as full replace"))
            lastMsg = plugin.TryImportRulesJson(importBuf, out var err2) ? "Replaced all rules." : err2;

        if (!string.IsNullOrEmpty(lastMsg))
            ImGui.TextWrapped(lastMsg);
    }

    private void Toggle(string label, Func<bool> get, Action<bool> set)
    {
        var v = get();
        if (ImGui.Checkbox(label, ref v))
        {
            set(v);
            plugin.Configuration.Save();
        }
    }
}
