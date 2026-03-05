namespace ItchyPassword.Client.Services;

public sealed class ToastService
{
    public event Action<string, TimeSpan>? OnShow;

    public void Show(string message, TimeSpan duration)
    {
        OnShow?.Invoke(message, duration);
    }
}
