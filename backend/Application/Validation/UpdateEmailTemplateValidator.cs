using System.Linq;
using FluentValidation;
using Monolegal.Domain.Email;

namespace Backend.Application.Validation;

/// <summary>Cuerpo de PUT /api/settings/email/templates/{type} (spec 017, US2).</summary>
public sealed record EmailTemplateInput(string? Subject, string? Body);

/// <summary>
/// Validador de plantillas de email (spec 017, FR-011/FR-015): asunto y cuerpo no vacíos y que
/// solo referencien variables del catálogo cerrado (<see cref="EmailTemplateVariables"/>).
/// </summary>
/// <remarks>
/// SOLID: SRP — única razón de cambio: las reglas de validación de plantillas de email.
/// LSP — sustituye a <c>AbstractValidator&lt;EmailTemplateInput&gt;</c> sin romper a sus consumidores.
/// </remarks>
public sealed class UpdateEmailTemplateValidator : AbstractValidator<EmailTemplateInput>
{
    public UpdateEmailTemplateValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("El asunto es obligatorio.")
            .Must(HasOnlyAllowedVariables).WithMessage(InvalidVariableMessage);

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("El cuerpo es obligatorio.")
            .Must(HasOnlyAllowedVariables).WithMessage(InvalidVariableMessage);
    }

    private static bool HasOnlyAllowedVariables(string? text)
        => string.IsNullOrEmpty(text) || EmailTemplateRenderer.FindInvalidVariables(text).Count == 0;

    private static string InvalidVariableMessage(EmailTemplateInput input, string? text)
    {
        var invalid = string.IsNullOrEmpty(text)
            ? Enumerable.Empty<string>()
            : EmailTemplateRenderer.FindInvalidVariables(text);
        return $"Variable no admitida: {string.Join(", ", invalid.Select(v => $"{{{{{v}}}}}"))}.";
    }
}
