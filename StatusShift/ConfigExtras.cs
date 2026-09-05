using System;
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
    public List<CommentTemplate> NamedTemplates { get; set; } = [];

    public void MigrateCommentTemplates()
    {
        if (CommentTemplates.Count == 0) return;
        foreach (var raw in CommentTemplates)
        {
            var body = SearchComments.Clamp(raw);
            if (string.IsNullOrWhiteSpace(body)) continue;
            if (NamedTemplates.Exists(t => t.Body.Equals(body, StringComparison.Ordinal))) continue;
            NamedTemplates.Add(new CommentTemplate
            {
                Title = body.Length > 18 ? body[..18] + "…" : body,
                Body = body,
            });
        }
        CommentTemplates.Clear();
    }
}
