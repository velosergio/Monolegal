using Backend.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Unit tests for MongoDbOptions binding from configuration (FR-004, T009).
/// Contract: specs/004-mongodb-connection/contracts/connection-config.md.
/// </summary>
public class MongoDbOptionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void BuildMongoOptions_MapsConnectionStringFromMongoDbUri()
    {
        var config = BuildConfig(new()
        {
            ["MONGODB_URI"] = "mongodb://root:pwd@mongo:27017/monolegal_dev",
        });

        var options = DependencyInjection.BuildMongoOptions(config);

        options.ConnectionString.ShouldBe("mongodb://root:pwd@mongo:27017/monolegal_dev");
    }

    [Fact]
    public void BuildMongoOptions_DerivesDatabaseNameFromUri()
    {
        var config = BuildConfig(new()
        {
            ["MONGODB_URI"] = "mongodb://root:pwd@mongo:27017/monolegal_dev",
        });

        var options = DependencyInjection.BuildMongoOptions(config);

        options.DatabaseName.ShouldBe("monolegal_dev");
    }

    [Fact]
    public void BuildMongoOptions_DefaultsDatabaseName_WhenUriHasNoDatabase()
    {
        var config = BuildConfig(new()
        {
            ["MONGODB_URI"] = "mongodb://root:pwd@mongo:27017",
        });

        var options = DependencyInjection.BuildMongoOptions(config);

        options.DatabaseName.ShouldBe(MongoDbOptions.DefaultDatabaseName);
    }

    [Fact]
    public void BuildMongoOptions_AppliesDefaultPoolAndTimeout()
    {
        var config = BuildConfig(new()
        {
            ["MONGODB_URI"] = "mongodb://mongo:27017/monolegal_dev",
        });

        var options = DependencyInjection.BuildMongoOptions(config);

        options.MaxConnectionPoolSize.ShouldBe(100);
        options.ServerSelectionTimeout.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void BuildMongoOptions_BindsNonSensitiveOverridesFromMongoDbSection()
    {
        var config = BuildConfig(new()
        {
            ["MONGODB_URI"] = "mongodb://mongo:27017/monolegal_dev",
            ["MongoDb:MaxConnectionPoolSize"] = "50",
            ["MongoDb:ServerSelectionTimeout"] = "00:00:03",
        });

        var options = DependencyInjection.BuildMongoOptions(config);

        options.MaxConnectionPoolSize.ShouldBe(50);
        options.ServerSelectionTimeout.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void BuildMongoOptions_FallsBackToConnectionStringsMongoDb()
    {
        var config = BuildConfig(new()
        {
            ["ConnectionStrings:MongoDB"] = "mongodb://mongo:27017/monolegal_dev",
        });

        var options = DependencyInjection.BuildMongoOptions(config);

        options.ConnectionString.ShouldBe("mongodb://mongo:27017/monolegal_dev");
    }

    [Fact]
    public void BuildMongoOptions_Throws_WhenNoConnectionStringProvided()
    {
        var config = BuildConfig(new());

        Should.Throw<InvalidOperationException>(() => DependencyInjection.BuildMongoOptions(config));
    }
}
