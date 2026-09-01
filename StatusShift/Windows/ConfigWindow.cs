using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace StatusShift.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin) : base("StatusShift Settings###StatusShiftConfig")
    {
        this.plugin = plugin;
        Size = new Vector2(380, 220);
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

        ImGui.TextWrapped("Auto mode sends /searchcomment and status commands when a rule changes. Keep Confirm on until you have tested it.");
    }
}
