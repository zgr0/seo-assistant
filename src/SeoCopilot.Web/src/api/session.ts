import type { AuthResult, UserInfo } from './types.ts'

// Oturum localStorage'da tutulur; access token kisa omurlu (15 dk), refresh ile yenilenir.

const KEY = 'seocopilot.session'

interface StoredSession {
  accessToken: string
  refreshToken: string
  user: UserInfo
}

type Listener = (session: StoredSession | null) => void

const listeners = new Set<Listener>()
let current: StoredSession | null = read()

function read(): StoredSession | null {
  try {
    const raw = localStorage.getItem(KEY)
    return raw ? (JSON.parse(raw) as StoredSession) : null
  } catch {
    return null
  }
}

function publish() {
  for (const listener of listeners) listener(current)
}

export function getSession(): StoredSession | null {
  return current
}

export function getAccessToken(): string | null {
  return current?.accessToken ?? null
}

export function getRefreshToken(): string | null {
  return current?.refreshToken ?? null
}

export function saveSession(auth: AuthResult) {
  current = {
    accessToken: auth.accessToken,
    refreshToken: auth.refreshToken,
    user: auth.user,
  }
  localStorage.setItem(KEY, JSON.stringify(current))
  publish()
}

/** Yalniz token cifti yenilenir — kullanici bilgisi korunur. */
export function updateTokens(auth: AuthResult) {
  saveSession(auth)
}

export function clearSession() {
  current = null
  localStorage.removeItem(KEY)
  publish()
}

export function subscribe(listener: Listener): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}
