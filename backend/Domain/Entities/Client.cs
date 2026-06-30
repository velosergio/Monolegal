using System;
using System.Diagnostics.CodeAnalysis;

namespace Monolegal.Domain.Entities;

/// <summary>
/// Cliente al que se le emiten facturas (spec 018). Hasta esta spec el cliente sólo existía como
/// un identificador (<see cref="Invoice.ClientId"/>); aquí se modela como entidad de primera clase
/// en la colección <c>Clients</c>. El <see cref="Email"/> es obligatorio y único entre clientes
/// (RF-015a, research D5) y se normaliza a minúsculas antes de persistir. Teléfono y dirección son
/// opcionales (clarificación Q3).
/// </summary>
public sealed class Client
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Client(string name, string email, string? phone = null, string? address = null)
    {
        Id = Guid.NewGuid().ToString("N");
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        SetDetails(name, email, phone, address);
    }

    /// <summary>
    /// Actualiza los datos editables del cliente, normalizando email (minúsculas) y recortando
    /// los campos de texto. Refresca <see cref="UpdatedAt"/>.
    /// </summary>
    public void Update(string name, string email, string? phone, string? address)
    {
        SetDetails(name, email, phone, address);
        UpdatedAt = DateTime.UtcNow;
    }

    [MemberNotNull(nameof(Name), nameof(Email))]
    private void SetDetails(string name, string email, string? phone, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del cliente es obligatorio.", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email del cliente es obligatorio.", nameof(email));

        Name = name.Trim();
        Email = NormalizeEmail(email);
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
    }

    /// <summary>
    /// Crea un cliente con un identificador explícito. Reservado para el sembrador de datos de
    /// desarrollo (spec 008/018, research D11), que necesita IDs estables y conocidos para mantener
    /// la integridad referencial con las facturas sembradas. No usar en flujos de alta normales.
    /// </summary>
    public static Client CreateForSeed(string id, string name, string email, string? phone = null, string? address = null)
    {
        var client = new Client(name, email, phone, address);
        client.Id = id;
        return client;
    }

    /// <summary>Normaliza un email a minúsculas y sin espacios, para comparación/unicidad.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
