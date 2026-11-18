namespace Saffron.Api.Tests;

using Xunit;

public class WiringTests
{
    [Fact]
    public void AssemblyMarkerBelongsToSaffronApiAssembly()
    {
        Assert.Equal("Saffron.Api", typeof(Saffron.Api.AssemblyMarker).Assembly.GetName().Name);
    }
}
