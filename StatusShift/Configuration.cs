using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace StatusShift;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public bool Enabled { get; set; } = true;
    public ApplyMode ApplyMode { get; set; } = ApplyMode.Confirm;
    public int CooldownSeconds { get; set; } = 45;
    public int PollSeconds { get; set; } = 10;
    public bool NotifyInChat { get; set; } = true;
    public List<StatusRule> Rules { get; set; } = DefaultRules();

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    private static List<StatusRule> DefaultRules() =>
    [
        new()
        {
            Name = "In duty",
            Priority = 100,
            SearchComment = "In content — whispers after.",
            OnlineStatus = OnlineStatusAction.Busy,
            Activities = [ActivityFlag.InDuty],
        },
        new()
        {
            Name = "Venue hours",
            Priority = 50,
            SearchComment = "Venue hours. Walk-ups welcome.",
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
            Priority = 0,
            SearchComment = "Whispers welcome.",
        },
    ];
}
