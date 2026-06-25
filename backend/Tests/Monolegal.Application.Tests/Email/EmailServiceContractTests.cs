using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Monolegal.Domain.Entities;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Email;

/// <summary>
/// Pruebas de contrato para <see cref="IEmailService"/> (spec 011). Verifican que el
/// contrato es sustituible por un doble de prueba (CE-002) y que sus operaciones son
/// asíncronas y reciben el correo del cliente y la factura asociada (RF-002, RF-003, RF-004).
/// </summary>
public class EmailServiceContractTests
{
    private const string ClientEmail = "cliente@correo.com";

    private static Invoice CreateInvoice() => new("cliente-001", 150_000m);

    [Fact]
    public async Task SendReminderAsync_EsSustituible_YRecibeCorreoYFactura()
    {
        // El fake se usa a través de la abstracción: demuestra sustituibilidad (Liskov/DIP).
        IEmailService emailService = new FakeEmailService();
        var invoice = CreateInvoice();

        await emailService.SendReminderAsync(ClientEmail, invoice);

        var fake = (FakeEmailService)emailService;
        fake.ReminderCalls.ShouldHaveSingleItem();
        fake.ReminderCalls[0].ClientEmail.ShouldBe(ClientEmail);
        fake.ReminderCalls[0].Invoice.ShouldBeSameAs(invoice);
    }

    [Fact]
    public async Task SendPaymentConfirmationAsync_EsSustituible_YRecibeCorreoYFactura()
    {
        IEmailService emailService = new FakeEmailService();
        var invoice = CreateInvoice();

        await emailService.SendPaymentConfirmationAsync(ClientEmail, invoice);

        var fake = (FakeEmailService)emailService;
        fake.PaymentConfirmationCalls.ShouldHaveSingleItem();
        fake.PaymentConfirmationCalls[0].ClientEmail.ShouldBe(ClientEmail);
        fake.PaymentConfirmationCalls[0].Invoice.ShouldBeSameAs(invoice);
    }
}
