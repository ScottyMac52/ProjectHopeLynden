namespace ProjectHopeLynden.Web.Information;

public static class ApplicationVersionFormatter
{
    public static string Format(Version? version)
    {
        if (version is null)
        {
            return "0.0.0.0";
        }

        return string.Join(
            '.',
            Math.Max(version.Major, 0),
            Math.Max(version.Minor, 0),
            Math.Max(version.Build, 0),
            Math.Max(version.Revision, 0));
    }
}
