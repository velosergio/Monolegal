using Microsoft.Extensions.Logging;

namespace Backend.Application.Services;

/// <summary>
/// Abstract base class for application services providing common infrastructure.
/// </summary>
/// <typeparam name="T">The derived service type for logger categorization.</typeparam>
public abstract class AppService<T> where T : AppService<T>
{
    protected readonly ILogger<T> Logger;

    protected AppService(ILogger<T> logger)
    {
        Logger = logger;
    }
}
