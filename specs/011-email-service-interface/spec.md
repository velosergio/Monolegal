# Especificación de Funcionalidad: Email Service Interface

**Rama de Funcionalidad**: `011-email-service-interface`

**Creado**: 2026-06-25

**Estado**: Draft

**Entrada**: User description: "### Spec 3.1: Email Service Interface — GIVEN necesidad de enviar correos WHEN se define contrato THEN interfaz `IEmailService`: SendReminderAsync(string clientEmail, Invoice invoice), SendPaymentConfirmationAsync(string clientEmail, Invoice invoice)"

## Escenarios de Usuario y Pruebas *(obligatorio)*

### Historia de Usuario 1 - Contrato para Envío de Recordatorios (Prioridad: P1)

Como sistema de gestión de cartera, necesito un contrato abstracto para el envío de correos de recordatorio de cobro, de modo que el worker de transiciones de estado pueda notificar a los clientes sin depender de un proveedor de correo específico.

**Por qué esta prioridad**: El envío de recordatorios es el mecanismo central del flujo de cobro automatizado; sin un contrato definido, el worker (Fase 3) no puede acoplarse a la capacidad de notificación de forma testeable y desacoplada.

**Prueba Independiente**: Se puede probar de forma aislada creando una implementación falsa (mock/stub) del contrato y verificando que recibe correctamente el correo del cliente y los datos de la factura al solicitar un recordatorio, sin necesidad de un proveedor de correo real.

**Escenarios de Aceptación**:

1. **Dado** un cliente con una dirección de correo válida y una factura pendiente de pago, **Cuando** un componente solicita el envío de un recordatorio a través del contrato, **Entonces** la operación recibe el correo del cliente y los datos de la factura y se completa de forma asíncrona.
2. **Dado** una implementación del contrato registrada en el contenedor de dependencias, **Cuando** otro componente solicita el contrato `IEmailService`, **Entonces** recibe la implementación concreta sin conocer sus detalles internos.

---

### Historia de Usuario 2 - Contrato para Confirmación de Pago (Prioridad: P1)

Como sistema de gestión de cartera, necesito un contrato abstracto para el envío de correos de confirmación de pago, de modo que se pueda notificar al cliente cuando una factura pasa a estado `pagado`.

**Por qué esta prioridad**: La confirmación de pago cierra el ciclo de comunicación con el cliente y es parte esencial de la experiencia de cobro; debe estar disponible desde el mismo contrato que los recordatorios.

**Prueba Independiente**: Se puede probar de forma aislada con una implementación falsa del contrato verificando que la operación de confirmación recibe el correo del cliente y la factura pagada, completándose de forma asíncrona.

**Escenarios de Aceptación**:

1. **Dado** un cliente con una dirección de correo válida y una factura en estado `pagado`, **Cuando** un componente solicita el envío de la confirmación de pago a través del contrato, **Entonces** la operación recibe el correo del cliente y los datos de la factura y se completa de forma asíncrona.

### Casos Límite

- ¿Qué ocurre cuando la dirección de correo del cliente es nula, vacía o tiene un formato inválido? (La validación y el manejo del error son responsabilidad de la implementación, pero el contrato debe permitir señalar el fallo).
- ¿Qué ocurre cuando el proveedor de correo subyacente no está disponible o falla? (El contrato debe permitir que la implementación propague o reporte el error de envío).
- ¿Qué ocurre si se invoca una operación de envío con una factura nula? (Debe poder rechazarse el envío).

## Requisitos *(obligatorio)*

### Requisitos Funcionales

- **RF-001**: El sistema DEBE definir un contrato abstracto `IEmailService` en la capa de aplicación/dominio que desacople a los consumidores del proveedor de correo concreto.
- **RF-002**: El contrato DEBE exponer una operación asíncrona para enviar un correo de recordatorio que reciba la dirección de correo del cliente y la factura asociada.
- **RF-003**: El contrato DEBE exponer una operación asíncrona para enviar un correo de confirmación de pago que reciba la dirección de correo del cliente y la factura asociada.
- **RF-004**: Ambas operaciones DEBEN ser asíncronas (devolver una tarea/`Task`) para no bloquear el hilo del proceso que las invoca.
- **RF-005**: El contrato DEBE poder registrarse e inyectarse mediante el contenedor de inyección de dependencias del proyecto, de modo que los consumidores dependan de la abstracción y no de una implementación concreta.
- **RF-006**: El contrato DEBE permitir que las implementaciones señalen fallos de envío (por ejemplo, mediante excepciones) sin imponer un proveedor de correo específico.

### Entidades Clave

- **IEmailService**: Contrato (interfaz) de la capa de aplicación que define las capacidades de notificación por correo del sistema. No contiene lógica de envío, solo declara las operaciones disponibles.
- **Invoice**: Entidad de dominio existente que se pasa como dato a las operaciones de envío para componer el contenido del correo (cliente, monto, estado, recordatorios). Definida en `005-invoice-entity`.

## Criterios de Éxito *(obligatorio)*

### Resultados Medibles

- **CE-001**: El 100% de los componentes que necesitan enviar correos (por ejemplo, el worker de transiciones) dependen exclusivamente del contrato `IEmailService` y no de implementaciones concretas.
- **CE-002**: El contrato puede ser sustituido por una implementación falsa en pruebas, logrando que los escenarios de envío de recordatorio y confirmación de pago se verifiquen sin un proveedor de correo real.
- **CE-003**: Las dos capacidades de notificación (recordatorio y confirmación de pago) quedan disponibles desde un único contrato y son invocables de forma asíncrona.

## Suposiciones

- La implementación concreta del contrato (proveedor de correo, plantillas, configuración SMTP/API) se aborda en specs posteriores de la Fase 3 (Spec 3.3); esta spec solo define el contrato.
- La entidad `Invoice` ya está definida (`005-invoice-entity`) y contiene los datos necesarios (cliente, monto, estado, `RemindersCount`, `LastReminderSentAt`) para componer los correos.
- La dirección de correo del cliente se recibe como parámetro de texto; la obtención y validación de dicho correo es responsabilidad del componente que invoca el contrato y de la implementación.
- El contrato se ubica en la capa de aplicación siguiendo la arquitectura por capas del proyecto (Domain / Application / Infrastructure / Api).
