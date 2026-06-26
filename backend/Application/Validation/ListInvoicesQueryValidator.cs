using FluentValidation;

namespace Backend.Application.Validation;

/// <summary>
/// Parámetros de consulta normalizados para GET /api/invoices.
/// Los valores nulos representan parámetros ausentes (se aplican defaults antes de validar).
/// <c>Search</c> es la búsqueda por cliente ya normalizada (trim; vacío ⇒ null) — spec 014, FR-012.
/// </summary>
public sealed record ListInvoicesQuery(string? Status, int Page, int PageSize, string? Search = null);

/// <summary>
/// Validador de los parámetros de listado (spec 009, FR-003/FR-003a/FR-006/FR-017).
/// Reglas: page ≥ 1; 1 ≤ pageSize ≤ 50; status, si está presente, debe ser un estado válido.
/// La validación del valor de <c>Status</c> contra el conjunto de estados de dominio se inyecta
/// como predicado para no acoplar Application a la capa Api.
/// </summary>
public sealed class ListInvoicesQueryValidator : AbstractValidator<ListInvoicesQuery>
{
    public const int MaxPageSize = 50;
    public const int MaxSearchLength = 100;

    public ListInvoicesQueryValidator(System.Func<string, bool> isValidStatus)
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El parámetro 'page' debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"El parámetro 'pageSize' debe estar entre 1 y {MaxPageSize}.");

        RuleFor(x => x.Status!)
            .Must(isValidStatus)
            .When(x => x.Status is not null)
            .WithMessage("El parámetro 'status' no corresponde a un estado válido.");

        RuleFor(x => x.Search!)
            .MaximumLength(MaxSearchLength)
            .When(x => x.Search is not null)
            .WithMessage($"El parámetro 'search' no puede exceder {MaxSearchLength} caracteres.");
    }
}
