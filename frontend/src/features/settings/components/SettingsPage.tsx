import { type LucideIcon, Monitor, Moon, Sun } from 'lucide-react'
import { useTheme } from '@/components/theme-provider'
import { useDocumentTitle } from '@/hooks/use-document-title'
import { cn } from '@/lib/utils'
import { AdminToolsSection } from './AdminToolsSection'
import { DangerZoneSection } from './DangerZoneSection'
import { EmailProviderSection } from './EmailProviderSection'
import { EmailTemplatesSection } from './EmailTemplatesSection'
import { TestEmailSection } from './TestEmailSection'

type ThemeOption = 'light' | 'dark' | 'system'

const THEME_OPTIONS: ReadonlyArray<{ value: ThemeOption; label: string; icon: LucideIcon }> = [
  { value: 'system', label: 'Sistema', icon: Monitor },
  { value: 'light', label: 'Claro', icon: Sun },
  { value: 'dark', label: 'Oscuro', icon: Moon },
]

/**
 * Vista de Configuración. Por ahora alberga la preferencia de tema
 * (claro/oscuro/sistema); se irán añadiendo más secciones.
 */
export function SettingsPage() {
  useDocumentTitle('Configuración')
  const { theme, setTheme } = useTheme()

  return (
    <section aria-labelledby="settings-title" className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 id="settings-title" className="font-heading text-2xl font-black tracking-tight">
          Configuración
        </h1>
        <p className="text-sm text-muted-foreground">Ajusta las preferencias del panel.</p>
      </header>

      {/* Cuadrícula responsive tipo masonry: una columna en móvil/tablet y dos en
          escritorio. Las tarjetas son independientes, por lo que evitamos cortes
          internos y dejamos que cada una conserve su altura natural. */}
      <div className="gap-6 lg:columns-2 [&>*]:mb-6 [&>*]:break-inside-avoid">
        <div className="rounded-lg border p-5">
          <div className="flex flex-col gap-1">
            <h2 className="font-heading text-base font-bold">Apariencia</h2>
            <p className="text-sm text-muted-foreground">
              Elige el tema de la interfaz. "Sistema" sigue la preferencia de tu dispositivo.
            </p>
          </div>

          <fieldset className="mt-4 min-w-0 border-0 p-0">
            <legend className="sr-only">Tema de la interfaz</legend>
            <div className="grid grid-cols-3 gap-2">
              {THEME_OPTIONS.map(({ value, label, icon: Icon }) => {
                const selected = theme === value
                return (
                  <button
                    key={value}
                    type="button"
                    aria-pressed={selected}
                    onClick={() => setTheme(value)}
                    className={cn(
                      'flex flex-col items-center gap-2 rounded-[2px] border px-3 py-4 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
                      selected
                        ? 'border-primary bg-primary/10 text-foreground'
                        : 'border-input text-muted-foreground hover:bg-accent hover:text-accent-foreground'
                    )}
                  >
                    <Icon className="h-5 w-5" aria-hidden="true" />
                    {label}
                  </button>
                )
              })}
            </div>
          </fieldset>
        </div>

        <EmailProviderSection />
        <EmailTemplatesSection />
        <TestEmailSection />
        <AdminToolsSection />
        <DangerZoneSection />
      </div>
    </section>
  )
}
