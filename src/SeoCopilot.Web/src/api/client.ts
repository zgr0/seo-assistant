// Api katmanina ince istemci. Token varsa Authorization header ekler.

const base = '/api'

function authHeaders(): HeadersInit {
  const token = localStorage.getItem('token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

export interface StartCrawlRequest {
  url: string
  ownerEmail: string
}

export interface StartCrawlResponse {
  siteId: string
  crawlId: string
}

export interface Finding {
  ruleCode: string
  severity: string
  message: string
}

export interface Page {
  url: string
  statusCode: number
  title: string | null
  findings: Finding[]
}

export interface CrawlSummary {
  crawlId: string
  status: string
  score: number
  startedAt: string
  finishedAt: string | null
  pages: Page[]
}

export async function startCrawl(req: StartCrawlRequest): Promise<StartCrawlResponse> {
  const res = await fetch(`${base}/crawls`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(req),
  })
  if (!res.ok) throw new Error(`startCrawl ${res.status}`)
  return res.json()
}

export async function getCrawl(crawlId: string): Promise<CrawlSummary> {
  const res = await fetch(`${base}/crawls/${crawlId}`, { headers: authHeaders() })
  if (!res.ok) throw new Error(`getCrawl ${res.status}`)
  return res.json()
}
