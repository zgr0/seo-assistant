import { useEffect, useMemo, useState } from 'react'
import { cancelCrawl, getCrawl, listIssueGroups, listIssues } from '../api/client.ts'
import type { IssueGroup } from '../api/types.ts'
import { categoryLabel, formatDate, severityLabel, shortUrl } from '../components/format.ts'
import {
  Card,
  CategoryBars,
  Empty,
  ErrorBox,
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

/** Tarama devam ederken ozet bu araliklarla yeniden cekilir. */
const pollMs = 3000

/** Acilan satirda kac sayfa gosterilir; gerisi kural detayinda. */
const previewSize = 8

type StatusFilter = 'Open' | 'Ignored' | ''

/** Satirin durum filtresine karsilik gelen adedi. */
function countFor(group: IssueGroup, status: StatusFilter): number {
  if (status === 'Open') return group.openCount
  if (status === 'Ignored') return group.ignoredCount
  return group.totalCount
}

export function CrawlPage({ crawlId }: { crawlId: string }) {
  const { data: crawl, error, loading, reload } = useAsync(() => getCrawl(crawlId), [crawlId])
  const [severity, setSeverity] = useState('')
  const [category, setCategory] = useState('')
  const [status, setStatus] = useState<StatusFilter>('Open')
  const { busy, error: actionError, run } = useAction()

  const running = crawl?.status === 'Queued' || crawl?.status === 'Running'

  useEffect(() => {
    if (!running) return
    const timer = setInterval(reload, pollMs)
    return () => clearInterval(timer)
  }, [running, reload])

  // Ozetin tamami tek istekte gelir (satir sayisi kural katalogu kadar), filtreler
  // istemcide uygulanir. Boylece sekme adetleri ile satirlar hep ayni veriden turer.
  // pagesCrawled tarama surerken her yoklamada degistigi icin ozet de tazelenir.
  const groups = useAsync(
    () => listIssueGroups(crawlId),
    [crawlId, crawl?.status, crawl?.pagesCrawled],
  )

  const visible = useMemo(
    () =>
      (groups.data ?? []).filter(
        (g) =>
          countFor(g, status) > 0 &&
          (severity === '' || g.severity === severity) &&
          (category === '' || g.category === category),
      ),
    [groups.data, severity, category, status],
  )

  const tabCounts = useMemo(() => {
    const counts: Record<string, number> = { '': 0 }
    for (const key of severities) counts[key] = 0

    for (const group of groups.data ?? []) {
      if (category !== '' && group.category !== category) continue
      const count = countFor(group, status)
      counts[''] += count
      counts[group.severity] += count
    }
    return counts
  }, [groups.data, category, status])

  if (loading && !crawl) return <Spinner />
  if (error) return <ErrorBox message={error} onRetry={reload} />
  if (!crawl) return null

  const cancel = () => void run(async () => { await cancelCrawl(crawlId); reload() })

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
            {crawl.pagesCrawled}/{crawl.pagesDiscovered} sayfa · başladı {formatDate(crawl.startedAt)}
            {crawl.finishedAt && ` · bitti ${formatDate(crawl.finishedAt)}`}
          </p>
        </div>
        {running && (
          <button type="button" className="btn btn-ghost danger" onClick={cancel} disabled={busy}>
            {busy ? '…' : 'Taramayı iptal et'}
          </button>
        )}
      </header>

      {crawl.errorMessage && <ErrorBox message={crawl.errorMessage} />}
      {actionError && <ErrorBox message={actionError} />}
      {running && <div className="state">Tarama devam ediyor, sonuçlar otomatik yenileniyor…</div>}

      <div className="split">
        <Card className="score-card">
          <ScoreGauge score={crawl.overallScore} />
        </Card>

        <Card>
          <h2 className="card-title">Kategori skorları</h2>
          <CategoryBars scores={crawl.categoryScores} />
        </Card>
      </div>

      <Card>
        <div className="table-head">
          <h2 className="card-title">Bulgular</h2>
          <div className="filters">
            <select
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              aria-label="Kategori filtresi"
            >
              <option value="">Tüm kategoriler</option>
              {categories.map((c) => (
                <option key={c} value={c}>
                  {categoryLabel(c)}
                </option>
              ))}
            </select>
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value as StatusFilter)}
              aria-label="Durum filtresi"
            >
              <option value="Open">Açık</option>
              <option value="Ignored">Yoksayılan</option>
              <option value="">Tümü</option>
            </select>
          </div>
        </div>

        <SeverityTabs counts={tabCounts} selected={severity} onSelect={setSeverity} />

        {groups.loading && !groups.data && <Spinner />}
        {groups.error && <ErrorBox message={groups.error} onRetry={groups.reload} />}
        {groups.data && visible.length === 0 && <Empty>Bu filtreye uyan bulgu yok.</Empty>}
        {visible.length > 0 && (
          <IssueGroupTable crawlId={crawlId} groups={visible} status={status} />
        )}
      </Card>
    </div>
  )
}

