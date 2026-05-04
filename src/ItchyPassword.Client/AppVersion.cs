namespace ItchyPassword.Client;

/// <summary>
/// Application version constants.
/// Major and Minor are updated manually. GitCommitHash is injected at build time via MSBuild.
/// </summary>
public static partial class AppVersion
{
    public const int Major = 2;
    public const int Minor = 1;

    public static string FullVersion
    {
        get
        {
            string hash = GitCommitHash.Length > 7 ? GitCommitHash[..7] : GitCommitHash;
            return $"{Major}.{Minor}-{hash}";
        }
    }
}
