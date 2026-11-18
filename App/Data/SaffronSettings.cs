namespace Saffron.Data;

public sealed record SaffronSettings(string CatalogConnectionString, string RedisConnectionString, int SessionIdleSeconds);
