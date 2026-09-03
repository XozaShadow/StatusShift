using System.Collections.Generic;

namespace StatusShift;

public partial class StatusRule
{
    public List<RuleChip> AndChips { get; set; } = [];
    public List<RuleChip> OrChips { get; set; } = [];
}
