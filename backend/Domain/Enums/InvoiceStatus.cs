namespace Monolegal.Domain.Enums;

public enum InvoiceStatus
{
    // Conjunto de estados activos. Los valores legacy (Draft=0/Overdue=3/Cancelled=4) se
    // retiraron en la spec 015 (FR-031); la migración remapea cualquier documento que los tuviera
    // a un estado activo válido antes de retirarlos. Los valores numéricos restantes se conservan
    // por compatibilidad con los documentos existentes.
    Pending = 1,
    PrimerRecordatorio = 10,
    SegundoRecordatorio = 11,
    Desactivado = 12,
    Pagado = 2
}
