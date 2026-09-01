import { useEffect, useMemo, useState } from 'react'
import { cancelCrawl, getCrawl, listIssues } from '../api/client.ts'
import type { Issue } from '../api/types.ts'
import { categoryLabel, formatDate, severityLabel, shortUrl } from '../components/format.ts'
import {
  Card,
  CategoryBars,
  Empty,
  ErrorBox,
  Pager,
  ScoreGauge,
  SeverityBadge,
  Spinner,
  StatusBadge,
} from '../components/ui.tsx'
import { useAction, useAsync } from '../hooks/useAsync.ts'

const severities = ['Critical', 'High', 'Medium', 'Low']
const categories = [
  'Indexability',
  'Meta',
  'Content',
  'Links',
  'Performance',
  'StructuredData',
  'Images',
  'I18n',
]
const pageSize = 25

/** Tarama devam ederken ozet bu araliklarla yeniden cekilir. */
const pollMs = 3000

export function CrawlPage({ crawlId }: { crawlId: string }) {
  const { data: crawl, error, loading, reload } = useAsync(() => getCrawl(crawlId), [crawlId])
  const [severity, setSeverity] = useState('')
  const [category, setCategory] = useState('')
  const [status, setStatus] = useState('Open')
  const [pageNumber, setPageNumber] = useState(1)
  const { busy, error: actionError, run } = useAction()

  const running = crawl?.status === 'Queued' || crawl?.status === 'Running'

  useEffect(() => {
    if (!running) return
    const timer = setInterval(reload, pollMs)
    return () => clearInterval(timer)
  }, [running, reload])

  const filter = useMemo(
    () => ({ severity, category, status, page: pageNumber, size: pageSize }),
    [severity, category, status, pageNumber],
  )

  const issues = useAsync(
    () => listIssues(crawlId, filter),
    [crawlId, filter, crawl?.status],
  )

  if (loading && !crawl) return <Spinner />
  if (error) return <ErrorBox message={error} onRetry={reload} />
  if (!crawl) return null

  const counts = crawl.issueCounts
  const cancel = () => void run(async () => { await cancelCrawl(crawlId); reload() })

  const resetFilters = (next: () => void) => {
    next()
    setPageNumber(1)
  }

  return (
    <div className="page">
      <nav className="crumbs">
        <a href="#/sites">Siteler</a> <span>/</span> <strong>Tarama</strong>
      </nav>

      <header className="page-head">
        <div>
          <h1>
            Tarama sonucu <StatusBadge status={crawl.status} />
          </h1>
          <p className="muted">
            {crawl.pagesCrawled}/{crawl.pagesDiscovered} sayfa · basladi {formatDate(crawl.startedAt)}
            {crawl.finishedAt && ` · bitti ${formatDate(crawl.finishedAt)}`}
          </p>
        </div>
        {running && (
          <button type="button" className="btn btn-ghost danger" onClick={cancel} disabled={busy}>
            {busy ? '…' : 'Taramayi iptal et'}
          </button>
        )}
      </header>

      {crawl.errorMessage && <ErrorBox message={crawl.errorMessage} />}
      {actionError && <ErrorBox message={actionError} />}
      {running && <div className="state">Tarama devam ediyor, sonuclar otomatik yenileniyor…</div>}

      <div className="split">
        <Card className="score-card">
          <ScoreGauge score={crawl.overallScore} />
          <ul className="count-list">
            {severities.map((key) => {
              const value = counts[key.toLowerCase()] ?? 0
              return (
                <li key={key}>
                  <SeverityBadge severity={key} />
                  <strong>{value}</strong>
                </li>
              )
            })}
          </ul>
        </Card>

        <Card>
          <h2 className="card-title">Kategori skorlari</h2>
          <CategoryBars scores={crawl.categoryScores} />
        </Card>
      </div>

      <Card>
        <div className="table-head">
          <h2 className="card-title">Bulgular</h2>
          <div className="filters">
            <select
              value={severity}
              onChange={(e) => resetFilters(() => setSeverity(e.target.value))}
              aria-label="Siddet filtresi"
            >
              <option value="">Tum siddetler</option>
              {severities.map((s) => (
                <option key={s} value={s}>
                  {severityLabel(s)}
                </option>
              ))}
            </select>
            <select
              value={category}
              onChange={(e) => resetFilters(() => setCategory(e.target.value))}
              aria-label="Kategori filtresi"
            >
              <option value="">Tum kategoriler</option>
              {categories.map((c) => (
                <option key={c} value={c}>
                  {categoryLabel(c)}
                </option>
              ))}
            </select>
            <select
              value={status}
              onChange={(e) => resetFilters(() => setStatus(e.target.value))}
              aria-label="Durum filtresi"
            >
              <option value="Open">Acik</option>
              <option value="Ignored">Yoksayilan</option>
              <option value="">Tumu</option>
            </select>
          </div>
        </div>

        {issues.loading && !issues.data && <Spinner />}
        {issues.error && <ErrorBox message={issues.error} onRetry={issues.reload} />}
        {issues.data && issues.data.items.length === 0 && (
          <Empty>Bu filtreye uyan bulgu yok.</Empty>
        )}

        {issues.data && issues.data.items.length > 0 && (
          <>
            <IssueTable crawlId={crawlId} issues={issues.data.items} />
            <Pager
              page={issues.data.page}
              size={issues.data.size}
              total={issues.data.total}
              onChange={setPageNumber}
            />
          </>
        )}
      </Card>
    </div>
  )
}

function IssueTable({ crawlId, issues }: { crawlId: string; issues: Issue[] }) {
  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>Siddet</th>
            <th>Kural</th>
            <th>Kategori</th>
            <th>Sayfa</th>
            <th>Kanit</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {issues.map((issue) => (
            <tr key={issue.id} className={issue.status === 'Ignored' ? 'muted-row' : undefined}>
              <td>
                <SeverityBadge severity={issue.severity} />
              </td>
              <td>
                <a href={`#/crawls/${crawlId}/rules/${issue.ruleCode}`}>
                  {issue.ruleTitle ?? issue.ruleCode}
                </a>
                <div className="cell-sub">{issue.ruleCode}</div>
              </td>
              <td>{issue.category ? categoryLabel(issue.category) : '—'}</td>
              <td className="cell-url">
                {issue.pageId ? (
                  <a href={`#/pages/${issue.pageId}`}>{shortUrl(issue.pageUrl)}</a>
                ) : (
                  <span className="muted">site geneli</span>
                )}
              </td>
              <td className="cell-evidence">{issue.found ?? '—'}</td>
              <td>
                {issue.status === 'Ignored' && <span className="badge">Yoksayildi</span>}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

