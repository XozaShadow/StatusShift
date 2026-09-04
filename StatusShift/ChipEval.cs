using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StatusShift;

internal static class ChipEval
{
    public static bool AllMatch(List<RuleChip> chips, GameSnapshot snap, LiveLook look)
    {
        if (chips is null || chips.Count == 0) return true;
        foreach (var chip in chips)
        {
            if (!Matches(chip, snap, look)) return false;
        }
        return true;
    }

    public static bool AnyMatch(List<RuleChip> chips, GameSnapshot snap, LiveLook look)
    {
        if (chips is null || chips.Count == 0) return true;
        foreach (var chip in chips)
        {
            if (Matches(chip, snap, look)) return true;
        }
        return false;
    }

    public static bool NoneMatch(List<RuleChip> chips, GameSnapshot snap, LiveLook look)
    {
        if (chips is null || chips.Count == 0) return true;
        foreach (var chip in chips)
        {
            if (Matches(chip, snap, look)) return false;
        }
        return true;
    }

    public static bool Matches(RuleChip chip, GameSnapshot snap, LiveLook look)
    {
        var v = chip.Value?.Trim() ?? string.Empty;
        if (v.Length == 0) return true;
        return chip.Kind switch
        {
            ChipKind.World => snap.WorldName.Equals(v, StringComparison.OrdinalIgnoreCase)
                              || snap.WorldName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Zone => snap.TerritoryName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Region => snap.RegionName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.ZoneType => snap.ZoneTypeName.Contains(v, StringComparison.OrdinalIgnoreCase)
                                 || snap.ZoneGroupName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Residence => snap.InResidence && snap.Housing.Summary.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Apartment => snap.Housing.Kind == ResidenceKind.Apartment
                                  && snap.Housing.Summary.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Duty => snap.Activities.Contains(ActivityFlag.InDuty)
                             && (v.Equals("any", StringComparison.OrdinalIgnoreCase)
                                 || snap.TerritoryName.Contains(v, StringComparison.OrdinalIgnoreCase)),
            ChipKind.Job => JobRoles.Matches(v, snap.JobAbbr)
                            || snap.JobAbbr.Equals(v, StringComparison.OrdinalIgnoreCase)
                            || snap.JobAbbr.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.NearbyPlayer => look.NearbyPlayers.Exists(n =>
                n.Equals(v, StringComparison.OrdinalIgnoreCase)
                || n.StartsWith(v + "@", StringComparison.OrdinalIgnoreCase)
                || n.Contains(v, StringComparison.OrdinalIgnoreCase)),
            ChipKind.Emote => look.EmoteName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Mount => look.Mounted && look.MountName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.State => Enum.TryParse<ActivityFlag>(v, true, out var flag) && snap.Activities.Contains(flag),
            ChipKind.Contains => snap.TerritoryName.Contains(v, StringComparison.OrdinalIgnoreCase)
                                 || snap.RegionName.Contains(v, StringComparison.OrdinalIgnoreCase)
                                 || snap.ZoneGroupName.Contains(v, StringComparison.OrdinalIgnoreCase)
                                 || snap.Housing.Summary.Contains(v, StringComparison.OrdinalIgnoreCase)
                                 || look.MountName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Regex => SafeRegex(v, snap),
            ChipKind.DataCenter => snap.DataCenterName.Equals(v, StringComparison.OrdinalIgnoreCase)
                                   || snap.DataCenterName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Property => PropertyMatch(v, snap),
            ChipKind.TellFrom => ChatWatch.TellFrom(v),
            ChipKind.Chat => ChatWatch.ChatMatches(v),
            ChipKind.Accessory => look.AccessoryName.Contains(v, StringComparison.OrdinalIgnoreCase),
            ChipKind.Status => look.Statuses.Exists(s => s.Contains(v, StringComparison.OrdinalIgnoreCase)),
            ChipKind.Role => JobRoles.Matches(v, snap.JobAbbr),
            _ => true,
        };
    }

    private static bool PropertyMatch(string v, GameSnapshot snap)
    {
        if (HousingChip.TryParse(v, out _, out _, out _, out _, out _, out _))
            return HousingChip.Matches(v, snap.Housing);
        if (!snap.InResidence) return false;
        if (v.Equals("any", StringComparison.OrdinalIgnoreCase)) return true;
        if (v.Equals("House", StringComparison.OrdinalIgnoreCase) || v.Equals("Residence", StringComparison.OrdinalIgnoreCase))
            return snap.Housing.Kind == ResidenceKind.House;
        if (v.Equals("Apartment", StringComparison.OrdinalIgnoreCase))
            return snap.Housing.IsWingApartment;
        if (v.Contains("FC", StringComparison.OrdinalIgnoreCase))
            return snap.Housing.IsFcApartment;
        if (v.Contains("Sub", StringComparison.OrdinalIgnoreCase))
            return snap.Housing.Subdivision;
        return snap.Housing.Summary.Contains(v, StringComparison.OrdinalIgnoreCase);
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
                   || rx.IsMatch(snap.WorldName)
                   || rx.IsMatch(snap.DataCenterName);
        }
        catch
        {
            return false;
        }
    }
}
