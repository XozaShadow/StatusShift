using System.Collections.Generic;

namespace StatusShift;

public partial class Configuration
{
    public bool ConfirmPing { get; set; } = true;
    public bool NotifyWithToast { get; set; }
    public int NotifySound { get; set; } = 1;
    public float NearbyRange { get; set; } = 80f;
    public bool SkipWhileTargeted { get; set; }
    public bool SkipWhileEmoting { get; set; }
    public bool ShowAutoApply { get; set; }
    public List<string> CommentTemplates { get; set; } = [];
}
