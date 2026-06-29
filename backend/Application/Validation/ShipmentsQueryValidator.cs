using FluentValidation;

namespace Backend.Application.Validation;

/// <summary>
/// Parámetros normalizados del listado de envíos GET /api/invoices/shipments (spec 019).
/// Los nulos representan parámetros ausentes (defaults aplicados antes de validar). <c>Search</c>
/// ya viene normalizada (trim; vacío ⇒ null). <c>SendStatus</c>, si está presente, debe ser uno de
/// pending/sent/failed/skipped.
/// </summary>
public sealed record ShipmentsQuery(string? SendStatus, int Page, int PageSize, string? Search = null);

/// <summary>
/// Validador de los parámetros del listado de envíos: page ≥ 1; 1 ≤ pageSize ≤ 50; sendStatus, si
/// está presente, debe ser válido; search ≤ 100. La validación de sendStatus se inyecta como
/// predicado para no acoplar Application a la capa Api.
/// </summary>
public sealed class ShipmentsQueryValidator : AbstractValidator<ShipmentsQuery>
{
    public const int MaxPageSize = 50;
    public const int MaxSearchLength = 100;

    public ShipmentsQueryValidator(System.Func<string?, bool> isValidSendStatus)
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El parámetro 'page' debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"El parámetro 'pageSize' debe estar entre 1 y {MaxPageSize}.");

        RuleFor(x => x.SendStatus)
            .Must(isValidSendStatus)
            .When(x => x.SendStatus is not null)
            .WithMessage("El parámetro 'sendStatus' no corresponde a un estado de envío válido.");

        RuleFor(x => x.Search!)
            .MaximumLength(MaxSearchLength)
            .When(x => x.Search is not null)
            .WithMessage($"El parámetro 'search' no puede exceder {MaxSearchLength} caracteres.");
    }
}
