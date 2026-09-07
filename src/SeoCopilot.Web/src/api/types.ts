// Api DTO'larinin birebir karsiligi (System.Text.Json varsayilani: camelCase).

export interface UserInfo {
  id: string
  email: string
  fullName: string
  role: string
  tenantId: string
}

export interface AuthResult {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
  user: UserInfo
}

export interface CrawlSettings {
  maxPages: number
  maxDepth: number
  delayMs: number
  renderJs: boolean
  concurrency: number
  includePatterns: string[]
  excludePatterns: string[]
}

export interface Site {
  id: string
  name: string
  baseUrl: string
  isActive: boolean
  createdAt: string
  crawlSettings: CrawlSettings
  scheduleCron: string | null
  defaultBrandProfileId: string | null
}

export type Severity = 'Low' | 'Medium' | 'High' | 'Critical'

export interface Issue {
  id: number
  ruleCode: string
  severity: Severity
  weight: number
  status: string
  pageId: string | null
  pageUrl: string | null
  category: string | null
  ruleTitle: string | null
  ruleDescription: string | null
  howToFix: string | null
  found: string | null
  expected: string | null
  sampleUrls: string[]
}

/**
 * Bulgu listesinin kural bazli ozeti. Adetler durum filtresinden bagimsizdir: acik ve
 * yoksayilan her zaman birlikte doner, hangisinin gosterilecegine istemci karar verir.
 */
export interface IssueGroup {
  ruleCode: string
  ruleTitle: string
  severity: Severity
  category: string
  weight: number
  openCount: number
  ignoredCount: number
  totalCount: number
}

export interface CrawlSummary {
  crawlId: string
  siteId: string
  status: string
  overallScore: number | null
  pagesDiscovered: number
  pagesCrawled: number
  startedAt: string | null
  finishedAt: string | null
  errorMessage: string | null
  categoryScores: Record<string, number>
  issueCounts: Record<string, number>
  issues: Issue[]
}

export interface CrawlListItem {
  crawlId: string
  siteId: string
  status: string
  trigger: string
  overallScore: number | null
  pagesDiscovered: number
  pagesCrawled: number
  startedAt: string | null
  finishedAt: string | null
  errorMessage: string | null
  issueCounts: Record<string, number>
  createdAt: string
}

export interface Page {
  id: string
  url: string
  depth: number
  statusCode: number
  contentType: string | null
  redirectTo: string | null
  responseTimeMs: number | null
  htmlSizeBytes: number | null
  title: string | null
  titleLength: number | null
  metaDescription: string | null
  metaDescLength: number | null
  h1Texts: string[]
  h2Count: number
  wordCount: number
  canonicalUrl: string | null
  robotsMeta: string | null
  schemaTypes: string[]
  imagesTotal: number
  imagesNoAlt: number
  inlinkCount: number
  outlinkInternal: number
  outlinkExternal: number
  lang: string | null
  crawledAt: string
}

export interface PageDetail {
  page: Page
  crawlId: string
  ogData: string | null
  mainText: string | null
  issues: Issue[]
}

export interface Paged<T> {
  items: T[]
  total: number
  page: number
  size: number
}

export interface Vital {
  id: number
  siteId: string
  crawlId: string | null
  url: string
  device: string
  source: string
  lcpMs: number | null
  inpMs: number | null
  cls: number | null
  ttfbMs: number | null
  fcpMs: number | null
  perfScore: number | null
  collectedAt: string
}

export interface SiteVitals {
  siteId: string
  latest: Vital | null
  history: Vital[]
}

export interface DashboardSite {
  siteId: string
  name: string
  baseUrl: string
  isActive: boolean
  lastCrawlId: string | null
  lastCrawlStatus: string | null
  lastScore: number | null
  scoreDelta: number | null
  lastCrawlAt: string | null
  issueCounts: Record<string, number>
}

export interface Dashboard {
  siteCount: number
  averageScore: number | null
  openIssueCounts: Record<string, number>
  sites: DashboardSite[]
  recentCrawls: CrawlListItem[]
  recentContentJobs: ContentJob[]
}

export interface ContentVariant {
  id: string
  variantIndex: number
  angle: string | null
  body: string
  hashtags: string[]
  cta: string | null
  charCount: number
  isFavorite: boolean
}

export interface ContentJob {
  id: string
  type: string
  status: 'Queued' | 'Running' | 'Done' | 'Failed'
  platformCode: string | null
  siteId: string | null
  pageId: string | null
  pageUrl: string | null
  brandProfileId: string | null
  input: string
  model: string | null
  tokensIn: number
  tokensOut: number
  costUsd: number
  errorMessage: string | null
  createdAt: string
  completedAt: string | null
  variants: ContentVariant[]
}

export interface IssueFilter {
  severity?: string
  minSeverity?: string
  category?: string
  ruleCode?: string
  status?: string
  pageId?: string
  page?: number
  size?: number
}
