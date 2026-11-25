using HopOn.Mobile.Core;
using Xunit;

namespace HopOn.Mobile.Tests;

public class PairAddressTests
{
    [Fact]
    public void SelectedDevice_Wins()
    {
        var addr = PairAddress.Resolve(new DiscoveredPc("192.168.1.50", 8766), "10.0.0.9");
        Assert.Equal("http://192.168.1.50:8766", addr);
    }

    [Fact]
    public void ManualIp_WhenNoSelection()
    {
        var addr = PairAddress.Resolve(null, "10.253.244.245");
        Assert.Equal("http://10.253.244.245:8766", addr);
    }

    [Fact]
    public void ManualIp_TrimsSpaces()
    {
        var addr = PairAddress.Resolve(null, "  10.253.244.245  ");
        Assert.Equal("http://10.253.244.245:8766", addr);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Nothing_ReturnsNull(string? typed)
    {
        Assert.Null(PairAddress.Resolve(null, typed));
    }
}
