using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StatusShift;

public enum ChipKind
{
    World = 0,
    Zone = 1,
    Region = 2,
    ZoneType = 3,
    Residence = 4,
    Apartment = 5,
    Duty = 6,
    Job = 7,
    NearbyPlayer = 8,
    Emote = 9,
    Mount = 10,
    State = 11,
    Contains = 12,
    Regex = 13,
}

[Serializable]
public class RuleChip
{
    public ChipKind Kind { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Label => Kind switch
    {
        ChipKind.World => $"W {Value}",
        ChipKind.Zone => $"Z {Value}",
        ChipKind.Region => $"R {Value}",
        ChipKind.ZoneType => $"T {Value}",
        ChipKind.Residence => $"House {Value}",
        ChipKind.Apartment => $"Apt {Value}",
        ChipKind.Duty => $"Duty {Value}",
        ChipKind.Job => $"Job {Value}",
        ChipKind.NearbyPlayer => $"Near {Value}",
        ChipKind.Emote => $"Emote {Value}",
        ChipKind.Mount => $"Mount {Value}",
        ChipKind.State => Value,
        ChipKind.Contains => $"~ {Value}",
        ChipKind.Regex => $"/{Value}/",
        _ => Value,
    };
}

internal static class ChipShare
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static string Encode(StatusRule rule)
    {
        var json = JsonSerializer.Serialize(rule, Json);
        var raw = Encoding.UTF8.GetBytes(json);
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize, true))
            gz.Write(raw, 0, raw.Length);
        return "SS1." + Convert.ToBase64String(ms.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static bool TryDecode(string text, out StatusRule? rule, out string error)
    {
        rule = null;
        error = string.Empty;
        text = (text ?? string.Empty).Trim();
        if (text.StartsWith('{') || text.StartsWith('['))
        {
            try
            {
                if (text.StartsWith('['))
                {
                    var many = JsonSerializer.Deserialize<List<StatusRule>>(text, Json);
                    rule = many is { Count: > 0 } ? many[0] : null;
                }
                else rule = JsonSerializer.Deserialize<StatusRule>(text, Json);
                if (rule is null) { error = "Empty share."; return false; }
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        if (text.StartsWith("SS1.", StringComparison.OrdinalIgnoreCase))
            text = text[4..];
        text = text.Replace('-', '+').Replace('_', '/');
        while (text.Length % 4 != 0) text += "=";
        try
        {
            var bytes = Convert.FromBase64String(text);
            using var input = new MemoryStream(bytes);
            using var gz = new GZipStream(input, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            gz.CopyTo(outMs);
            rule = JsonSerializer.Deserialize<StatusRule>(Encoding.UTF8.GetString(outMs.ToArray()), Json);
            if (rule is null) { error = "Empty share."; return false; }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

internal static class ChipEval
{
    public static bool AllMatch(List<RuleChip> chips, GameSnapshot snap)
    {
        if (chips.Count == 0) return true;
        foreach (var chip in chips)
        {
            if (!Matches(chip, snap)) return false;
        }
        return true;
    }

    public static bool AnyMatch(List<RuleChip> chips, GameSnapshot snap)
    {
        if (chips.Count == 0) return true;
        foreach (var chip in chips)
        {
            if (Matches(chip, snap)) return true;
        }
        return false;
    }

    public static bool Matches(RuleChip chip, GameSnapshot snap)
    {
        var v = chip.Value?.Trim() ?? string.Empty;
        if (v.Length == 0) return true;
        return chip.Kind switch
        {
            ChipKind.World => snap.WorldName.Equals(v, StringComparison.OrdinalIgnoreCase)
                              || snap.WorldName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Zone => snap.TerritoryName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Region => snap.RegionName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.ZoneType => snap.ZoneGroupName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Residence => snap.InResidence && snap.Housing.Summary.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Apartment => snap.Housing.Kind == ResidenceKind.Apartment
                                  && snap.Housing.Summary.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Duty => snap.Activities.Contains(ActivityFlag.InDuty)
                             && (v.Equals("any", StringComparison.OrdinalIgnoreCase)
                                 || snap.TerritoryName.Contains(v, StringComparison.OrdinalIgnoreCase)),
            ChipKind.Job => snap.JobAbbr.Equals(v, StringComparison.OrdinalIgnoreCase)
                            || snap.JobAbbr.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.NearbyPlayer => snap.NearbyPlayers.Exists(n =>
                n.Equals(v, StringComparison.OrdinalIgnoreCase)
                || n.StartsWith(v + "@", StringComparison.OrdinalIgnoreCase)
                || n.Contains(v, StringComparison.OrdinalIgnoreCase)),
            ChipKind.Emote => snap.EmoteName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Mount => snap.Mounted && snap.MountName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.State => Enum.TryParse<ActivityFlag>(v, true, out var flag) && snap.Activities.Contains(flag),
            ChipKind.Contains => snap.TerritoryName.Contains(v, StringComparison.OrdinalIgnoreCase)
                                 || snap.RegionName.Contains(v, StringComparison.OrdinalIgnoreCase)
                                 || snap.ZoneGroupName.Contains(v, StringComparison.OrdinalIgnoreCase)
                                 || snap.Housing.Summary.Contains(v, StringComparison.OrdinalIgnoreCase)
                                 || snap.MountName.Contains(v, StringComparison.OrdinalIgnoreCase)
                                 || snap.EmoteName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Regex => SafeRegex(v, snap),
            _ => true,
        };
    }

    private static bool SafeRegex(string pattern, GameSnapshot snap)
    {
        try
        {
            var rx = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(40));
            return rx.IsMatch(snap.TerritoryName)
                   || rx.IsMatch(snap.RegionName)
                   || rx.IsMatch(snap.ZoneGroupName)
                   || rx.IsMatch(snap.Housing.Summary)
                   || rx.IsMatch(snap.WorldName);
        }
        catch
        {
            return false;
        }
    }
}
