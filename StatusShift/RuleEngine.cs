using System;
using System.Globalization;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Lumina.Excel.Sheets;

namespace StatusShift;

internal sealed class RuleEngine(Configuration config)
{
    public StatusRule? FindMatch()
    {
        if (!Plugin.ClientState.IsLoggedIn || Plugin.ClientState.LocalPlayer is null)
            return null;

        var territoryId = Plugin.ClientState.TerritoryType;
        var territoryName = ResolveTerritoryName(territoryId);
        var jobId = Plugin.ClientState.LocalPlayer.ClassJob.RowId;
        var inDuty = Plugin.Condition[ConditionFlag.BoundByDuty];
        var now = DateTime.Now;

        return config.Rules
            .Where(r => r.Enabled)
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault(r => Matches(r, territoryId, territoryName, jobId, inDuty, now));
    }

    public string ResolveComment(StatusRule rule)
    {
        var player = Plugin.ClientState.LocalPlayer;
        var territoryName = ResolveTerritoryName(Plugin.ClientState.TerritoryType);
        var job = player?.ClassJob.Value.Abbreviation.ToString() ?? "";
        var world = player?.HomeWorld.Value.Name.ToString() ?? "";
        var time = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);

        return rule.SearchComment
            .Replace("{zone}", territoryName, StringComparison.OrdinalIgnoreCase)
            .Replace("{job}", job, StringComparison.OrdinalIgnoreCase)
            .Replace("{world}", world, StringComparison.OrdinalIgnoreCase)
            .Replace("{time}", time, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Matches(StatusRule rule, ushort territoryId, string territoryName, uint jobId, bool inDuty, DateTime now)
    {
        if (rule.TerritoryIds.Count > 0 && !rule.TerritoryIds.Contains(territoryId))
            return false;

        if (rule.TerritoryNameContains.Count > 0 &&
            !rule.TerritoryNameContains.Any(n => territoryName.Contains(n, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (rule.JobIds.Count > 0 && !rule.JobIds.Contains(jobId))
            return false;

        if (rule.Days.Count > 0 && !rule.Days.Contains(now.DayOfWeek))
            return false;

        if (rule.InDuty is { } dutyRequired && dutyRequired != inDuty)
            return false;

        if (!InTimeWindow(rule.TimeStart, rule.TimeEnd, now.TimeOfDay))
            return false;

        return true;
    }

    private static bool InTimeWindow(string? startText, string? endText, TimeSpan now)
    {
        if (string.IsNullOrWhiteSpace(startText) && string.IsNullOrWhiteSpace(endText))
            return true;

        if (!TryParseTime(startText, out var start))
            start = TimeSpan.Zero;
        if (!TryParseTime(endText, out var end))
            end = new TimeSpan(23, 59, 59);

        return start <= end
            ? now >= start && now <= end
            : now >= start || now <= end;
    }

    private static bool TryParseTime(string? text, out TimeSpan value) =>
        TimeSpan.TryParseExact(text, ["h\\:mm", "hh\\:mm"], CultureInfo.InvariantCulture, out value);

    private static string ResolveTerritoryName(ushort territoryId)
    {
        var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        var row = sheet?.GetRowOrDefault(territoryId);
        return row?.PlaceName.Value.Name.ToString() ?? territoryId.ToString();
    }
}
