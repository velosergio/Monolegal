import { Component, type ErrorInfo, type ReactNode } from 'react'

interface ErrorBoundaryProps {
  children: ReactNode
  fallback?: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean
}

/**
 * Captura errores de render del sub-árbol y muestra una degradación elegante sin
 * tumbar el resto del panel (Constitución VI). Solo registra en desarrollo.
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false }

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    if (import.meta.env.DEV) {
      console.error('ErrorBoundary capturó un error:', error, info)
    }
  }

  render(): ReactNode {
    if (this.state.hasError) {
      return (
        this.props.fallback ?? (
          <div role="alert" className="p-6 text-center text-muted-foreground">
            <p className="font-medium text-foreground">Algo salió mal.</p>
            <p className="mt-1 text-sm">Recarga la página para continuar.</p>
          </div>
        )
      )
    }

    return this.props.children
  }
}
