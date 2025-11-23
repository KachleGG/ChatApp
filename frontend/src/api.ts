export type CheckAuthResult = { authenticated: boolean }

const API_BASE = '' // relative to dev server; adjust if your API is hosted elsewhere

async function request(path: string, opts: RequestInit = {}) {
  const url = API_BASE + path
  const init: RequestInit = {
    credentials: 'include',
    headers: { 'Accept': 'application/json' },
    ...opts,
  }
  const res = await fetch(url, init)
  if (!res.ok) throw new Error(`HTTP ${res.status}`)
  return res.json()
}

export async function checkAuth(): Promise<CheckAuthResult> {
  try {
    // Try a simple GET to the auth endpoint; backend may return 404/401 which we treat as unauthenticated
    const data = await request('/api/auth/check')
    // Expect backend to return an object with `authenticated` boolean, but be lenient.
    return { authenticated: !!(data && data.authenticated) }
  } catch (e) {
    return { authenticated: false }
  }
}

export default {
  checkAuth,
}
