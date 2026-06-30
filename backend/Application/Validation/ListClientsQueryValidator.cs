using FluentValidation;

namespace Backend.Application.Validation;

/// <summary>
/// Parámetros de consulta normalizados para GET /api/clients (spec 018, RF-012/RF-013).
/// <c>Search</c> ya viene normalizada (trim; vacío ⇒ null).
/// </summary>
public sealed record ListClientsQuery(int Page, int PageSize, string? Search = null);

/// <summary>
/// Validador del listado de clientes. Reglas: page ≥ 1; 1 ≤ pageSize ≤ 50; search ≤ 100 caracteres.
/// Replica <see cref="ListInvoicesQueryValidator"/> para consistencia (spec 018, research D9).
/// </summary>
/// <remarks>
/// SOLID: SRP — única razón de cambio: las reglas de validación del listado de clientes.
/// LSP — sustituye a <c>AbstractValidator&lt;ListClientsQuery&gt;</c> sin romper a sus consumidores.
/// </remarks>
public sealed class ListClientsQueryValidator : AbstractValidator<ListClientsQuery>
{
    public const int MaxPageSize = 50;
    public const int MaxSearchLength = 100;

    public ListClientsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El parámetro 'page' debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"El parámetro 'pageSize' debe estar entre 1 y {MaxPageSize}.");

        RuleFor(x => x.Search!)
            .MaximumLength(MaxSearchLength)
            .When(x => x.Search is not null)
            .WithMessage($"El parámetro 'search' no puede exceder {MaxSearchLength} caracteres.");
    }
}
