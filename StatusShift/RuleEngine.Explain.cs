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
            if (!rule.Enabled) continue;
            if (Matches(rule, ctx) && ChipsOk(rule, ctx))
            {
                winner = rule;
                break;
            }
        }

        if (winner is null)
            return "No enabled rule matches.";

        sb.Append($"P{winner.Priority} {winner.Name}");
        sb.Append(ScheduleMatches(winner, ctx.Now) ? " · schedule yes" : " · schedule no");
        var andN = winner.AndChips?.Count ?? 0;
        var orN = winner.OrChips?.Count ?? 0;
        if (andN > 0) sb.Append($" · AND x{andN}");
        if (orN > 0) sb.Append($" · OR x{orN}");
        if (winner.HasCharacterFilter)
            sb.Append(" · character yes");
        return sb.ToString();
    }

    internal static bool ChipsOk(StatusRule rule, GameSnapshot ctx)
    {
        var look = LiveLook.Capture(80f);
        return ChipEval.AllMatch(rule.AndChips ?? [], ctx, look)
               && ChipEval.AnyMatch(rule.OrChips ?? [], ctx, look);
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
