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

    public ConfigWindow(Plugin plugin) : base("StatusShift Settings###StatusShiftConfig")
    {
        this.plugin = plugin;
        Size = new Vector2(520, 420);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;

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

        var notify = cfg.NotifyInChat;
        if (ImGui.Checkbox("Notify in chat", ref notify))
        {
            cfg.NotifyInChat = notify;
            cfg.Save();
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Install");
        ImGui.TextWrapped("Dev plugin: add StatusShift.dll from bin/x64/Debug or a CI artifact.");
        ImGui.TextWrapped("Custom repo (repo must be public): https://raw.githubusercontent.com/XozaShadow/StatusShift/main/repo.json");

        ImGui.Separator();
        if (ImGui.Button("Copy rules JSON"))
        {
            ImGui.SetClipboardText(plugin.ExportRulesJson());
            lastMsg = "Rules copied to clipboard.";
        }
        ImGui.SameLine();
        if (ImGui.Button("Import clipboard JSON"))
        {
            var clip = ImGui.GetClipboardText();
            lastMsg = plugin.TryImportRulesJson(clip, out var err) ? "Imported." : err;
        }

        ImGui.InputTextMultiline("##import", ref importBuf, 20000, new Vector2(-1, 120));
        if (ImGui.Button("Import box"))
            lastMsg = plugin.TryImportRulesJson(importBuf, out var err2) ? "Imported." : err2;

        if (!string.IsNullOrEmpty(lastMsg))
            ImGui.TextWrapped(lastMsg);
    }
}
