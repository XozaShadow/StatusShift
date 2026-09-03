using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

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
