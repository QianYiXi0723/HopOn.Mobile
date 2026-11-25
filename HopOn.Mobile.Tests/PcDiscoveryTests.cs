using System;
using HopOn.Mobile.Core;
using Xunit;

namespace HopOn.Mobile.Tests;

public class PcDiscoveryTests
{
    [Fact]
    public void ParseResponse_ExtractsIpAndPort()
    {
        var pc = PcDiscovery.ParseResponse("{\"ip\":\"192.168.1.50\",\"port\":8766}");
        Assert.Equal("192.168.1.50", pc.Ip);
        Assert.Equal(8766, pc.Port);
    }

    [Fact]
    public void ParseResponse_RejectsMalformed()
    {
        Assert.Throws<FormatException>(() => PcDiscovery.ParseResponse("not-json"));
    }

    [Fact]
    public void ParseResponse_RejectsMissingPort()
    {
        Assert.Throws<FormatException>(() => PcDiscovery.ParseResponse("{\"ip\":\"1.2.3.4\"}"));
    }
}
