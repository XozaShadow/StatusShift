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

public enum ScheduleMode
{
    Always = 0,
    Daily = 1,
    Weekly = 2,
    OneTime = 3,
    Custom = 4,
}

public enum ActivityFlag
{
    InDuty = 0,
    InCombat = 1,
    Crafting = 2,
    Gathering = 3,
    Mounted = 4,
    Flying = 5,
    Swimming = 6,
    WatchingCutscene = 7,
    Dead = 8,
    InParty = 9,
    BoundByDuty = 10,
}

[Serializable]
public class RuleSchedule
{
    public ScheduleMode Mode { get; set; } = ScheduleMode.Always;
    public bool AllDay { get; set; } = true;
    public int StartHour { get; set; } = 9;
    public int StartMinute { get; set; }
    public int EndHour { get; set; } = 12;
    public int EndMinute { get; set; }
    public List<DayOfWeek> Days { get; set; } = [];
    public string? DateStart { get; set; }
    public string? DateEnd { get; set; }
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

    public RuleSchedule Schedule { get; set; } = new();
    public List<ActivityFlag> Activities { get; set; } = [];
    public List<ushort> TerritoryIds { get; set; } = [];
    public List<string> TerritoryNameContains { get; set; } = [];
    public List<uint> JobIds { get; set; } = [];
    public List<uint> WorldIds { get; set; } = [];

    public List<DayOfWeek> Days { get; set; } = [];
    public bool? InDuty { get; set; }
    public string? TimeStart { get; set; }
    public string? TimeEnd { get; set; }
}
