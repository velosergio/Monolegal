import { FileText, LayoutDashboard, type LucideIcon, Settings } from 'lucide-react'

export interface NavItem {
  /** Ruta de react-router (ignorada cuando `disabled`). */
  to: string
  label: string
  icon: LucideIcon
  disabled: boolean
}

/**
 * Ítems de la navegación lateral. "Dashboard" queda deshabilitado
 * ("Próximamente"); "Facturas" y "Configuración" son rutas reales.
 */
export const NAV_ITEMS: readonly NavItem[] = [
  { to: '/facturas', label: 'Facturas', icon: FileText, disabled: false },
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, disabled: true },
  { to: '/configuracion', label: 'Configuración', icon: Settings, disabled: false },
]
