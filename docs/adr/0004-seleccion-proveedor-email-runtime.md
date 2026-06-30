# ADR 0004 — Selección del proveedor de correo en runtime con factory y fallback NoOp

**Estado**: Aceptada · **Fecha**: 2026-06-30 · **Feature**: 026 — Comentarios de Código y Documentación de Arquitectura

> ADR retroactivo: documenta una decisión vigente introducida en la spec 017.

## Contexto

El sistema debe enviar correos de notificación pudiendo usar distintos proveedores (SMTP vía MailKit
o la API de Resend), configurables por el administrador y modificables **sin reiniciar** el servicio.
Además, en Desarrollo/CI no debe exigirse un servidor de correo real para poder ejecutar los flujos de
transición y notificación.

## Decisión

1. Definir una abstracción `IEmailProvider` por proveedor (`SmtpEmailProvider`, `ResendEmailProvider`),
   registradas como múltiples implementaciones, y un `IEmailProviderFactory` que selecciona la activa
   en cada envío según `SystemSettings` (cambios efectivos en runtime).
2. Exponer un `IEmailService` de alto nivel: `SettingsBackedEmailService` cuando hay algún proveedor
   configurado, o `NoOpEmailService` (fallback) en Dev/CI sin proveedor, que registra un log y completa
   con éxito sin enviar correo real.
3. Los **secretos** (contraseña SMTP, API key de Resend) se leen solo del entorno; la configuración no
   secreta vive en `SystemSettings`.

## Alternativas consideradas

- **Un único proveedor fijo por configuración de arranque**: impediría cambiar de proveedor sin
  reiniciar y complicaría Dev/CI. Descartada.
- **`if/switch` de proveedor embebido en el servicio de envío**: viola OCP (cada nuevo proveedor obliga
  a modificar el servicio) y mezcla responsabilidades. Descartada en favor de la factory.

## Consecuencias

- **Positivas**: añadir un proveedor nuevo es registrar otro `IEmailProvider` (OCP); cambios de
  proveedor en caliente; entornos sin SMTP funcionan vía `NoOpEmailService` (LSP).
- **Negativas / costes**: la resolución por envío añade una indirección; hay que vigilar que ningún
  proveedor exponga secretos en logs.
