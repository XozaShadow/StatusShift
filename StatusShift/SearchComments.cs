using System;
using System.Linq;

namespace StatusShift;

internal static class SearchComments
{
    public const int MaxLength = 60;

    public static string Clamp(string? text)
    {
        text ??= string.Empty;
        return text.Length <= MaxLength ? text : text[..MaxLength];
    }

    public static CommentTemplate? Find(Configuration cfg, string idOrTitle)
    {
        if (string.IsNullOrWhiteSpace(idOrTitle)) return null;
        cfg.MigrateCommentTemplates();
        return cfg.NamedTemplates.FirstOrDefault(t =>
            t.Id.Equals(idOrTitle, StringComparison.OrdinalIgnoreCase)
            || t.Title.Equals(idOrTitle, StringComparison.OrdinalIgnoreCase));
    }

    public static string ResolveBody(Configuration cfg, StatusRule rule, bool fallback)
    {
        var id = fallback ? rule.FallbackCommentTemplate : rule.CommentTemplate;
        var tmpl = Find(cfg, id);
        if (tmpl is not null) return Clamp(tmpl.Body);
        return Clamp(fallback ? rule.FallbackComment : rule.SearchComment);
    }
}

[Serializable]
public class CommentTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "Untitled";
    public string Body { get; set; } = string.Empty;
}

public enum RuleTestStage
{
    Full = 0,
    FromSchedule = 1,
    FromConditions = 2,
    ThenOnly = 3,
    RevertNow = 4,
}
