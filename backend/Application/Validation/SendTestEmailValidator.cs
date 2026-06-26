using System;
using FluentValidation;
using Monolegal.Domain.Enums;

namespace Backend.Application.Validation;

/// <summary>Cuerpo de POST /api/settings/email/test (spec 017, US3).</summary>
public sealed record SendTestEmailInput(string? To, string? TemplateType);

/// <summary>
/// Validador del envío de prueba (spec 017, FR-017): destino con formato email y tipo de plantilla
/// dentro del conjunto admitido.
/// </summary>
public sealed class SendTestEmailValidator : AbstractValidator<SendTestEmailInput>
{
    public SendTestEmailValidator()
    {
        RuleFor(x => x.To)
            .NotEmpty().WithMessage("El campo 'to' es obligatorio.")
            .EmailAddress().WithMessage("El campo 'to' debe ser un correo válido.");

        RuleFor(x => x.TemplateType)
            .NotEmpty().WithMessage("El campo 'templateType' es obligatorio.")
            .Must(IsValidTemplateType)
            .WithMessage("El campo 'templateType' no corresponde a un tipo de plantilla válido.");
    }

    private static bool IsValidTemplateType(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && Enum.TryParse<NotificationType>(value, ignoreCase: true, out var type)
           && Enum.IsDefined(type);
}
