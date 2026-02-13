using System.Numerics;
using System.Text;

namespace ItchyPassword.App.Services;

public interface IPasswordGeneratorService
{
    string Generate(string masterKey, string publicPart, string alphabet, int length, int version);
}

public class PasswordGeneratorService : IPasswordGeneratorService
{
    private readonly ICryptoService _cryptoService;

    public PasswordGeneratorService(ICryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    public string Generate(string masterKey, string publicPart, string alphabet, int length, int version)
    {
        if (string.IsNullOrEmpty(masterKey))
        {
            return "";
        }

        if (string.IsNullOrEmpty(publicPart))
        {
            return "";
        }

        if (publicPart.Length < 8)
        {
            return ""; // Minimum public part length.
        }

        int iterations = version switch
        {
            1 => 100_000,
            2 => 400_000,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unsupported version")
        };
        string purpose = "Password";

        byte[] privateBytes = Encoding.UTF8.GetBytes(masterKey);
        byte[] publicBytes = Encoding.UTF8.GetBytes(publicPart);

        byte[] hash = _cryptoService.GeneratePassword(privateBytes, publicBytes, purpose, iterations);

        string rawPassword = ToCustomBaseOneWay(hash, alphabet);

        if (rawPassword.Length > length)
        {
            return rawPassword.Substring(0, length);
        }

        return rawPassword;
    }

    private string ToCustomBaseOneWay(byte[] bytes, string alphabet)
    {
        // Treat bytes as unsigned little-endian number
        BigInteger number = new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
        BigInteger alphabetLength = alphabet.Length;
        StringBuilder result = new StringBuilder();

        while (number > 0)
        {
            BigInteger remainder = number % alphabetLength;
            number /= alphabetLength;
            result.Append(alphabet[(int)remainder]);
        }

        return result.ToString();
    }
}
