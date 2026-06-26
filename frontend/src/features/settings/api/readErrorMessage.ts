/** Extrae un mensaje de error legible del cuerpo (`{ error }` o ValidationProblem). */
export async function readErrorMessage(response: Response, fallbackBase: string): Promise<string> {
  const fallback = `${fallbackBase} (${response.status}).`
  try {
    const body = (await response.json()) as {
      error?: string
      errors?: Record<string, string[]>
    }
    if (typeof body?.error === 'string' && body.error.length > 0) {
      return body.error
    }
    if (body?.errors) {
      const first = Object.values(body.errors).flat()[0]
      if (typeof first === 'string' && first.length > 0) return first
    }
  } catch {
    // Cuerpo no-JSON: usar el mensaje de respaldo.
  }
  return fallback
}
