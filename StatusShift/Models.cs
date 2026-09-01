using System;
using System.Collections.Generic;

namespace StatusShift;

public enum OnlineStatusAction
{
    LeaveAlone = 0,
    Online = 1,
    Roleplaying = 2,
    Busy = 3,
    Away = 4,
    LookingForParty = 5,
}

public enum ApplyMode
{
    Confirm = 0,
    Auto = 1,
}

[Serializable]
public class StatusRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New rule";
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }

    public string SearchComment { get; set; } = string.Empty;
    public OnlineStatusAction OnlineStatus { get; set; } = OnlineStatusAction.LeaveAlone;

    public List<ushort> TerritoryIds { get; set; } = [];
    public List<string> TerritoryNameContains { get; set; } = [];
    public List<uint> JobIds { get; set; } = [];
    public List<DayOfWeek> Days { get; set; } = [];

    public bool? InDuty { get; set; }
    public string? TimeStart { get; set; }
    public string? TimeEnd { get; set; }
}
