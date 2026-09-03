using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lumina.Excel.Sheets;

namespace StatusShift;

internal sealed partial class RuleEngine(Configuration config)
{
    public StatusRule? FindMatch()
    {
        if (!Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded)
            return null;

        var ctx = Snapshot();
        if (Skipped(ctx, out _))
            return null;

        return config.Rules
            .Where(r => r.Enabled)
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault(r => Matches(r, ctx) && ChipsOk(r, ctx));
    }

    public string ResolveComment(StatusRule rule)
    {
        var snap = GameSnapshot.Capture();
        var time = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
        return rule.SearchComment
            .Replace("{zone}", snap.TerritoryName, StringComparison.OrdinalIgnoreCase)
            .Replace("{region}", snap.RegionName, StringComparison.OrdinalIgnoreCase)
            .Replace("{job}", snap.JobAbbr, StringComparison.OrdinalIgnoreCase)
            .Replace("{world}", snap.WorldName, StringComparison.OrdinalIgnoreCase)
            .Replace("{home}", snap.HomeWorldName, StringComparison.OrdinalIgnoreCase)
            .Replace("{ward}", snap.Housing.Ward.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{plot}", snap.Housing.Plot.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{time}", time, StringComparison.OrdinalIgnoreCase);
    }

    public GameSnapshot Snapshot() => GameSnapshot.Capture();

    private static bool Matches(StatusRule rule, GameSnapshot ctx)
    {
        var playerName = Plugin.ObjectTable.LocalPlayer?.Name.TextValue ?? string.Empty;
        if (!StatusRule.MatchesCharacter(rule.CharacterFilter, playerName, ctx.WorldName))
            return false;
        if (!ScheduleMatches(rule, ctx.Now))
            return false;
        if (!WorldMatches(rule, ctx))
            return false;
        if (!LocationMatches(rule, ctx))
            return false;
        if (!StatesMatch(rule, ctx))
            return false;

        if ((rule.JobIds.Count > 0 || rule.JobAbbrs.Count > 0)
            && !rule.JobIds.Contains(ctx.JobId)
            && !rule.JobAbbrs.Any(a => a.Equals(ctx.JobAbbr, StringComparison.OrdinalIgnoreCase)))
            return false;

        return true;
    }

    private static bool StatesMatch(StatusRule rule, GameSnapshot ctx)
    {
        if (rule.States.Count == 0)
        {
            if (rule.Activities.Count > 0 && !rule.Activities.All(ctx.Activities.Contains))
                return false;
            return true;
        }

        var yes = rule.States.Where(s => s.Op == MatchOp.Yes).ToList();
        var no = rule.States.Where(s => s.Op == MatchOp.No).ToList();

        foreach (var filter in no)
        {
            if (ctx.Activities.Contains(filter.Flag))
                return false;
        }

        if (yes.Count == 0)
            return true;

        if (rule.StateMatch == StateCombine.All)
            return yes.All(s => ctx.Activities.Contains(s.Flag));
        return yes.Any(s => ctx.Activities.Contains(s.Flag));
    }

