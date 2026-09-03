using System;
using System.Collections.Generic;
using System.Text.Json;
using StatusShift.Windows;

namespace StatusShift;

public sealed partial class Plugin
{
    public static string AppVersion
    {
        get
        {
            var v = typeof(Plugin).Assembly.GetName().Version;
            return v is null ? "0.1.5.0" : $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
        }
    }

    public List<StatusRule> CurrentMatches() => engine.FindMatches();

    public void OpenRule(string id) => mainWindow.OpenRule(id);

    public void DuplicateRule(StatusRule rule)
    {
        var json = JsonSerializer.Serialize(rule);
        var copy = JsonSerializer.Deserialize<StatusRule>(json);
        if (copy is null) return;
        copy.Id = Guid.NewGuid().ToString("N");
        copy.Name = rule.Name + " (copy)";
        copy.Enabled = false;
        copy.ClearLegacy();
        Configuration.Rules.Add(copy);
        Configuration.Save();
    }

    public void ShowSelector(List<StatusRule> matches) => selectorWindow.Show(matches);
}
