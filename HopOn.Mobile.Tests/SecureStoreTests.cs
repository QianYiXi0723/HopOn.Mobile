using HopOn.Mobile.Core;
using Xunit;

namespace HopOn.Mobile.Tests;

public class SecureStoreTests
{
    [Fact]
    public async Task Store_RoundTrips_And_Removes()
    {
        ISecureStore store = new InMemorySecureStore();
        await store.SetAsync("token", "secret-value");
        Assert.Equal("secret-value", await store.GetAsync("token"));
        await store.RemoveAsync("token");
        Assert.Null(await store.GetAsync("token"));
    }

    [Fact]
    public async Task Missing_Key_Returns_Null()
    {
        ISecureStore store = new InMemorySecureStore();
        Assert.Null(await store.GetAsync("never-set"));
    }
}
