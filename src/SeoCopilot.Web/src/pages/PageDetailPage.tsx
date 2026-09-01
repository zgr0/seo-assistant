import { getCrawl, getPage, getSiteVitals } from '../api/client.ts'
import type { PageDetail, SiteVitals } from '../api/types.ts'
import { categoryLabel, formatDate } from '../components/format.ts'
import { Card, Empty, ErrorBox, SeverityBadge, Spinner } from '../components/ui.tsx'
import { useAsync } from '../hooks/useAsync.ts'

interface Loaded {
  detail: PageDetail
  siteId: string
  vitals: SiteVitals | null
}

async function load(pageId: string): Promise<Loaded> {
  const detail = await getPage(pageId)
  const crawl = await getCrawl(detail.crawlId)

  // PSI olcumu opsiyonel (PageSpeed:ApiKey tanimli degilse hic yazilmaz).
  let vitals: SiteVitals | null = null
  try {
    vitals = await getSiteVitals(crawl.siteId)
  } catch {
    vitals = null
  }

  return { detail, siteId: crawl.siteId, vitals }
}

export function PageDetailPage({ pageId }: { pageId: string }) {
  const { data, error, loading, reload } = useAsync(() => load(pageId), [pageId])

  if (loading && !data) return <Spinner />
  if (error) return <ErrorBox message={error} onRetry={reload} />
  if (!data) return null

  const { detail, vitals } = data
  const page = detail.page
  const og = parseOg(detail.ogData)

  return (
    <div className="page">
      <nav className="crumbs">
        <a href="#/sites">Siteler</a> <span>/</span>{' '}
        <a href={`#/crawls/${detail.crawlId}`}>Tarama</a> <span>/</span> <strong>Sayfa</strong>
      </nav>

      <header className="page-head">
        <div>
          {/* title bos string de olabilir — bu durumda da yer tutucu gosterilir */}
          <h1>{page.title || 'Basliksiz sayfa'}</h1>
          <a className="site-url" href={page.url} target="_blank" rel="noreferrer">
            {page.url}
          </a>
        </div>
        <div className="site-tags">
          <span className={`badge ${page.statusCode >= 400 || page.statusCode === 0 ? 'sev-critical' : 'ok'}`}>
            HTTP {page.statusCode || '—'}
          </span>
          <span className="badge">Derinlik {page.depth}</span>
        </div>
      </header>

      <div className="split">
        <Card>
          <h2 className="card-title">Cikarilan meta veriler</h2>
          <dl className="meta-list">
            <Row label="Title" value={page.title} extra={lengthNote(page.titleLength, 30, 60)} />
            <Row
              label="Meta description"
              value={page.metaDescription}
              extra={lengthNote(page.metaDescLength, 50, 160)}
            />
            <Row label="H1" value={page.h1Texts.join(' | ') || null} extra={`${page.h1Texts.length} adet`} />
            <Row label="H2 sayisi" value={String(page.h2Count)} />
            <Row label="Canonical" value={page.canonicalUrl} />
            <Row label="Robots meta" value={page.robotsMeta} />
            <Row label="Dil" value={page.lang} />
            <Row label="Schema" value={page.schemaTypes.join(', ') || null} />
            <Row label="Kelime sayisi" value={String(page.wordCount)} />
            <Row
              label="Gorseller"
              value={`${page.imagesTotal} adet`}
              extra={page.imagesNoAlt > 0 ? `${page.imagesNoAlt} tanesinde alt yok` : 'hepsinde alt var'}
            />
            <Row
              label="Linkler"
              value={`${page.inlinkCount} gelen · ${page.outlinkInternal} ic · ${page.outlinkExternal} dis`}
            />
            <Row
              label="Yanit"
              value={page.responseTimeMs !== null ? `${page.responseTimeMs} ms` : null}
              extra={page.htmlSizeBytes !== null ? bytes(page.htmlSizeBytes) : undefined}
            />
            <Row label="Content-Type" value={page.contentType} />
            <Row label="Yonlendirme" value={page.redirectTo} />
            <Row label="Tarandi" value={formatDate(page.crawledAt)} />
            {og && <Row label="Open Graph" value={og} />}
          </dl>
        </Card>

        <Card>
          <h2 className="card-title">Core Web Vitals</h2>
          <VitalsPanel vitals={vitals} />
        </Card>
      </div>

      <Card>
        <h2 className="card-title">Bu sayfanin bulgulari ({detail.issues.length})</h2>
        {detail.issues.length === 0 ? (
          <Empty>Bu sayfada bulgu yok.</Empty>
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Siddet</th>
                  <th>Kural</th>
                  <th>Kategori</th>
                  <th>Kanit</th>
                </tr>
              </thead>
              <tbody>
                {detail.issues.map((issue) => (
                  <tr key={issue.id} className={issue.status === 'Ignored' ? 'muted-row' : undefined}>
                    <td>
                      <SeverityBadge severity={issue.severity} />
                    </td>
                    <td>
                      <a href={`#/crawls/${detail.crawlId}/rules/${issue.ruleCode}`}>
                        {issue.ruleTitle ?? issue.ruleCode}
                      </a>
                      <div className="cell-sub">{issue.ruleCode}</div>
                    </td>
                    <td>{issue.category ? categoryLabel(issue.category) : '—'}</td>
                    <td className="cell-evidence">{issue.found ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {detail.mainText && (
        <Card>
          <h2 className="card-title">Ana metin</h2>
          <pre className="main-text">{detail.mainText}</pre>
        </Card>
      )}
    </div>
  )
}

function VitalsPanel({ vitals }: { vitals: SiteVitals | null }) {
  const latest = vitals?.latest
  if (!latest) {
    return (
      <Empty>
        PSI olcumu yok. <code>PageSpeed:ApiKey</code> tanimliysa her taramada kok sayfa icin olculur.
      </Empty>
    )
  }

  return (
    <>
      <ul className="vitals">
        <Vital label="Performans" value={latest.perfScore} tone={score(latest.perfScore, 90, 50)} />
        <Vital label="LCP" value={ms(latest.lcpMs)} tone={threshold(latest.lcpMs, 2500, 4000)} />
        <Vital label="INP" value={ms(latest.inpMs)} tone={threshold(latest.inpMs, 200, 500)} />
        <Vital
          label="CLS"
          value={latest.cls === null ? '—' : latest.cls.toFixed(3)}
          tone={threshold(latest.cls, 0.1, 0.25)}
        />
        <Vital label="TTFB" value={ms(latest.ttfbMs)} tone={threshold(latest.ttfbMs, 800, 1800)} />
        <Vital label="FCP" value={ms(latest.fcpMs)} tone={threshold(latest.fcpMs, 1800, 3000)} />
      </ul>
      <p className="muted small">
        {latest.url} · {latest.device} · {formatDate(latest.collectedAt)}
      </p>
    </>
  )
}

function Vital({ label, value, tone }: { label: string; value: string | number | null; tone: string }) {
  return (
    <li>
      <span>{label}</span>
      <strong className={`tone-${tone}`}>{value ?? '—'}</strong>
    </li>
  )
}

function Row({ label, value, extra }: { label: string; value: string | null; extra?: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>
        {value ?? <span className="muted">yok</span>}
        {extra && <span className="cell-sub">{extra}</span>}
      </dd>
    </div>
  )
}

function lengthNote(length: number | null, min: number, max: number): string | undefined {
  if (length === null) return undefined
  if (length < min) return `${length} karakter — kisa (onerilen ${min}-${max})`
  if (length > max) return `${length} karakter — uzun (onerilen ${min}-${max})`
  return `${length} karakter`
}

function bytes(value: number): string {
  return value < 1024 ? `${value} B` : `${Math.round(value / 1024)} KB`
}

function ms(value: number | null): string {
  return value === null ? '—' : `${value} ms`
}

/** Dusuk deger iyi olan metrikler (LCP/CLS/INP...). */
function threshold(value: number | null, good: number, poor: number): string {
  if (value === null) return 'none'
  if (value <= good) return 'good'
  if (value <= poor) return 'warn'
  return 'bad'
}

/** Yuksek deger iyi olan metrikler (Lighthouse skoru). */
function score(value: number | null, good: number, warn: number): string {
  if (value === null) return 'none'
  if (value >= good) return 'good'
  if (value >= warn) return 'warn'
  return 'bad'
}

/** og_data jsonb metni — okunabilir tek satira indirgenir. */
function parseOg(raw: string | null): string | null {
  if (!raw) return null
  try {
    const parsed = JSON.parse(raw) as Record<string, string>
    const entries = Object.entries(parsed).filter(([, v]) => Boolean(v))
    return entries.length > 0 ? entries.map(([k, v]) => `${k}: ${v}`).join(' · ') : null
  } catch {
    return raw
  }
}
