using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace StatusShift;

internal static class RuleStore
{
    private const string FileName = "rules.json";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private static string FilePath =>
        Path.Combine(Plugin.PluginInterface.GetPluginConfigDirectory(), FileName);

    public static void Load(Configuration cfg)
    {
        try
        {
            Directory.CreateDirectory(Plugin.PluginInterface.GetPluginConfigDirectory());
            if (File.Exists(FilePath))
            {
                var rules = JsonSerializer.Deserialize<List<StatusRule>>(File.ReadAllText(FilePath), JsonOpts);
                if (rules is not null)
                {
                    cfg.Rules = rules;
                    Plugin.Log.Info("Loaded {Count} rules from {Path}", rules.Count, FilePath);
                    return;
                }
            }

            Save(cfg);
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "Failed to load {Path}", FilePath);
        }
    }

    public static void Save(Configuration cfg)
    {
        try
        {
            Directory.CreateDirectory(Plugin.PluginInterface.GetPluginConfigDirectory());
            File.WriteAllText(FilePath, JsonSerializer.Serialize(cfg.Rules ?? [], JsonOpts));
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "Failed to save {Path}", FilePath);
        }
    }
}
