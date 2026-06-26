import { lazy, Suspense } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import './index.css'
import { ErrorBoundary } from './components/feedback/ErrorBoundary'
import { AppShell } from './components/layout/AppShell'
import { Skeleton } from './components/ui/skeleton'
import { DashboardSkeleton } from './features/dashboard/components/DashboardSkeleton'
import { InvoicesTableSkeleton } from './features/invoices/components/InvoicesTableSkeleton'

const InvoicesPage = lazy(() =>
  import('./features/invoices/components/InvoicesPage').then((module) => ({
    default: module.InvoicesPage,
  }))
)

const SettingsPage = lazy(() =>
  import('./features/settings/components/SettingsPage').then((module) => ({
    default: module.SettingsPage,
  }))
)

const DashboardPage = lazy(() =>
  import('./features/dashboard/components/DashboardPage').then((module) => ({
    default: module.DashboardPage,
  }))
)

function App() {
  return (
    <BrowserRouter>
      <AppShell>
        <ErrorBoundary>
          <Routes>
            <Route path="/" element={<Navigate to="/facturas" replace />} />
            <Route
              path="/facturas"
              element={
                <Suspense fallback={<InvoicesTableSkeleton />}>
                  <InvoicesPage />
                </Suspense>
              }
            />
            <Route
              path="/dashboard"
              element={
                <Suspense fallback={<DashboardSkeleton />}>
                  <DashboardPage />
                </Suspense>
              }
            />
            <Route
              path="/configuracion"
              element={
                <Suspense fallback={<Skeleton className="h-48 w-full max-w-2xl" />}>
                  <SettingsPage />
                </Suspense>
              }
            />
            <Route path="*" element={<Navigate to="/facturas" replace />} />
          </Routes>
        </ErrorBoundary>
      </AppShell>
    </BrowserRouter>
  )
}

export default App
