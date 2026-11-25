using HopOn.Mobile.Core;
using Microsoft.Maui.Storage;

namespace HopOn.Mobile;

/// <summary>
/// 用 MAUI 系统安全存储实现 Core 的 ISecureStore：
/// Android = EncryptedSharedPreferences，iOS = Keychain。放 App 工程以保持 Core 零 MAUI 依赖。
/// </summary>
public sealed class MauiSecureStore : ISecureStore
{
    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);

    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);

    public Task RemoveAsync(string key)
    {
        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }
}
