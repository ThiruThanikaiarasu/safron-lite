namespace Saffron.Rules.Tests;

using Xunit;

public class WiringTests
{
    [Fact]
    public void AssemblyMarkerBelongsToSaffronRulesAssembly()
    {
        Assert.Equal("Saffron.Rules", typeof(Saffron.Rules.AssemblyMarker).Assembly.GetName().Name);
    }
}
