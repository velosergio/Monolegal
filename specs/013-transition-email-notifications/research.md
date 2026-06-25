# Research — Envío de Correos y Registro en Transiciones (013)

Decisiones técnicas para resolver los "unknowns" del plan. Cada entrada sigue el formato Decisión / Justificación / Alternativas.

## D1 — Proveedor de correo concreto

**Decisión**: Implementar `IEmailService` en Infrastructure con **MailKit/MimeKit** sobre SMTP, configurable por variables de entorno (`Email__Host`, `Email__Port`, `Email__Username`, `Email__Password`, `Email__From`, `Email__UseStartTls`). En entorno **Development** se registra un emisor **`NoOpEmailService`** que sólo escribe un log estructurado (no requiere servidor SMTP real). La selección entre uno u otro se hace en DI según `IHostEnvironment`/configuración.

**Justificación**: `System.Net.Mail.SmtpClient` está marcado como obsoleto/no recomendado por Microsoft para clientes nuevos; MailKit es la librería SMTP de facto en .NET, mantenida y compatible con STARTTLS/SSL. El emisor no-op permite ejecutar el sistema y los flujos de transición en local y CI sin infraestructura de correo, cumpliendo Test-First sin sockets. Las credenciales por entorno respetan la regla de "sin secretos hardcodeados".

**Alternativas consideradas**:
- `System.Net.Mail.SmtpClient`: descartado (obsoleto, sin STARTTLS moderno).
- SDK de proveedor (SendGrid/Mailgun): añade acoplamiento a un vendor y una dependencia HTTP; se puede introducir luego detrás del mismo `IEmailService` sin cambiar consumidores.

## D2 — Origen del correo del cliente

**Problema**: La entidad `Invoice` sólo tiene `ClientId`; **no existe entidad `Client` ni campo de correo** en el modelo actual (verificado en el código: ninguna referencia a "email" salvo el contrato y su fake).

**Decisión**: Introducir la abstracción **`IClientEmailResolver`** (Application) con `Task<string?> ResolveEmailAsync(string clientId, CancellationToken)`. La implementación inicial (`ConfiguredClientEmailResolver`, Infrastructure) resuelve el correo desde una fuente configurable (mapa de configuración o convención derivada del `ClientId`) y devuelve `null` cuando no hay correo disponible. Si el resolver devuelve `null`/vacío, el envío **se omite y se registra como `Skipped`** (RF-010), sin abortar el lote.

**Justificación**: Desacopla la notificación de la inexistente gestión de clientes (DIP) y permite avanzar con la feature sin bloquear en un modelo de cliente completo. El comportamiento "sin correo ⇒ Skipped" es consistente con la clarificación de manejo de fallos y con RF-010.

**Alternativas consideradas**:
- Añadir `ClientEmail` directamente a `Invoice`: contamina el agregado de factura con datos de contacto del cliente y duplica información; descartado.
- Bloquear la feature hasta tener entidad `Client`: descartado, retrasa valor; la gestión de clientes es una feature futura del roadmap.

## D3 — Estrategia de plantillas por estado

**Decisión**: Una plantilla por tipo de notificación, seleccionada según el **nuevo estado** de la factura. Render simple por sustitución de placeholders (`{InvoiceId}`, `{Amount}`, `{ClientId}`, etc.) a partir de plantillas almacenadas como recursos/strings en Infrastructure. Mapa estado→plantilla:

| Nuevo estado | Tipo de notificación | Método de `IEmailService` |
|--------------|----------------------|---------------------------|
| `PrimerRecordatorio`, `SegundoRecordatorio` | Recordatorio | `SendReminderAsync` |
| `Pagado` | Confirmación de pago | `SendPaymentConfirmationAsync` |
| `Desactivado` | Aviso de desactivación | `SendDeactivationNoticeAsync` (nuevo, ver D5) |
| Otros (p. ej. `Draft`, `Pending`, `Overdue`, `Cancelled`) | — (sin plantilla) | no se envía; se registra `Skipped`/"no aplicable" |

**Justificación**: Mantiene la selección de plantilla en un único punto (el orquestador), evita un motor de plantillas pesado (Razor) para 3 plantillas simples y respeta OCP (añadir un estado/plantilla no obliga a tocar a los consumidores).

**Alternativas consideradas**:
- Motor de plantillas (RazorLight/Scriban): sobredimensionado para el alcance actual; se puede migrar luego sin cambiar el contrato.
- Plantillas en base de datos/configuración editable por admin: útil a futuro, fuera de alcance de esta spec.

## D4 — Ubicación de la orquestación "notificar al transicionar"

