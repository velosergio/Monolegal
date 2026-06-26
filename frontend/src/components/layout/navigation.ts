import { FileText, LayoutDashboard, type LucideIcon, Settings, Users } from 'lucide-react'

export interface NavItem {
  /** Ruta de react-router (ignorada cuando `disabled`). */
  to: string
  label: string
  icon: LucideIcon
  disabled: boolean
}

/**
 * Ítems de la navegación lateral. Todas son rutas reales (spec 015 habilitó
 * "Dashboard").
 */
export const NAV_ITEMS: readonly NavItem[] = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, disabled: false },
  { to: '/facturas', label: 'Facturas', icon: FileText, disabled: false },
  { to: '/clientes', label: 'Clientes', icon: Users, disabled: false },
  { to: '/configuracion', label: 'Configuración', icon: Settings, disabled: false },
]
