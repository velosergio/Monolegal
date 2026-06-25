import { InvoiceTransitionsConfig } from './getInvoiceTransitions'

export const updateInvoiceTransitions = async (data: InvoiceTransitionsConfig): Promise<void> => {
  const response = await fetch('/api/settings/invoice-transitions', {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    throw new Error('Network response was not ok')
  }
}
