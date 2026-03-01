namespace ItchyPassword.Core.Exceptions;

public class VaultConnectorNotConfiguredException : Exception
{
    public VaultConnectorNotConfiguredException() : base("The selected vault connector is not configured.")
    {
    }

    public VaultConnectorNotConfiguredException(string message) : base(message)
    {
    }

    public VaultConnectorNotConfiguredException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class VaultAccessDeniedException : Exception
{
    public VaultAccessDeniedException() : base("Access to the vault connector was denied.")
    {
    }

    public VaultAccessDeniedException(string message) : base(message)
    {
    }

    public VaultAccessDeniedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class VaultFormatException : Exception
{
    public VaultFormatException() : base("The vault format is invalid.")
    {
    }

    public VaultFormatException(string message) : base(message)
    {
    }

    public VaultFormatException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class VaultDecryptionException : Exception
{
    public VaultDecryptionException() : base("Failed to decrypt the vault. The Master Key may be incorrect.")
    {
    }

    public VaultDecryptionException(string message) : base(message)
    {
    }

    public VaultDecryptionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
