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

    public static string ConfigDirectory => Plugin.PluginInterface.GetPluginConfigDirectory();
    public static string FilePath => Path.Combine(ConfigDirectory, FileName);

    public static void Load(Configuration cfg)
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory);
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
            Directory.CreateDirectory(ConfigDirectory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(cfg.Rules ?? [], JsonOpts));
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "Failed to save {Path}", FilePath);
        }
    }

    public static string ArchiveAndWipe(Configuration cfg)
    {
        Directory.CreateDirectory(ConfigDirectory);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var archive = Path.Combine(ConfigDirectory, $"rules-archive-{stamp}.json");
        if (File.Exists(FilePath))
            File.Copy(FilePath, archive, overwrite: true);
        else
            File.WriteAllText(archive, JsonSerializer.Serialize(cfg.Rules ?? [], JsonOpts));
        cfg.Rules = [];
        Save(cfg);
        return archive;
    }
}
