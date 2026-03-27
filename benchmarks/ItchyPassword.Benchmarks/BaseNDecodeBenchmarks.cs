using BenchmarkDotNet.Attributes;
using ItchyPassword.Core.Encoding;

namespace ItchyPassword.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="BaseN.Decode"/> with various input sizes
/// using the Base58 alphabet (the most common alphabet in the codebase).
/// </summary>
[MemoryDiagnoser]
public class BaseNDecodeBenchmarks
{
    private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    /// <summary>
    /// Short input simulating a small encoded value (e.g., an HMAC signature).
    /// </summary>
    private string _shortInput = string.Empty;

    /// <summary>
    /// Medium input simulating a typical encrypted secret cipher.
    /// </summary>
    private string _mediumInput = string.Empty;

    /// <summary>
    /// Long input simulating a large encrypted payload.
    /// </summary>
    private string _longInput = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        // Generate realistic Base58-encoded strings by encoding random byte arrays.
        _shortInput = BaseN.Encode(GenerateBytes(32), Base58Alphabet);    // ~44 chars
        _mediumInput = BaseN.Encode(GenerateBytes(128), Base58Alphabet);  // ~175 chars
        _longInput = BaseN.Encode(GenerateBytes(512), Base58Alphabet);    // ~700 chars
    }

    [Benchmark(Description = "Decode 32 bytes (~44 chars)")]
    public byte[] DecodeShort()
    {
        return BaseN.Decode(_shortInput, Base58Alphabet);
    }

    [Benchmark(Description = "Decode 128 bytes (~175 chars)")]
    public byte[] DecodeMedium()
    {
        return BaseN.Decode(_mediumInput, Base58Alphabet);
    }

    [Benchmark(Description = "Decode 512 bytes (~700 chars)")]
    public byte[] DecodeLong()
    {
        return BaseN.Decode(_longInput, Base58Alphabet);
    }

    private static byte[] GenerateBytes(int count)
    {
        byte[] data = new byte[count];
        Random.Shared.NextBytes(data);
        return data;
    }
}
