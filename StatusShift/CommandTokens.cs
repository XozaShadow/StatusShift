using System;

namespace StatusShift;

internal static class CommandTokens
{
    public static string Resolve(string command, GameSnapshot snap, LiveLook look)
    {
        if (string.IsNullOrWhiteSpace(command)) return command;
        var targeter = look.TargeterName;
        var teller = ChatWatch.LastTellFrom;
        var target = Plugin.TargetManager.Target?.Name.TextValue ?? string.Empty;
        return command
            .Replace("{zone}", snap.TerritoryName, StringComparison.OrdinalIgnoreCase)
            .Replace("{region}", snap.RegionName, StringComparison.OrdinalIgnoreCase)
            .Replace("{job}", snap.JobAbbr, StringComparison.OrdinalIgnoreCase)
            .Replace("{world}", snap.WorldName, StringComparison.OrdinalIgnoreCase)
            .Replace("{home}", snap.HomeWorldName, StringComparison.OrdinalIgnoreCase)
            .Replace("{dc}", snap.DataCenterName, StringComparison.OrdinalIgnoreCase)
            .Replace("{ward}", snap.Housing.Ward.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{plot}", snap.Housing.Plot.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{targeter}", targeter, StringComparison.OrdinalIgnoreCase)
            .Replace("{teller}", teller, StringComparison.OrdinalIgnoreCase)
            .Replace("{target}", target, StringComparison.OrdinalIgnoreCase)
            .Replace("{time}", DateTime.Now.ToString("HH:mm"), StringComparison.OrdinalIgnoreCase);
    }
}