/** Siddet sekmeleri; adetler satirlarla ayni ozetten turedigi icin filtreyle tutarli kalir. */
function SeverityTabs({
  counts,
  selected,
  onSelect,
}: {
  counts: Record<string, number>
  selected: string
  onSelect: (severity: string) => void
}) {
  const tone: Record<string, string> = { Critical: 'tone-bad', High: 'tone-warn' }

  return (
    <div className="tabs tabs-inline">
      <button
        type="button"
        className={`tab ${selected === '' ? 'active' : ''}`}
        onClick={() => onSelect('')}
      >
        Tümü <strong className="tab-count">{counts[''] ?? 0}</strong>
      </button>
      {severities.map((key) => (
        <button
          key={key}
          type="button"
          className={`tab ${selected === key ? 'active' : ''}`}
          onClick={() => onSelect(key)}
        >
          {severityLabel(key)}{' '}
          <strong className={`tab-count ${tone[key] ?? ''}`}>{counts[key] ?? 0}</strong>
        </button>
      ))}
    </div>
  )
}

/**
 * Satir = kural. Bir kural bir taramada onlarca sayfada tetiklenebilir; satir acilinca
 * etkilenen sayfalarin ilk {@link previewSize} tanesi yerinde listelenir.
 */
function IssueGroupTable({
  crawlId,
  groups,
  status,
}: {
  crawlId: string
  groups: IssueGroup[]
  status: StatusFilter
}) {
  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>Şiddet</th>
            <th>Kural</th>
            <th>Kategori</th>
            <th>Bulgu</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {groups.map((group) => (
            <IssueGroupRow
              key={group.ruleCode}
              crawlId={crawlId}
              group={group}
              status={status}
            />
          ))}
        </tbody>
      </table>
    </div>
  )
}

function IssueGroupRow({
  crawlId,
  group,
  status,
}: {
  crawlId: string
  group: IssueGroup
  status: StatusFilter
}) {
  const [open, setOpen] = useState(false)
  const toggle = () => setOpen((v) => !v)

  return (
    <>
      <tr className="row-toggle" onClick={toggle}>
        <td>
          <SeverityBadge severity={group.severity} />
        </td>
        <td>
          {group.ruleTitle}
          <div className="cell-sub">{group.ruleCode}</div>
        </td>
        <td>{categoryLabel(group.category)}</td>
        <td className="cell-count">
          {countFor(group, status)}
          {status === 'Open' && group.ignoredCount > 0 && (
            <div className="cell-sub">{group.ignoredCount} yoksayıldı</div>
          )}
        </td>
        <td className="cell-chevron">
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            aria-expanded={open}
            aria-label={`${group.ruleTitle} etkilenen sayfalar`}
            onClick={(e) => {
              // Satirin kendi tiklamasi da acip kapatiyor; ikisi ust uste binmesin.
              e.stopPropagation()
              toggle()
            }}
          >
            {open ? '▾' : '▸'}
          </button>
        </td>
      </tr>

      {open && (
        <tr className="inline-form-row">
          <td colSpan={5}>
            <AffectedPreview crawlId={crawlId} ruleCode={group.ruleCode} status={status} />
          </td>
        </tr>
      )}
    </>
  )
}

/** Yalnizca acik satir icin monte edilir; kapali satirlar istek uretmez. */
function AffectedPreview({
  crawlId,
  ruleCode,
  status,
}: {
  crawlId: string
  ruleCode: string
  status: StatusFilter
}) {
  const { data, error, loading, reload } = useAsync(
    () => listIssues(crawlId, { ruleCode, status, size: previewSize }),
    [crawlId, ruleCode, status],
  )

  if (loading && !data) return <Spinner />
  if (error) return <ErrorBox message={error} onRetry={reload} />
  if (!data) return null

  const rest = data.total - data.items.length

  return (
    <div className="group-detail">
      <ul className="url-list">
        {data.items.map((issue) => (
          <li key={issue.id}>
            {issue.pageId ? (
              <a href={`#/pages/${issue.pageId}`}>{shortUrl(issue.pageUrl)}</a>
            ) : (
              <span className="muted">site geneli</span>
            )}
            {issue.status === 'Ignored' && <span className="badge">Yoksayıldı</span>}
          </li>
        ))}
      </ul>
      <a className="btn btn-ghost btn-sm" href={`#/crawls/${crawlId}/rules/${ruleCode}`}>
        {rest > 0 ? `+${rest} sayfa daha · kural detayı` : 'Kural detayı ve düzeltme önerisi'}
      </a>
    </div>
  )
}
