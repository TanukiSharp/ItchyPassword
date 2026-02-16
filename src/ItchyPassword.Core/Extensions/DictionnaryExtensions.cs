namespace ItchyPassword.Core.Extensions;

public static class DictionnaryExtensions
{
    extension(Dictionary<string, object> source)
    {
        public bool TryGetStringValue(string key, out string? value)
        {
            if (source.TryGetValue(key, out object? objValue) && objValue is string strValue)
            {
                value = strValue;
                return true;
            }

            value = null;
            return false;
        }

        public string? GetStringValueOrDefault(string key, string? defaultValue = null)
        {
            if (source.TryGetStringValue(key, out string? value))
            {
                return value;
            }

            return defaultValue;
        }

        public bool TryGetIntegerValue(string key, out int? value)
        {
            if (source.TryGetValue(key, out object? objValue) && objValue is int intValue)
            {
                value = intValue;
                return true;
            }

            value = null;
            return false;
        }

        public int? GetIntegerValueOrDefault(string key, int? defaultValue = null)
        {
            if (source.TryGetIntegerValue(key, out int? value))
            {
                return value;
            }

            return defaultValue;
        }

    }
}
