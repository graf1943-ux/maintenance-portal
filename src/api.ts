/**
 * Roadbike Care API client.
 * Vite proxies /api to the ASP.NET Core API during local development.
 */
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api'

export type ApiError = { message?: string }

export async function apiGet<T>(path: string): Promise<T> {
  return request<T>(path, { method: 'GET' })
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  return request<T>(path, { method: 'POST', body: JSON.stringify(body) })
}

export async function apiPut<T>(path: string, body: unknown): Promise<T> {
  return request<T>(path, { method: 'PUT', body: JSON.stringify(body) })
}

export async function apiDelete(path: string): Promise<void> {
  await request<void>(path, { method: 'DELETE' })
}

async function request<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init.headers },
  })

  if (!response.ok) {
    let message = `API request failed: ${response.status}`
    try {
      const body = (await response.json()) as ApiError
      if (body.message) message = body.message
    } catch {
      // Keep the HTTP status message when the response is not JSON.
    }
    throw new Error(message)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}
