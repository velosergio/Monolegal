namespace Backend.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed MongoDB connection options.
/// The connection string is sourced from the MONGODB_URI environment variable
/// (injected by docker-compose) and is never hardcoded. See
/// specs/004-mongodb-connection/contracts/connection-config.md.
/// </summary>
public sealed class MongoDbOptions
{
    /// <summary>Default development database name (FR-003).</summary>
    public const string DefaultDatabaseName = "monolegal_dev";

    /// <summary>
    /// MongoDB connection URI. Required; sourced from MONGODB_URI. Not logged.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Target database name. Defaults to <see cref="DefaultDatabaseName"/> and can be
    /// derived from the connection string when present.
    /// </summary>
    public string DatabaseName { get; set; } = DefaultDatabaseName;

    /// <summary>Maximum connection pool size (FR-010). Constitution: pooling configured.</summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Server selection timeout. Kept short so unavailability is reported quickly and
    /// can be distinguished from authentication failures (FR-007, research D4).
    /// </summary>
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