    private static bool WorldMatches(StatusRule rule, GameSnapshot ctx)
    {
        var hasIds = rule.WorldIds.Count > 0;
        var hasNames = rule.WorldNames.Count > 0;
        var filter = rule.WorldFilter?.Trim() ?? string.Empty;
        if (!hasIds && !hasNames && string.IsNullOrEmpty(filter))
            return true;

        if (hasIds && rule.WorldIds.Contains(ctx.WorldId))
            return true;
        if (hasNames && rule.WorldNames.Any(n => ctx.WorldName.Equals(n, StringComparison.OrdinalIgnoreCase)
                                                 || ctx.WorldName.Contains(n, StringComparison.OrdinalIgnoreCase)))
            return true;
        if (!string.IsNullOrEmpty(filter))
        {
            if (uint.TryParse(filter, out var id) && id == ctx.WorldId)
                return true;
            if (ctx.WorldName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool LocationMatches(StatusRule rule, GameSnapshot ctx)
    {
        var loc = rule.Location ?? new LocationFilter();
        var value = loc.Value?.Trim() ?? string.Empty;

        switch (loc.Kind)
        {
            case LocationKind.Any:
            case LocationKind.World:
                break;
            case LocationKind.TerritoryId:
                if (!uint.TryParse(value, out var tid) || tid != ctx.TerritoryId)
                    return false;
                break;
            case LocationKind.ZoneName:
                if (string.IsNullOrEmpty(value) || !ctx.TerritoryName.Contains(value, StringComparison.OrdinalIgnoreCase))
                    return false;
                break;
            case LocationKind.Region:
                if (string.IsNullOrEmpty(value) || !ctx.RegionName.Contains(value, StringComparison.OrdinalIgnoreCase))
                    return false;
                break;
            case LocationKind.ZoneGroup:
                if (string.IsNullOrEmpty(value) || !ctx.ZoneGroupName.Contains(value, StringComparison.OrdinalIgnoreCase))
                    return false;
                break;
            case LocationKind.Residence:
                if (!ResidenceMatches(loc, ctx.Housing))
                    return false;
                break;
        }

        if (rule.TerritoryIds.Count > 0 && !rule.TerritoryIds.Contains(ctx.TerritoryId))
            return false;

        if (rule.TerritoryNameContains.Count > 0 &&
            !rule.TerritoryNameContains.Any(n => ctx.TerritoryName.Contains(n, StringComparison.OrdinalIgnoreCase)))
            return false;

        return true;
    }

    private static bool ResidenceMatches(LocationFilter loc, HousingAddress here)
    {
        if (here.Kind == ResidenceKind.None || here.Ward <= 0)
            return false;
        if (loc.ResidenceKind != ResidenceKind.None && loc.ResidenceKind != here.Kind)
            return false;
        if (!string.IsNullOrWhiteSpace(loc.District)
            && !here.District.Contains(loc.District, StringComparison.OrdinalIgnoreCase))
            return false;
        if (loc.Ward > 0 && loc.Ward != here.Ward)
            return false;
        if (loc.ResidenceKind == ResidenceKind.House && loc.Plot > 0 && loc.Plot != here.Plot)
            return false;
        if (loc.ResidenceKind == ResidenceKind.Apartment && loc.Apartment > 0 && loc.Apartment != here.Apartment)
            return false;
        if (loc.Subdivision && !here.Subdivision)
            return false;
        return true;
    }

    private static bool ScheduleMatches(StatusRule rule, DateTime now)
    {
        var sched = rule.Schedule ??= new RuleSchedule();
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
        if (DateTime.TryParse(sched.DateStart, out var start) && today < start.Date) return false;
        if (DateTime.TryParse(sched.DateEnd, out var end) && today > end.Date) return false;
        return true;
    }

    private static bool TimeMatches(RuleSchedule sched, TimeSpan now)
    {
        if (sched.AllDay) return true;
        var start = new TimeSpan(Clamp(sched.StartHour, 0, 23), Clamp(sched.StartMinute, 0, 59), 0);
        var end = new TimeSpan(Clamp(sched.EndHour, 0, 23), Clamp(sched.EndMinute, 0, 59), 0);
        return start <= end ? now >= start && now <= end : now >= start || now <= end;
    }

    private static bool InTimeWindow(string? startText, string? endText, TimeSpan now)
    {
        if (string.IsNullOrWhiteSpace(startText) && string.IsNullOrWhiteSpace(endText)) return true;
        if (!TryParseTime(startText, out var start)) start = TimeSpan.Zero;
        if (!TryParseTime(endText, out var end)) end = new TimeSpan(23, 59, 59);
        return start <= end ? now >= start && now <= end : now >= start || now <= end;
    }

    private static bool TryParseTime(string? text, out TimeSpan value) =>
        TimeSpan.TryParseExact(text, ["h\\:mm", "hh\\:mm"], CultureInfo.InvariantCulture, out value);

    private static int Clamp(int value, int min, int max) => Math.Min(max, Math.Max(min, value));

    internal static (string Name, string Region, string Group) ResolvePlace(uint territoryId)
    {
        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        var row = sheet?.GetRowOrDefault(territoryId);
        if (row is null)
            return (territoryId.ToString(), string.Empty, string.Empty);

        return (
            row.Value.PlaceName.Value.Name.ToString(),
            row.Value.PlaceNameRegion.Value.Name.ToString(),
            row.Value.PlaceNameZone.Value.Name.ToString());
    }
}
