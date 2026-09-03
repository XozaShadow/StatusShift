using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StatusShift;

public partial class StatusRule
{
    public List<RuleChip> AndChips { get; set; } = [];
    public List<RuleChip> OrChips { get; set; } = [];
    public string FallbackCommand { get; set; } = string.Empty;
    public bool NotifyIfNotApplied { get; set; }
    public bool NotifyChat { get; set; } = true;
    public bool NotifyAudible { get; set; }
    public int NotifySound { get; set; } = 1;

    public string CharacterKey
    {
        get
        {
            var filter = (CharacterFilter ?? string.Empty).Trim();
            if (filter.Length == 0) return string.Empty;
            var at = filter.IndexOf('@');
            return at < 0 ? filter : filter[..at].Trim();
        }
    }

    public bool HasLegacy =>
        ((Location?.Kind ?? LocationKind.Any) is not LocationKind.Any and not LocationKind.World)
        || WorldNames.Count > 0
        || WorldIds.Count > 0
        || JobAbbrs.Count > 0
        || JobIds.Count > 0
        || States.Exists(s => s.Op != MatchOp.Any)
        || TerritoryNameContains.Count > 0
        || TerritoryIds.Count > 0;

    public string LegacySummary()
    {
        var sb = new StringBuilder();
        if (WorldNames.Count > 0) sb.Append("Worlds: ").Append(string.Join(", ", WorldNames)).Append('\n');
        var loc = Location;
        if (loc is not null && loc.Kind is not LocationKind.Any and not LocationKind.World)
            sb.Append("Place: ").Append(loc.Kind).Append(' ').Append(loc.Value).Append(' ').Append(loc.District).Append('\n');
        if (TerritoryNameContains.Count > 0) sb.Append("Zones: ").Append(string.Join(", ", TerritoryNameContains)).Append('\n');
        if (JobAbbrs.Count > 0) sb.Append("Jobs: ").Append(string.Join(", ", JobAbbrs)).Append('\n');
        var states = States.Where(s => s.Op != MatchOp.Any).Select(s => $"{s.Op} {s.Flag}");
        if (states.Any()) sb.Append("States: ").Append(string.Join(", ", states)).Append('\n');
        return sb.Length == 0 ? "Older condition fields are set." : sb.ToString().Trim();
    }

    public void ClearLegacy()
    {
        Location = new LocationFilter();
        WorldNames.Clear();
        WorldIds.Clear();
        WorldFilter = string.Empty;
        JobAbbrs.Clear();
        JobIds.Clear();
        States.Clear();
        Activities.Clear();
        TerritoryNameContains.Clear();
        TerritoryIds.Clear();
    }
}
