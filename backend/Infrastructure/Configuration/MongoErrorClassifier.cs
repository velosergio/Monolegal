using MongoDB.Driver;

namespace Backend.Infrastructure.Configuration;

/// <summary>
/// Categories of MongoDB connectivity failures (FR-007, research D5).
/// </summary>
public enum MongoConnectionFailureKind
{
    /// <summary>The server could not be reached (down, wrong host/port, network).</summary>
    Unavailable,

    /// <summary>The server was reached but credentials were rejected.</summary>
    Authentication,

    /// <summary>An unexpected failure that does not fit the categories above.</summary>
    Unknown,
}

/// <summary>
/// Result of classifying a MongoDB connectivity exception: a stable kind plus a
/// human-readable, credential-free message suitable for structured logs and health output.
/// </summary>
/// <param name="Kind">The classified failure kind.</param>
/// <param name="Message">A clear, differentiated message (no secrets).</param>
public readonly record struct MongoConnectionFailure(MongoConnectionFailureKind Kind, string Message);

/// <summary>
/// Classifies MongoDB driver exceptions into availability vs. authentication failures
/// so callers can report clear, differentiated messages (FR-007, SC-004).
/// </summary>
public static class MongoErrorClassifier
{
    public static MongoConnectionFailure Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // MongoAuthenticationException derives from MongoConnectionException, so it must
        // be checked first to avoid being swallowed by the broader connection branch.
        if (ContainsException<MongoAuthenticationException>(exception))
        {
            return new MongoConnectionFailure(
                MongoConnectionFailureKind.Authentication,
                "Fallo de autenticación con MongoDB: credenciales inválidas.");
        }

        // Server selection timeout surfaces as TimeoutException; connection issues as
        // MongoConnectionException. Both mean the server is effectively unreachable.
        if (ContainsException<TimeoutException>(exception)
            || ContainsException<MongoConnectionException>(exception))
        {
            return new MongoConnectionFailure(
                MongoConnectionFailureKind.Unavailable,
                "Servicio MongoDB no disponible: no se pudo establecer conexión con el servidor.");
        }

        return new MongoConnectionFailure(
            MongoConnectionFailureKind.Unknown,
            "Fallo de conexión con MongoDB no clasificado.");
    }

    private static bool ContainsException<T>(Exception exception) where T : Exception
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is T)
            {
                return true;
            }

            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.Flatten().InnerExceptions)
                {
                    if (ContainsException<T>(inner))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
