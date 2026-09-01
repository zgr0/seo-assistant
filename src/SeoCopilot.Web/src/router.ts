import { useCallback, useSyncExternalStore } from 'react'

// Hash tabanli mini router — ek bagimlilik yok, statik servis (Caddy) icin sunucu
// tarafi rewrite gerektirmez.

function currentHash(): string {
  const hash = window.location.hash.replace(/^#/, '')
  return hash.length > 0 ? hash : '/'
}

function subscribe(callback: () => void): () => void {
  window.addEventListener('hashchange', callback)
  return () => window.removeEventListener('hashchange', callback)
}

export function useRoute(): string {
  return useSyncExternalStore(subscribe, currentHash, currentHash)
}

export function navigate(path: string) {
  if (currentHash() === path) return
  window.location.hash = path
}

export function useNavigate() {
  return useCallback((path: string) => navigate(path), [])
}

/**
 * Desen `/sites/:siteId/verify` bicimindedir; eslesirse yakalanan parametreler,
 * eslesmezse null doner.
 */
export function match(pattern: string, path: string): Record<string, string> | null {
  const patternParts = pattern.split('/').filter(Boolean)
  const pathParts = path.split('/').filter(Boolean)
  if (patternParts.length !== pathParts.length) return null

  const params: Record<string, string> = {}
  for (let i = 0; i < patternParts.length; i++) {
    const expected = patternParts[i]
    const actual = pathParts[i]
    if (expected.startsWith(':')) {
      params[expected.slice(1)] = decodeURIComponent(actual)
      continue
    }
    if (expected !== actual) return null
  }
  return params
}
