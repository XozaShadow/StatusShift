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

    public string Summary => Kind switch
    {
        ResidenceKind.House => $"{District}  W{Ward}{(Subdivision ? " sub" : "")}  P{Plot}",
        ResidenceKind.Apartment => $"{District}  W{Ward}{(Subdivision ? " sub" : "")}  Apt {Apartment}",
        _ => "not in housing",
    };
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
