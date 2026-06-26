/** Proveedor de envío de correo (valores del contrato, en minúscula). */
export type EmailProvider = 'smtp' | 'resend'

/** Estado de la credencial del proveedor (sin exponer el valor). */
export type CredentialStatus = 'notconfigured' | 'configured' | 'validated' | 'invalid'

/** Parámetros SMTP no secretos. */
export interface SmtpSettings {
  host: string | null
  port: number
  username: string | null
  useStartTls: boolean
}

/** Parámetros Resend no secretos. */
export interface ResendSettings {
  fromDomain: string | null
}

/** Configuración de email devuelta por `GET /api/settings/email` (sin secretos). */
export interface EmailSettings {
  activeProvider: EmailProvider
  fromAddress: string
  fromName: string
  smtp: SmtpSettings
  resend: ResendSettings
  credentialStatus: CredentialStatus
}

/** Cuerpo de `PUT /api/settings/email` (configuración no secreta a persistir). */
export interface EmailSettingsInput {
  activeProvider: EmailProvider
  fromAddress: string
  fromName: string
  smtp: {
    host: string
    port: number
    username: string
    useStartTls: boolean
  }
  resend: {
    fromDomain: string
  }
}

/** Resultado de `POST /api/settings/email/validate`. */
export interface ValidateCredentialsResult {
  provider: EmailProvider
  status: CredentialStatus
  message: string | null
}

/** Tipo de notificación / plantilla (valores del contrato, en minúscula). */
export type NotificationType = 'reminder' | 'paymentconfirmation' | 'deactivationnotice'

/** Plantilla efectiva de un tipo de notificación. */
export interface EmailTemplate {
  type: NotificationType
  subject: string
  body: string
  isCustomized: boolean
}

/** Respuesta de `GET /api/settings/email/templates`. */
export interface EmailTemplatesResponse {
  allowedVariables: string[]
  templates: EmailTemplate[]
}

/** Resultado de la vista previa de una plantilla. */
export interface TemplatePreview {
  subject: string
  body: string
}

/** Resultado de `POST /api/settings/email/test`. */
export interface SendTestEmailResult {
  to: string
  result: 'sent' | 'failed'
  message: string | null
}

/** Resultado de `POST /api/settings/email/tools/resend-failed`. */
export interface ResendFailedResult {
  attempted: number
  resent: number
  failed: number
}

/** Resultado de `POST /api/settings/email/tools/sanitize`. */
export interface SanitizeResult {
  sanitized: number
}

/** Resultado de `POST /api/settings/maintenance/delete-all-data`. */
export interface DeleteAllDataResult {
  deletedInvoices: number
}

/** Resultado de `POST /api/settings/maintenance/flush-database`. */
export interface FlushDatabaseResult {
  deletedInvoices: number
  seeded: boolean
  clientsCreated: number
  invoicesCreated: number
}
