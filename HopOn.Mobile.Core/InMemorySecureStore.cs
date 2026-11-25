namespace HopOn.Mobile.Core;

public sealed class InMemorySecureStore : ISecureStore
{
    private readonly Dictionary<string, string> _data = new();

    public Task SetAsync(string key, string value)
    {
        _data[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key) =>
        Task.FromResult(_data.TryGetValue(key, out var v) ? v : null);

    public Task RemoveAsync(string key)
    {
        _data.Remove(key);
        return Task.CompletedTask;
    }
}
