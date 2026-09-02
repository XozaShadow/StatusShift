using System;
using System.Collections.Generic;

namespace StatusShift;

internal static class ListText
{
    public static bool Contains(this List<string> list, string value, StringComparer comparer)
        => list.Exists(x => comparer.Equals(x, value));
}
