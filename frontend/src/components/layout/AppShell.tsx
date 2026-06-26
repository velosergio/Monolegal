import { Menu } from 'lucide-react'
import { type ReactNode, useCallback, useState } from 'react'
import { ToastViewport } from '@/components/feedback/ToastViewport'
import { Button } from '@/components/ui/button'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { cn } from '@/lib/utils'
import { Sidebar } from './Sidebar'

interface AppShellProps {
  children: ReactNode
}

const COLLAPSE_KEY = 'ml-sidebar-collapsed'

function readCollapsed(): boolean {
  if (typeof window === 'undefined') return false
  return window.localStorage.getItem(COLLAPSE_KEY) === '1'
}

/**
 * Estructura base del panel (patrón de dashboard moderno): sidebar de marca a
 * altura completa a la izquierda (persistente y colapsable en escritorio, en
 * `Sheet` deslizante en móvil) con el logo arriba y el pie anclado abajo; a la
 * derecha el área principal. Sin barra superior: en móvil un botón flotante
 * abre el menú. Dark mode heredado del `ThemeProvider`.
 */
export function AppShell({ children }: AppShellProps) {
  const [menuOpen, setMenuOpen] = useState(false)
  const [collapsed, setCollapsed] = useState(readCollapsed)

  const toggleCollapse = useCallback(() => {
    setCollapsed((prev) => {
      const next = !prev
      window.localStorage.setItem(COLLAPSE_KEY, next ? '1' : '0')
      return next
    })
  }, [])

  return (
    <div className="flex min-h-screen">
      <aside
        className={cn(
          'hidden shrink-0 border-r border-sidebar-border bg-sidebar transition-[width] duration-200 md:sticky md:top-0 md:block md:h-screen',
          collapsed ? 'md:w-16' : 'md:w-64'
        )}
      >
        <Sidebar collapsed={collapsed} onToggleCollapse={toggleCollapse} />
      </aside>

      <Sheet open={menuOpen} onOpenChange={setMenuOpen}>
        <SheetContent
          side="left"
          className="w-64 border-sidebar-border bg-sidebar p-0 text-sidebar-foreground"
        >
          <SheetHeader className="sr-only">
            <SheetTitle>Navegación</SheetTitle>
            <SheetDescription>Menú principal del panel de administración</SheetDescription>
          </SheetHeader>
          <Sidebar onNavigate={() => setMenuOpen(false)} />
        </SheetContent>
      </Sheet>

      <div className="flex min-w-0 flex-1 flex-col">
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="fixed left-4 top-4 z-30 bg-background shadow-sm md:hidden"
          aria-label="Abrir menú"
          aria-controls="sidebar-navigation"
          aria-expanded={menuOpen}
          onClick={() => setMenuOpen(true)}
        >
          <Menu className="h-5 w-5" aria-hidden="true" />
        </Button>

        {/* Entrada por opacidad vía CSS (tw-animate-css); la regla global de
            prefers-reduced-motion la atenúa automáticamente. Mantener Motion
            fuera del shell deja la librería en el chunk diferido de InvoicesPage. */}
        <main
          id="main"
          tabIndex={-1}
          className="flex-1 px-4 py-6 pt-16 duration-300 animate-in fade-in md:px-6 md:pt-6 lg:px-8"
        >
          {children}
        </main>
      </div>

      <ToastViewport />
    </div>
  )
}
