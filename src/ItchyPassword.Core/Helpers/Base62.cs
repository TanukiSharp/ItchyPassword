using System.Text;

namespace ItchyPassword.Core.Helpers;

public static class Base62
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string Encode(byte[] input)
    {
        if (input == null || input.Length == 0) return "";
        if (input.Length > 0xFFFF) throw new ArgumentException("Buffer too large");

        // 1. Create Headered Buffer (2 bytes LE length + data)
        var headeredBuffer = new byte[2 + input.Length];
        headeredBuffer[0] = (byte)(input.Length % 256);
        headeredBuffer[1] = (byte)(input.Length / 256);
        Array.Copy(input, 0, headeredBuffer, 2, input.Length);

        // 2. Encode using BaseN
        return BaseN.Encode(headeredBuffer, Alphabet);
    }
}
