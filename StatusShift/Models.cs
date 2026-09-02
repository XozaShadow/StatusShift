using System;
using System.Collections.Generic;

namespace StatusShift;

public enum OnlineStatusAction
{
    LeaveAlone = 0,
    Online = 1,
    Away = 2,
    Busy = 3,
    Roleplaying = 4,
    LookingToMeld = 5,
    LookingForParty = 6,
    Mentor = 7,
    PveMentor = 8,
    PvpMentor = 9,
    TradeMentor = 10,
    Returner = 11,
    NewAdventurer = 12,
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
    Diving = 11,
    WeaponDrawn = 12,
    WaitingForDutyFinder = 13,
    PvP = 14,
    PartyLeader = 15,
    InResidence = 16,
    Sitting = 17,
    Casting = 18,
    Jumping = 19,
    Occupied = 20,
    Trading = 21,
    BetweenAreas = 22,
    Roleplaying = 23,
    TargetingPlayer = 24,
    TargetingEnemy = 25,
    TargetedByPlayer = 26,
}

public enum LocationKind
{
    Any = 0,
    TerritoryId = 1,
    ZoneName = 2,
    Region = 3,
    ZoneGroup = 4,
    World = 5,
    Residence = 6,
}

public enum MatchOp
{
    Any = 0,
    Yes = 1,
    No = 2,
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
public class LocationFilter
{
    public LocationKind Kind { get; set; } = LocationKind.Any;
    public string Value { get; set; } = string.Empty;
    public ResidenceKind ResidenceKind { get; set; } = ResidenceKind.House;
    public string District { get; set; } = string.Empty;
    public int Ward { get; set; }
    public int Plot { get; set; }
    public int Apartment { get; set; }
    public bool Subdivision { get; set; }
}

[Serializable]
public class StateFilter
{
    public ActivityFlag Flag { get; set; }
    public MatchOp Op { get; set; } = MatchOp.Yes;
}

[Serializable]
public class StatusRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New rule";
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }

    public bool ChangeSearchComment { get; set; }
    public string SearchComment { get; set; } = string.Empty;
    public OnlineStatusAction OnlineStatus { get; set; } = OnlineStatusAction.LeaveAlone;
    public bool RevertWhenFalse { get; set; } = true;
    public OnlineStatusAction FallbackStatus { get; set; } = OnlineStatusAction.Online;
    public bool ChangeFallbackComment { get; set; }
    public string FallbackComment { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;
    public bool RerunCommand { get; set; }
    public int CommandIntervalSeconds { get; set; }

    public RuleSchedule Schedule { get; set; } = new();
    public List<StateFilter> States { get; set; } = [];
    public LocationFilter Location { get; set; } = new();
    public string WorldFilter { get; set; } = string.Empty;

    public List<ActivityFlag> Activities { get; set; } = [];
    public List<uint> TerritoryIds { get; set; } = [];
    public List<string> TerritoryNameContains { get; set; } = [];
    public List<uint> JobIds { get; set; } = [];
    public List<string> JobAbbrs { get; set; } = [];
    public List<uint> WorldIds { get; set; } = [];
    public List<DayOfWeek> Days { get; set; } = [];
    public bool? InDuty { get; set; }
    public string? TimeStart { get; set; }
    public string? TimeEnd { get; set; }

    public bool Sticky
    {
        get => !RevertWhenFalse;
        set => RevertWhenFalse = !value;
    }

    public bool HasCommand => !string.IsNullOrWhiteSpace(Command);

    public int EffectiveCommandInterval(int pollSeconds)
    {
        if (!RerunCommand) return 0;
        return CommandIntervalSeconds > 0 ? CommandIntervalSeconds : Math.Max(3, pollSeconds);
    }
}
