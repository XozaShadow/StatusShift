namespace StatusShift;

internal static class AppInfo
{
    public static string Version
    {
        get
        {
            var v = typeof(Plugin).Assembly.GetName().Version;
            return v is null ? "0.1.4.1" : $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
        }
    }
}
