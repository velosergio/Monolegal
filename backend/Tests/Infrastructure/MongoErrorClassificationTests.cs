using System.Net;
using Backend.Infrastructure.Configuration;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Unit tests for failure classification: availability vs authentication (FR-007, T010).
/// Reference: specs/004-mongodb-connection/research.md (D5).
/// </summary>
public class MongoErrorClassificationTests
{
    private static ConnectionId BuildConnectionId()
    {
        var serverId = new ServerId(new ClusterId(), new DnsEndPoint("mongo", 27017));
        return new ConnectionId(serverId);
    }

    [Fact]
    public void Classify_TimeoutException_AsUnavailable()
    {
        var failure = MongoErrorClassifier.Classify(new TimeoutException("server selection timed out"));

        failure.Kind.ShouldBe(MongoConnectionFailureKind.Unavailable);
        failure.Message.ShouldContain("no disponible");
    }

    [Fact]
    public void Classify_MongoConnectionException_AsUnavailable()
    {
        var ex = new MongoConnectionException(BuildConnectionId(), "connection refused");

        var failure = MongoErrorClassifier.Classify(ex);

        failure.Kind.ShouldBe(MongoConnectionFailureKind.Unavailable);
    }

    [Fact]
    public void Classify_MongoAuthenticationException_AsAuthentication()
    {
        var ex = new MongoAuthenticationException(BuildConnectionId(), "auth failed");

        var failure = MongoErrorClassifier.Classify(ex);

        failure.Kind.ShouldBe(MongoConnectionFailureKind.Authentication);
        failure.Message.ShouldContain("autenticación");
    }

    [Fact]
    public void Classify_UnwrapsInnerException()
    {
        var inner = new TimeoutException("timed out");
        var wrapper = new InvalidOperationException("wrapper", inner);

        var failure = MongoErrorClassifier.Classify(wrapper);

        failure.Kind.ShouldBe(MongoConnectionFailureKind.Unavailable);
    }

    [Fact]
    public void Classify_UnknownException_AsUnknown()
    {
        var failure = MongoErrorClassifier.Classify(new InvalidOperationException("something else"));

        failure.Kind.ShouldBe(MongoConnectionFailureKind.Unknown);
    }
}
