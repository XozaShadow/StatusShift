using System;

namespace StatusShift;

internal static class ZoneGroups
{
    public static readonly string[] Names =
    [
        "Apartment", "Aquatic", "City", "Duty", "House", "Inn",
        "Overworld", "Residential Area", "Sanctuary",
    ];

    public static bool Matches(string value, GameSnapshot snap)
    {
        value = (value ?? string.Empty).Trim();
        if (value.Length == 0) return true;
        if (value.Equals("Duty", StringComparison.OrdinalIgnoreCase))
            return snap.Activities.Contains(ActivityFlag.InDuty);
        if (value.Equals("House", StringComparison.OrdinalIgnoreCase))
            return snap.Housing.Kind == ResidenceKind.House;
        if (value.Equals("Apartment", StringComparison.OrdinalIgnoreCase))
            return snap.Housing.Kind == ResidenceKind.Apartment;
        if (value.Equals("Residential Area", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Residence", StringComparison.OrdinalIgnoreCase))
            return snap.InResidence || snap.Activities.Contains(ActivityFlag.InResidence);
        if (value.Equals("Inn", StringComparison.OrdinalIgnoreCase))
            return ContainsAny(snap, "Inn", "Inn Room", "The Hourglass", "Mizzenmast", "Pendants", "Cloud Nine", "Andron");
        if (value.Equals("City", StringComparison.OrdinalIgnoreCase))
            return ContainsAny(snap, "Limsa", "Ul'dah", "Gridania", "Ishgard", "Kugane", "Crystarium", "Old Sharlayan", "Tuliyollal", "Solution Nine", "Foundation", "Idyllshire", "Rhalgr", "Radz-at-Han");
        if (value.Equals("Aquatic", StringComparison.OrdinalIgnoreCase))
            return snap.Activities.Contains(ActivityFlag.Swimming) || snap.Activities.Contains(ActivityFlag.Diving)
                   || ContainsAny(snap, "Ocean", "Deep Dungeon", "The Sirensong", "Limsa Lominsa Lower");
        if (value.Equals("Sanctuary", StringComparison.OrdinalIgnoreCase))
            return snap.Activities.Contains(ActivityFlag.InSanctuary);
        if (value.Equals("Overworld", StringComparison.OrdinalIgnoreCase))
            return !snap.Activities.Contains(ActivityFlag.InDuty) && !snap.InResidence;
        return snap.ZoneGroupName.Contains(value, StringComparison.OrdinalIgnoreCase)
               || snap.TerritoryName.Contains(value, StringComparison.OrdinalIgnoreCase)
               || snap.RegionName.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAny(GameSnapshot snap, params string[] needles)
    {
        foreach (var n in needles)
        {
            if (snap.TerritoryName.Contains(n, StringComparison.OrdinalIgnoreCase)
                || snap.ZoneGroupName.Contains(n, StringComparison.OrdinalIgnoreCase)
                || snap.RegionName.Contains(n, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
