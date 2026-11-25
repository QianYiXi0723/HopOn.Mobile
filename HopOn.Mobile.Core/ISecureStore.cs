namespace HopOn.Mobile.Core;

public interface ISecureStore
{
    Task SetAsync(string key, string value);
    Task<string?> GetAsync(string key);
    Task RemoveAsync(string key);
}
