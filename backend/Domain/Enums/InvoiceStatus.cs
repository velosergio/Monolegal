namespace Monolegal.Domain.Enums;

public enum InvoiceStatus
{
    // Legacy / general statuses
    Draft = 0,
    Overdue = 3,
    Cancelled = 4,

    // Active workflow statuses (US1 / US2)
    Pending = 1,
    PrimerRecordatorio = 10,
    SegundoRecordatorio = 11,
    Desactivado = 12,
    Pagado = 2
}
