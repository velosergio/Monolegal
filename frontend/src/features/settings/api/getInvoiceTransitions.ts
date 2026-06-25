export interface InvoiceTransitionsConfig {
  pendingToFirstReminderDays: number
  firstToSecondReminderDays: number
  secondToDeactivatedDays: number
}

export const getInvoiceTransitions = async (): Promise<InvoiceTransitionsConfig> => {
  const response = await fetch('/api/settings/invoice-transitions')
  if (!response.ok) {
    throw new Error('Network response was not ok')
  }
  return response.json()
}
