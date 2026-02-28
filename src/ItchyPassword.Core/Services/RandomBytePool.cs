namespace ItchyPassword.Core.Services;

/// <summary>
/// Manages a reusable buffer of cryptographically secure random bytes.
/// Automatically fetches fresh batches from <see cref="ICryptoService"/> when exhausted.
/// Register as a scoped service so leftover bytes carry over between generation calls.
/// </summary>
public sealed class RandomBytePool(ICryptoService cryptoService)
{
    /// <summary>
    /// Number of bytes fetched per batch. 256 bytes covers the common case of a
    /// 64-character password (127 two-byte reads) with room for rejection sampling.
    /// </summary>
    private const int BatchSize = 256;

    private byte[] _bytes = [];
    private int _index;

    /// <summary>
    /// Reads two bytes and returns a 16-bit unsigned value (0–65535).
    /// Fetches a new batch when fewer than 2 bytes remain.
    /// </summary>
    public async Task<int> ReadTwoBytesAsync()
    {
        if (_index + 2 > _bytes.Length)
        {
            _bytes = await cryptoService.GenerateRandomBytesAsync(BatchSize);
            _index = 0;
        }

        int value = (_bytes[_index] << 8) | _bytes[_index + 1];
        _index += 2;
        return value;
    }
}
