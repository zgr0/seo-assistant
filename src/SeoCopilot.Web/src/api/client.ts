// Api katmanina ince istemci: Bearer basligi ekler, 401'de bir kez refresh dener,
// ProblemDetails yanitlarini okunabilir hataya cevirir.

import {
  clearSession,
  getAccessToken,
  getRefreshToken,
  saveSession,
  updateTokens,
} from './session.ts'
import type {
  AuthResult,
  ContentJob,
  CrawlListItem,
  CrawlSummary,
  Dashboard,
  Issue,
  IssueFilter,
  IssueGroup,
  Page,
  PageDetail,
  Paged,
  Site,
  SiteVitals,
} from './types.ts'

const base = '/api'

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

interface RequestOptions {
  method?: string
  body?: unknown
  /** Refresh akisinin kendini tetiklememesi icin. */
  anonymous?: boolean
}

let refreshing: Promise<boolean> | null = null

async function tryRefresh(): Promise<boolean> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) return false

  // Es zamanli 401'ler tek bir yenileme istegini paylasir.
  refreshing ??= (async () => {
    try {
      const res = await fetch(`${base}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      })
      if (!res.ok) return false
      updateTokens((await res.json()) as AuthResult)
      return true
    } catch {
      return false
    } finally {
      // Sonraki 401 yeni bir deneme baslatabilsin.
      setTimeout(() => (refreshing = null), 0)
    }
  })()

  return refreshing
}

async function toError(res: Response): Promise<ApiError> {
  let message = `Istek basarisiz (${res.status})`
  try {
    const problem = (await res.json()) as { title?: string; detail?: string }
    message = problem.detail || problem.title || message
  } catch {
    // govde bos ya da JSON degil — varsayilan mesaj kalir
  }
  return new ApiError(res.status, message)
}

async function send(path: string, options: RequestOptions): Promise<Response> {
  const headers: Record<string, string> = {}
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'

  const token = getAccessToken()
  if (token && !options.anonymous) headers.Authorization = `Bearer ${token}`

  return fetch(`${base}${path}`, {
    method: options.method ?? 'GET',
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  })
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  let res = await send(path, options)

  if (res.status === 401 && !options.anonymous) {
    if (await tryRefresh()) {
      res = await send(path, options)
    } else {
      clearSession()
      throw new ApiError(401, 'Oturum suresi doldu, tekrar giris yapin')
    }
  }

  if (!res.ok) throw await toError(res)
  if (res.status === 204) return undefined as T

  const text = await res.text()
  return (text ? JSON.parse(text) : undefined) as T
}

function query(params: Record<string, string | number | boolean | undefined>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === '') continue
    search.set(key, String(value))
  }
  const text = search.toString()
  return text ? `?${text}` : ''
}

// --- auth ---

export async function login(email: string, password: string): Promise<AuthResult> {
  const auth = await request<AuthResult>('/auth/login', {
    method: 'POST',
    body: { email, password },
    anonymous: true,
  })
  saveSession(auth)
  return auth
}

export async function register(input: {
  email: string
  password: string
  fullName: string
  tenantName: string
}): Promise<AuthResult> {
  const auth = await request<AuthResult>('/auth/register', {
    method: 'POST',
    body: input,
    anonymous: true,
  })
  saveSession(auth)
  return auth
}

export async function logout(): Promise<void> {
  const refreshToken = getRefreshToken()
  clearSession()
  if (!refreshToken) return
  try {
    await fetch(`${base}/auth/logout`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    })
  } catch {
    // cikis her halukarda yerelde tamamlandi
  }
}

// --- dashboard & sites ---

export const getDashboard = () => request<Dashboard>('/dashboard')

export const listSites = () => request<Site[]>('/sites')

export const getSite = (siteId: string) => request<Site>(`/sites/${siteId}`)

export const createSite = (name: string, baseUrl: string) =>
  request<Site>('/sites', { method: 'POST', body: { name, baseUrl } })

export const deleteSite = (siteId: string) =>
  request<void>(`/sites/${siteId}`, { method: 'DELETE' })

export const startCrawl = (siteId: string) =>
  request<{ crawlId: string }>(`/sites/${siteId}/crawls`, { method: 'POST' })

export const listCrawls = (siteId: string, page = 1, size = 20) =>
  request<Paged<CrawlListItem>>(`/sites/${siteId}/crawls${query({ page, size })}`)

export const getSiteVitals = (siteId: string) =>
  request<SiteVitals>(`/sites/${siteId}/vitals`)

// --- crawls ---

export const getCrawl = (crawlId: string) => request<CrawlSummary>(`/crawls/${crawlId}`)

export const cancelCrawl = (crawlId: string) =>
  request<CrawlSummary>(`/crawls/${crawlId}/cancel`, { method: 'POST' })

export const listIssues = (crawlId: string, filter: IssueFilter = {}) =>
  request<Paged<Issue>>(`/crawls/${crawlId}/issues${query({ ...filter })}`)

/** Kural bazli ozet — sayfalanmaz, satir sayisi kural katalogu kadardir. */
export const listIssueGroups = (
  crawlId: string,
  filter: { severity?: string; minSeverity?: string; category?: string } = {},
) => request<IssueGroup[]>(`/crawls/${crawlId}/issues/summary${query({ ...filter })}`)

export const listPages = (
  crawlId: string,
  filter: { url?: string; statusCode?: number; depth?: number; page?: number; size?: number } = {},
) => request<Paged<Page>>(`/crawls/${crawlId}/pages${query({ ...filter })}`)

export const getPage = (pageId: string) => request<PageDetail>(`/pages/${pageId}`)

// --- issues ---

export const ignoreIssue = (issueId: number, reason: string, applyToSite: boolean) =>
  request<Issue>(`/issues/${issueId}/ignore`, {
    method: 'POST',
    body: { reason, applyToSite },
  })

export const reopenIssue = (issueId: number) =>
  request<Issue>(`/issues/${issueId}/reopen`, { method: 'POST' })

// --- icerik uretimi (LLM) ---

export const generateContent = (input: {
  type: string
  pageId?: string
  input?: Record<string, unknown>
  variantCount?: number
}) => request<ContentJob>('/content/generate', { method: 'POST', body: input })

export const getContentJob = (jobId: string) => request<ContentJob>(`/content/jobs/${jobId}`)
