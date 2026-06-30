using Microsoft.Extensions.Logging;

namespace Backend.Application.Services;

/// <summary>
/// Abstract base class for application services providing common infrastructure.
/// </summary>
/// <typeparam name="T">The derived service type for logger categorization.</typeparam>
/// <remarks>
/// SOLID: SRP — centraliza la infraestructura común (logging) de los servicios de aplicación.
/// OCP — las clases derivadas extienden el comportamiento sin modificar la base.
/// </remarks>
public abstract class AppService<T> where T : AppService<T>
{
    protected readonly ILogger<T> Logger;

    protected AppService(ILogger<T> logger)
    {
        Logger = logger;
    }
}
