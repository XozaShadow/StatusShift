using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace StatusShift;

[Serializable]
public partial class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 7;

    public bool Enabled { get; set; } = true;
    public ApplyMode ApplyMode { get; set; } = ApplyMode.Confirm;
    public int CooldownSeconds { get; set; } = 15;
    public int PollSeconds { get; set; } = 10;
    public int MinMatchSeconds { get; set; }
    public bool NotifyInChat { get; set; } = true;
    public bool SkipWhileCutscene { get; set; } = true;
    public bool SkipWhileDead { get; set; } = true;
    public bool SkipWhileDuty { get; set; }
    public bool SkipWhileCombat { get; set; }
    public bool SkipWhileBetweenAreas { get; set; } = true;
    public bool SkipWhileOccupied { get; set; }
    public bool SkipWhileTargetingPlayer { get; set; }
    public bool OpenUiOnLoad { get; set; }
    public bool ShowSnapshot { get; set; } = true;
    public List<StatusRule> Rules { get; set; } = DefaultRules();

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
        RuleStore.Save(this);
    }

    private static List<StatusRule> DefaultRules() =>
    [
        new()
        {
            Name = "In duty",
            Notes = "Busy while instanced",
            Folder = "Examples",
            Enabled = true,
            Priority = 100,
            OnlineStatus = OnlineStatusAction.Busy,
            Activities = [ActivityFlag.InDuty],
            States = [new StateFilter { Flag = ActivityFlag.InDuty, Op = MatchOp.Yes }],
        },
        new()
        {
            Name = "Venue hours",
            Notes = "Weekend evenings",
            Folder = "Examples",
            Enabled = true,
            Priority = 50,
            OnlineStatus = OnlineStatusAction.Roleplaying,
            Schedule = new RuleSchedule
            {
                Mode = ScheduleMode.Weekly,
                AllDay = false,
                StartHour = 20,
                EndHour = 23,
                Days = [DayOfWeek.Friday, DayOfWeek.Saturday],
            },
        },
        new()
        {
            Name = "Default",
            Notes = "Fallback online",
            Folder = "Examples",
            Enabled = true,
            Priority = 0,
            OnlineStatus = OnlineStatusAction.Online,
        },
    ];
}
