using System;
using System.Collections.Generic;

namespace StatusShift;

internal static class JobRoles
{
    public static readonly string[] Names =
    [
        "Crafter", "DoH", "DoL", "DoM", "DoW", "DPS", "Gatherer",
        "Healer", "Magical Ranged DPS", "Melee DPS", "Physical Ranged DPS", "Tank",
    ];

    private static readonly Dictionary<string, string[]> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tank"] = ["GLA", "PLD", "MRD", "WAR", "DRK", "GNB"],
        ["Healer"] = ["CNJ", "WHM", "SCH", "AST", "SGE"],
        ["Melee DPS"] = ["PGL", "MNK", "LNC", "DRG", "ROG", "NIN", "SAM", "RPR", "VPR"],
        ["Physical Ranged DPS"] = ["ARC", "BRD", "MCH", "DNC"],
        ["Magical Ranged DPS"] = ["THM", "BLM", "ACN", "SMN", "RDM", "BLU", "PCT"],
        ["DPS"] =
        [
            "PGL", "MNK", "LNC", "DRG", "ROG", "NIN", "SAM", "RPR", "VPR",
            "ARC", "BRD", "MCH", "DNC", "THM", "BLM", "ACN", "SMN", "RDM", "BLU", "PCT",
        ],
        ["DoW"] =
        [
            "GLA", "PLD", "MRD", "WAR", "DRK", "GNB",
            "PGL", "MNK", "LNC", "DRG", "ROG", "NIN", "SAM", "RPR", "VPR",
            "ARC", "BRD", "MCH", "DNC",
        ],
        ["DoM"] = ["CNJ", "WHM", "SCH", "AST", "SGE", "THM", "BLM", "ACN", "SMN", "RDM", "BLU", "PCT"],
        ["DoH"] = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL"],
        ["DoL"] = ["MIN", "BTN", "FSH"],
        ["Crafter"] = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL"],
        ["Gatherer"] = ["MIN", "BTN", "FSH"],
    };

    public static bool Matches(string roleOrAbbr, string jobAbbr)
    {
        if (string.IsNullOrWhiteSpace(jobAbbr)) return false;
        if (jobAbbr.Equals(roleOrAbbr, StringComparison.OrdinalIgnoreCase)) return true;
        return Map.TryGetValue(roleOrAbbr.Trim(), out var jobs)
               && Array.Exists(jobs, j => j.Equals(jobAbbr, StringComparison.OrdinalIgnoreCase));
    }
}
