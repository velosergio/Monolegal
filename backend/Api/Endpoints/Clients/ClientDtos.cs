using System;
using Monolegal.Domain.Entities;

namespace Monolegal.Api.Endpoints.Clients;

/// <summary>DTO de transporte de un cliente (spec 018).</summary>
public sealed record ClientDto(
    string Id,
    string Name,
    string Email,
    string? Phone,
    string? Address,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ClientDto FromEntity(Client client) => new(
        client.Id,
        client.Name,
        client.Email,
        client.Phone,
        client.Address,
        client.CreatedAt,
        client.UpdatedAt);
}

/// <summary>Cuerpo de POST /api/clients (spec 018, RF-014).</summary>
public sealed record CreateClientRequest(string? Name, string? Email, string? Phone, string? Address);

/// <summary>Cuerpo de PUT /api/clients/{id} (spec 018, RF-016).</summary>
public sealed record UpdateClientRequest(string? Name, string? Email, string? Phone, string? Address);
