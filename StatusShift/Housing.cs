using System;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace StatusShift;

public enum ResidenceKind
{
    None = 0,
    House = 1,
    Apartment = 2,
}

public readonly record struct HousingAddress(
    ResidenceKind Kind,
    string District,
    int Ward,
    int Plot,
    int Apartment,
    bool Subdivision)
{
    public bool HasAddress => Kind != ResidenceKind.None && Ward > 0;
    public bool IsFcApartment => Kind == ResidenceKind.Apartment && Plot > 0 && Apartment > 0;
    public bool IsWingApartment => Kind == ResidenceKind.Apartment && Apartment > 0 && Plot <= 0;

    public string Summary => Kind switch
    {
        ResidenceKind.House => $"{District}  W{Ward}{(Subdivision ? " sub" : "")}  P{Plot}",
        ResidenceKind.Apartment when Plot > 0 =>
            $"{District}  W{Ward}{(Subdivision ? " sub" : "")}  P{Plot}  Room {Apartment}",
        ResidenceKind.Apartment => $"{District}  W{Ward}{(Subdivision ? " sub" : "")}  Apt {Apartment}",
        _ => "not in housing",
    };
}

internal static class HousingZones
{
    public static readonly string[] Choices =
    [
        "Empyreum",
        "Empyreum (Subdivision)",
        "Mist",
        "Mist (Subdivision)",
        "Shirogane",
        "Shirogane (Subdivision)",
        "The Goblet",
        "The Goblet (Subdivision)",
        "The Lavender Beds",
        "The Lavender Beds (Subdivision)",
    ];

    public static (string District, bool Subdivision) ParseZone(string zone)
    {
        zone = (zone ?? string.Empty).Trim();
        var sub = zone.Contains("Subdivision", StringComparison.OrdinalIgnoreCase);
        var district = zone.Replace(" (Subdivision)", "", StringComparison.OrdinalIgnoreCase).Trim();
        return (district, sub);
    }
}

internal static class HousingChip
{
    public static string Encode(string type, string zone, int ward, int plot, int apt)
    {
        var (district, sub) = HousingZones.ParseZone(zone);
        return type switch
        {
            "Apartment" => $"Apartment|{district}|{ward}|{apt}{(sub ? "|sub" : "")}",
            "FC Apartment" => $"FcApartment|{district}|{ward}|{plot}|{apt}{(sub ? "|sub" : "")}",
            _ => $"House|{district}|{ward}|{plot}{(sub ? "|sub" : "")}",
        };
    }

    public static string FormatLabel(string value)
    {
        if (!TryParse(value, out var type, out var district, out var ward, out var plot, out var apt, out var sub))
            return "House " + value;
        var zone = district + (sub ? " sub" : "");
        return type switch
        {
            "Apartment" => $"Apt {zone} W{ward}" + (apt > 0 ? $" #{apt}" : ""),
            "FcApartment" => $"FC {zone} W{ward} P{plot}" + (apt > 0 ? $" R{apt}" : ""),
            _ => $"House {zone} W{ward}" + (plot > 0 ? $" P{plot}" : ""),
        };
    }

    public static bool TryParse(string value, out string type, out string district, out int ward, out int plot, out int apt, out bool sub)
    {
        type = "House";
        district = string.Empty;
        ward = 0;
        plot = 0;
        apt = 0;
        sub = false;
        var parts = (value ?? string.Empty).Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;
        if (!parts[0].Equals("House", StringComparison.OrdinalIgnoreCase)
            && !parts[0].Equals("Apartment", StringComparison.OrdinalIgnoreCase)
            && !parts[0].Equals("FcApartment", StringComparison.OrdinalIgnoreCase))
            return false;
        type = parts[0];
        district = parts[1];
        if (parts.Length > 2) int.TryParse(parts[2], out ward);
        if (type.Equals("Apartment", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length > 3) int.TryParse(parts[3], out apt);
            if (parts.Length > 4) sub = parts[4].Equals("sub", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            if (parts.Length > 3) int.TryParse(parts[3], out plot);
            if (type.Equals("FcApartment", StringComparison.OrdinalIgnoreCase) && parts.Length > 4)
                int.TryParse(parts[4], out apt);
            sub = parts[^1].Equals("sub", StringComparison.OrdinalIgnoreCase);
        }
        return true;
    }

    public static bool Matches(string value, HousingAddress here)
    {
        if (here.Kind == ResidenceKind.None) return false;
        if (!TryParse(value, out var type, out var district, out var ward, out var plot, out var apt, out var sub))
            return false;
        if (!string.IsNullOrWhiteSpace(district)
            && !here.District.Contains(district, StringComparison.OrdinalIgnoreCase))
            return false;
        if (ward > 0 && here.Ward != ward) return false;
        if (sub && !here.Subdivision) return false;
        if (type.Equals("House", StringComparison.OrdinalIgnoreCase))
        {
            if (here.Kind != ResidenceKind.House) return false;
            if (plot > 0 && here.Plot != plot) return false;
            return true;
        }
        if (type.Equals("Apartment", StringComparison.OrdinalIgnoreCase))
        {
            if (!here.IsWingApartment) return false;
            if (apt > 0 && here.Apartment != apt) return false;
            return true;
        }
        if (here.IsFcApartment)
        {
            if (plot > 0 && here.Plot != plot) return false;
            if (apt > 0 && here.Apartment != apt) return false;
            return true;
        }
        return false;
    }
}

internal static class HousingReader
{
    public static HousingAddress Read(string territoryName)
    {
        var district = ResolveDistrict(territoryName);
        var ward = 0;
        var plot = 0;
        var room = 0;
        var division = 1;

        try
        {
            unsafe
            {
                var h = HousingManager.Instance();
                if (h != null)
                {
                    ward = h->GetCurrentWard() + 1;
                    plot = h->GetCurrentPlot() + 1;
                    room = h->GetCurrentRoom();
                    division = h->GetCurrentDivision();
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Verbose(ex, "HousingManager read failed");
        }

        if (ward is < 1 or > 30)
            return default;

        var subdivision = division == 2 || plot is >= 31 and <= 60;
        if (room > 0 && plot is >= 1 and <= 60)
            return new HousingAddress(ResidenceKind.Apartment, district, ward, plot, room, subdivision);
        if (room > 0)
            return new HousingAddress(ResidenceKind.Apartment, district, ward, 0, room, subdivision);
        if (plot is >= 1 and <= 60)
            return new HousingAddress(ResidenceKind.House, district, ward, plot, 0, plot >= 31 || subdivision);

        if (!string.IsNullOrEmpty(district))
            return new HousingAddress(ResidenceKind.None, district, ward, 0, 0, subdivision);

        return default;
    }

    public static string ResolveDistrict(string territoryName)
    {
        if (string.IsNullOrWhiteSpace(territoryName))
            return string.Empty;
        if (Contains(territoryName, "Mist") || Contains(territoryName, "Topmast"))
            return "Mist";
        if (Contains(territoryName, "Lavender") || Contains(territoryName, "Lily Hills"))
            return "The Lavender Beds";
        if (Contains(territoryName, "Goblet") || Contains(territoryName, "Sultana"))
            return "The Goblet";
        if (Contains(territoryName, "Shirogane") || Contains(territoryName, "Kobai"))
            return "Shirogane";
        if (Contains(territoryName, "Empyreum") || Contains(territoryName, "Ingleside"))
            return "Empyreum";
        return territoryName;
    }

    private static bool Contains(string hay, string needle) =>
        hay.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
