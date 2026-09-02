using System.Linq;
using System.Text;

namespace StatusShift;

internal sealed partial class RuleEngine
{
    public string Explain()
    {
        if (!Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded)
            return "Not logged in.";

        var ctx = Snapshot();
        if (Skipped(ctx, out var why))
            return $"Skipped: {why}";

        var sb = new StringBuilder();
        StatusRule? winner = null;
        foreach (var rule in config.Rules.OrderByDescending(r => r.Priority))
        {
            if (!rule.Enabled)
                continue;
            if (Matches(rule, ctx))
            {
                winner = rule;
                break;
            }
        }

        if (winner is null)
            return "No enabled rule matches.";

        sb.Append($"P{winner.Priority} {winner.Name}");
        sb.Append(ScheduleMatches(winner, ctx.Now) ? " · schedule yes" : " · schedule no");
        sb.Append(WorldMatches(winner, ctx) && LocationMatches(winner, ctx) ? " · loc yes" : " · loc no");
        sb.Append(winner.JobIds.Count == 0 && winner.JobAbbrs.Count == 0 ? " · job any" : " · job yes");
        var stateN = winner.States.Count(s => s.Op != MatchOp.Any);
        sb.Append(stateN == 0 ? " · state any" : winner.StateMatch == StateCombine.All ? $" · state AND x{stateN}" : $" · state OR x{stateN}");
        if (winner.HasCharacterFilter)
            sb.Append(" · character yes");
        return sb.ToString();
    }

    public bool Skipped(GameSnapshot ctx, out string reason)
    {
        reason = string.Empty;
        if (config.SkipWhileCutscene && ctx.Activities.Contains(ActivityFlag.WatchingCutscene))
        { reason = "cutscene"; return true; }
        if (config.SkipWhileDead && ctx.Activities.Contains(ActivityFlag.Dead))
        { reason = "dead"; return true; }
        if (config.SkipWhileDuty && ctx.Activities.Contains(ActivityFlag.InDuty))
        { reason = "duty"; return true; }
        if (config.SkipWhileCombat && ctx.Activities.Contains(ActivityFlag.InCombat))
        { reason = "combat"; return true; }
        if (config.SkipWhileBetweenAreas && ctx.Activities.Contains(ActivityFlag.BetweenAreas))
        { reason = "between areas"; return true; }
        if (config.SkipWhileOccupied && ctx.Activities.Contains(ActivityFlag.Occupied))
        { reason = "occupied"; return true; }
        if (config.SkipWhileTargetingPlayer && ctx.Activities.Contains(ActivityFlag.TargetingPlayer))
        { reason = "targeting player"; return true; }
        return false;
    }
}
