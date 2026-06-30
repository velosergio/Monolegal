import {
  BookOpen,
  FileText,
  LayoutDashboard,
  type LucideIcon,
  Send,
  Settings,
  Users,
} from 'lucide-react'

export interface NavItem {
  /** Ruta de react-router (ignorada cuando `disabled`). */
  to: string
  label: string
  icon: LucideIcon
  disabled: boolean
}

/** Ítem de navegación que apunta a un recurso externo (se abre en pestaña nueva). */
export interface ExternalNavItem {
  /** URL externa de destino. */
  href: string
  label: string
  icon: LucideIcon
  external: true
}

/**
 * Ítems de la navegación lateral. Todas son rutas reales (spec 015 habilitó
 * "Dashboard").
 */
export const NAV_ITEMS: readonly NavItem[] = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, disabled: false },
  { to: '/facturas', label: 'Facturas', icon: FileText, disabled: false },
  { to: '/envios', label: 'Envíos', icon: Send, disabled: false },
  { to: '/clientes', label: 'Clientes', icon: Users, disabled: false },
  { to: '/configuracion', label: 'Configuración', icon: Settings, disabled: false },
]

/**
 * URL de Swagger UI configurable por entorno (spec 025). Por defecto `/swagger`,
 * reenviado al backend por el proxy de desarrollo de Vite. Una cadena vacía
 * (p. ej. en producción con Swagger deshabilitado) oculta el acceso para evitar
 * un enlace roto.
 */
export function getSwaggerNavItem(): ExternalNavItem | null {
  const configured = import.meta.env.VITE_SWAGGER_URL
  const url = configured === undefined ? '/swagger' : configured
  if (!url) return null
  return { href: url, label: 'API (Swagger)', icon: BookOpen, external: true }
}
