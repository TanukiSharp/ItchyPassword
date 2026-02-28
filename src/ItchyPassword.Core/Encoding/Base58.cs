namespace ItchyPassword.Core.Encoding;

public static class Base58
{
    private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    public static string Encode(byte[] input)
    {
        return BaseN.Encode(input, Alphabet);
    }

    public static byte[] Decode(string input)
    {
        return BaseN.Decode(input, Alphabet);
    }
}
