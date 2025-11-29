export type AuthUser = { id: number; name: string; email: string; isAdmin?: boolean }
export type CheckAuthResult = { authenticated: boolean; user?: AuthUser | null }

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
    // Expect backend to return { authenticated: boolean, user?: { ... } }
    return { authenticated: !!(data && data.authenticated), user: data.user ?? null }
  } catch (e) {
    return { authenticated: false, user: null }
  }
}

export type GroupSummary = { id: number; name: string; ownerId: number; ownerName?: string; createdAt?: string; isDeactivated?: boolean }

export async function fetchGroups(): Promise<GroupSummary[]> {
  try {
    return await request('/api/groups')
  } catch (e) {
    // Return empty array on failure
    return []
  }
}

export async function createGroup(name: string) {
  return await request('/api/groups', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  })
}

export async function fetchMessagesForGroup(groupId = 1, limit = 50, beforeId?: number) {
  const params = new URLSearchParams()
  params.set('limit', String(limit))
  params.set('groupId', String(groupId))
  if (beforeId) params.set('beforeId', String(beforeId))
  return await request('/api/messages?' + params.toString())
}

export async function postMessageToGroup(payload: { UserId: number; Message: string; GroupId?: number }) {
  return await request('/api/messages', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
}

export default {
  checkAuth,
  fetchGroups,
  createGroup,
  fetchMessagesForGroup,
  postMessageToGroup,
}
