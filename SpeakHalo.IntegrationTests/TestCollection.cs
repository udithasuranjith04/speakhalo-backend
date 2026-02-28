namespace SpeakHalo.IntegrationTests;

/// <summary>
/// Collection definition that ensures a single SpeakHaloWebApplicationFactory instance
/// is shared across all test classes. This prevents Serilog "frozen logger" issues
/// that occur when multiple WebApplicationFactory instances are created.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<SpeakHaloWebApplicationFactory>
{
}