**Decisión**: Servicio de **Application** `InvoiceTransitionNotifier : IInvoiceTransitionNotifier` con la firma `Task NotifyTransitionAsync(Invoice invoice, InvoiceStatus previousStatus, CancellationToken)`. Responsabilidades: (1) seleccionar el tipo de notificación por `invoice.Status` (nuevo estado); (2) resolver el correo vía `IClientEmailResolver`; (3) invocar el método correspondiente de `IEmailService`; (4) registrar el resultado en la entidad (`RecordNotificationResult`) y, en envíos de recordatorio exitosos, `RecordReminderSent()`. **No persiste**: muta la entidad en memoria y el **llamador (worker o endpoint) ejecuta un único `UpdateAsync`**, evitando doble escritura.

**Justificación**: Centraliza la lógica compartida por worker y endpoints (clarificación de alcance = cualquier transición), cumpliendo DRY y SRP. Que el llamador persista una sola vez mantiene consistencia (estado + resultado de notificación en una sola escritura) y deja al orquestador libre de dependencias de persistencia (más fácil de testear).

**Alternativas consideradas**:
- Lógica duplicada en worker y en cada endpoint: viola DRY; descartado.
- Orquestador que persiste por sí mismo: provoca doble escritura (estado por el llamador + metadatos por el orquestador) y mezcla responsabilidades; descartado.
- Patrón de eventos de dominio (publicar `InvoiceTransitioned` y un handler que notifica): más desacoplado pero introduce infraestructura de mensajería/eventos no existente; sobredimensionado para el alcance actual. Se puede evolucionar hacia esto sin romper el contrato del notifier.

## D5 — Extensión de `IEmailService` para aviso de desactivación

**Decisión**: Añadir `Task SendDeactivationNoticeAsync(string clientEmail, Invoice invoice, CancellationToken ct = default)` al contrato `IEmailService`. Actualizar el `FakeEmailService` de pruebas y las pruebas de contrato.

**Justificación**: La clarificación estableció que `Desactivado` debe notificar al cliente; el contrato actual sólo cubre recordatorio y confirmación de pago. Mantener un método por tipo de notificación respeta ISP y deja el contrato explícito y testeable.

**Alternativas consideradas**:
- Reutilizar `SendReminderAsync` para la desactivación: confunde semántica (no es un recordatorio) y plantilla; descartado.
- Método genérico `SendAsync(NotificationType, ...)`: reduce expresividad del contrato y dificulta dobles de prueba específicos; descartado por ahora.

## D6 — Sink de logs estructurados persistente (JSON)

**Decisión**: Añadir a la configuración de Serilog (en `Program.cs`) un **sink de archivo** (`Serilog.Sinks.File`) con **`CompactJsonFormatter`** (`Serilog.Formatting.Compact`), con rolling diario y ruta configurable por entorno (p. ej. `Logging__File__Path`). Se conserva el sink de consola. El orquestador emite un log por factura procesada con propiedades estructuradas: `InvoiceId`, `PreviousStatus`, `NewStatus`, `NotificationType`, `NotificationOutcome` y, ante fallo, el motivo.

**Justificación**: La spec (3.4) exige logs estructurados en JSON y persistidos (archivo o nube). Hoy `Program.cs` sólo escribe a consola. `CompactJsonFormatter` produce JSON estructurado estándar; el sink de archivo cubre "persistido (file o cloud)" y es sustituible por un sink de nube sin cambiar el código de logging.

**Alternativas consideradas**:
- Sólo consola (confiando en que el runtime de contenedores recolecte stdout): no garantiza formato JSON estructurado consultable por sí mismo; la spec pide persistencia explícita.
- Sink directo a nube (Seq/Elastic): válido a futuro; se añade por configuración sin cambiar la instrumentación.

## D7 — Acoplamiento envío/transición, fallos y reintento

**Decisión**: La transición de estado y el envío son efectos relacionados pero **no transaccionales**. La transición ya aplicada **no se revierte** si el correo falla; ante fallo **no** se actualizan `RemindersCount`/`LastReminderSentAt`; el resultado se registra como `Failed` en la entidad y en el log. La factura se **reintenta de forma natural** en el siguiente ciclo del worker si sigue siendo elegible (no hay cola de reintentos dedicada). En endpoints manuales, un fallo de envío **no** convierte en error la respuesta de la transición: se responde con la transición aplicada y el resultado de notificación queda registrado.

**Justificación**: Coincide con las clarificaciones de la spec. Evita complejidad de compensación/rollback distribuido y mantiene el worker sin estado. El registro `Failed` + log da trazabilidad para diagnóstico.

**Alternativas consideradas**:
- Envío y transición atómicos con rollback: imposible de garantizar contra un proveedor SMTP externo (no participa en la transacción); descartado.
- Cola de reintentos / outbox pattern: mejora la entrega pero añade infraestructura; queda como mejora futura documentada.

## Notas de idempotencia (multi-réplica)

Con varias réplicas del worker, una misma factura podría ser evaluada por dos ciclos en paralelo y notificarse dos veces. Mitigación actual: la transición cambia `LastStatusTransitionAt` y la elegibilidad depende de ese valor, reduciendo la ventana; la actualización es por documento. Entrega exactamente-una-vez (lock distribuido / outbox) queda **fuera de alcance** de esta spec.
