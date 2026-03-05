namespace ItchyPassword.Core.Constants;

public static class SecretDataConstants
{
    public const int LatestCryptoVersion = 3;
    public const string LatestEncoding = "base58";
    public const int DefaultLength = 64;
    public const string DefaultSymbolAlphabet = "!@#$%^&*()-_=+[]{}|;:',.<>?/`~";
}

public static class MasterKeyConstants
{
    public const int MinimumLength = 16;
}
