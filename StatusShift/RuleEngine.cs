using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Lumina.Excel.Sheets;

namespace StatusShift;

internal sealed class RuleEngine(Configuration config)
{
    public StatusRule? FindMatch()
    {
        if (!Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded)
            return null;

        var ctx = Snapshot();
        return config.Rules
            .Where(r => r.Enabled)
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault(r => Matches(r, ctx));
    }

    public string ResolveComment(StatusRule rule)
    {
        var snap = GameSnapshot.Capture();
        var time = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);

        return rule.SearchComment
            .Replace("{zone}", snap.TerritoryName, StringComparison.OrdinalIgnoreCase)
            .Replace("{job}", snap.JobAbbr, StringComparison.OrdinalIgnoreCase)
            .Replace("{world}", snap.WorldName, StringComparison.OrdinalIgnoreCase)
            .Replace("{home}", snap.HomeWorldName, StringComparison.OrdinalIgnoreCase)
            .Replace("{time}", time, StringComparison.OrdinalIgnoreCase);
    }

    public GameSnapshot Snapshot() => GameSnapshot.Capture();

    private static bool Matches(StatusRule rule, GameSnapshot ctx)
    {
        if (rule.TerritoryIds.Count > 0 && !rule.TerritoryIds.Contains(ctx.TerritoryId))
            return false;

        if (rule.TerritoryNameContains.Count > 0 &&
            !rule.TerritoryNameContains.Any(n => ctx.TerritoryName.Contains(n, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (rule.JobIds.Count > 0 && !rule.JobIds.Contains(ctx.JobId))
            return false;

        if (rule.WorldIds.Count > 0 && !rule.WorldIds.Contains(ctx.WorldId))
            return false;

        if (rule.Activities.Count > 0 && !rule.Activities.All(ctx.Activities.Contains))
            return false;

        if (rule.InDuty == true && !ctx.Activities.Contains(ActivityFlag.InDuty))
            return false;

        return ScheduleMatches(rule, ctx.Now);
    }

    private static bool ScheduleMatches(StatusRule rule, DateTime now)
    {
        var sched = rule.Schedule ?? new RuleSchedule();

        if (sched.Mode == ScheduleMode.Always && (rule.Days.Count > 0 || rule.TimeStart is not null || rule.TimeEnd is not null))
            return LegacyTimeMatches(rule, now);

        return sched.Mode switch
        {
            ScheduleMode.Always => true,
            ScheduleMode.Daily => TimeMatches(sched, now.TimeOfDay),
            ScheduleMode.Weekly => DaysMatch(sched.Days, now.DayOfWeek) && TimeMatches(sched, now.TimeOfDay),
            ScheduleMode.OneTime => DateMatches(sched, now.Date) && TimeMatches(sched, now.TimeOfDay),
            ScheduleMode.Custom => DateMatches(sched, now.Date) && DaysMatch(sched.Days, now.DayOfWeek) && TimeMatches(sched, now.TimeOfDay),
            _ => true,
        };
    }

    private static bool LegacyTimeMatches(StatusRule rule, DateTime now)
    {
        if (rule.Days.Count > 0 && !rule.Days.Contains(now.DayOfWeek))
            return false;
        return InTimeWindow(rule.TimeStart, rule.TimeEnd, now.TimeOfDay);
    }

    private static bool DaysMatch(List<DayOfWeek> days, DayOfWeek today) =>
        days.Count == 0 || days.Contains(today);

    private static bool DateMatches(RuleSchedule sched, DateTime today)
    {
        if (DateTime.TryParse(sched.DateStart, out var start) && today < start.Date)
            return false;
        if (DateTime.TryParse(sched.DateEnd, out var end) && today > end.Date)
            return false;
        return true;
    }

    private static bool TimeMatches(RuleSchedule sched, TimeSpan now)
    {
        if (sched.AllDay)
            return true;
        var start = new TimeSpan(Clamp(sched.StartHour, 0, 23), Clamp(sched.StartMinute, 0, 59), 0);
        var end = new TimeSpan(Clamp(sched.EndHour, 0, 23), Clamp(sched.EndMinute, 0, 59), 0);
        return start <= end ? now >= start && now <= end : now >= start || now <= end;
    }

    private static bool InTimeWindow(string? startText, string? endText, TimeSpan now)
    {
        if (string.IsNullOrWhiteSpace(startText) && string.IsNullOrWhiteSpace(endText))
            return true;
        if (!TryParseTime(startText, out var start)) start = TimeSpan.Zero;
        if (!TryParseTime(endText, out var end)) end = new TimeSpan(23, 59, 59);
        return start <= end ? now >= start && now <= end : now >= start || now <= end;
    }

    private static bool TryParseTime(string? text, out TimeSpan value) =>
        TimeSpan.TryParseExact(text, ["h\\:mm", "hh\\:mm"], CultureInfo.InvariantCulture, out value);

    private static int Clamp(int value, int min, int max) => Math.Min(max, Math.Max(min, value));

    internal static string ResolveTerritoryName(uint territoryId)
    {
        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        var row = sheet?.GetRowOrDefault(territoryId);
        return row?.PlaceName.Value.Name.ToString() ?? territoryId.ToString();
    }
}

public sealed record GameSnapshot(
    uint TerritoryId,
    string TerritoryName,
    uint JobId,
    string JobAbbr,
    uint WorldId,
    string WorldName,
    string HomeWorldName,
    DateTime Now,
    HashSet<ActivityFlag> Activities)
{
    public static GameSnapshot Capture()
    {
        var flags = new HashSet<ActivityFlag>();
        if (Plugin.Condition[ConditionFlag.BoundByDuty])
        {
            flags.Add(ActivityFlag.InDuty);
            flags.Add(ActivityFlag.BoundByDuty);
        }
        if (Plugin.Condition[ConditionFlag.InCombat]) flags.Add(ActivityFlag.InCombat);
        if (Plugin.Condition[ConditionFlag.Crafting]) flags.Add(ActivityFlag.Crafting);
        if (Plugin.Condition[ConditionFlag.Gathering]) flags.Add(ActivityFlag.Gathering);
        if (Plugin.Condition[ConditionFlag.Mounted]) flags.Add(ActivityFlag.Mounted);
        if (Plugin.Condition[ConditionFlag.InFlight]) flags.Add(ActivityFlag.Flying);
        if (Plugin.Condition[ConditionFlag.Swimming]) flags.Add(ActivityFlag.Swimming);
        if (Plugin.Condition[ConditionFlag.WatchingCutscene] || Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent])
            flags.Add(ActivityFlag.WatchingCutscene);
        if (Plugin.Condition[ConditionFlag.Unconscious]) flags.Add(ActivityFlag.Dead);
        if (Plugin.PartyList.Length > 0) flags.Add(ActivityFlag.InParty);

        var ps = Plugin.PlayerState;
        var territoryId = Plugin.ClientState.TerritoryType;
        var job = ps.IsLoaded ? ps.ClassJob : default;
        var world = ps.IsLoaded ? ps.CurrentWorld : default;
        var home = ps.IsLoaded ? ps.HomeWorld : default;

        return new GameSnapshot(
            territoryId,
            RuleEngine.ResolveTerritoryName(territoryId),
            job.RowId,
            job.IsValid ? job.Value.Abbreviation.ToString() : "",
            world.RowId,
            world.IsValid ? world.Value.Name.ToString() : "",
            home.IsValid ? home.Value.Name.ToString() : "",
            DateTime.Now,
            flags);
    }
}
