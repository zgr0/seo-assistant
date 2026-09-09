import { useState } from 'react'
import { createSite, deleteSite, getDashboard, startCrawl } from '../api/client.ts'
import type { DashboardSite } from '../api/types.ts'
import { scoreTone, timeAgo } from '../components/format.ts'
import { Card, Empty, ErrorBox, Field, ScoreGauge, Spinner, TrendArrow } from '../components/ui.tsx'
import { useAction, useAsync } from '../hooks/useAsync.ts'
import { navigate } from '../router.ts'

export function SitesPage() {
  const { data, error, loading, reload } = useAsync(getDashboard, [])
  const [adding, setAdding] = useState(false)

  return (
    <div className="page">
      <header className="page-head">
        <div>
          <h1>Siteler</h1>
          <p className="muted">
            {data
              ? `${data.siteCount} site${
                  data.averageScore !== null ? ` · ortalama skor ${data.averageScore.toFixed(1)}` : ''
                }`
              : 'Kiracı özeti'}
          </p>
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setAdding((v) => !v)}>
          {adding ? 'Vazgeç' : '+ Site ekle'}
        </button>
      </header>

      {adding && <AddSiteForm onDone={() => { setAdding(false); reload() }} />}

      {loading && !data && <Spinner />}
      {error && <ErrorBox message={error} onRetry={reload} />}

      {data && data.sites.length === 0 && (
        <Empty>Henüz site yok. İlk sitenizi ekleyin ve taramayı başlatın.</Empty>
      )}

      <div className="site-grid">
        {data?.sites.map((site) => (
          <SiteCard key={site.siteId} site={site} onChanged={reload} />
        ))}
      </div>
    </div>
  )
}

function AddSiteForm({ onDone }: { onDone: () => void }) {
  const [name, setName] = useState('')
  const [baseUrl, setBaseUrl] = useState('')
  const { busy, error, run } = useAction()

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    void run(async () => {
      await createSite(name, baseUrl)
      onDone()
    })
  }

  return (
    <Card className="add-site">
      <form className="form form-row" onSubmit={submit}>
        <Field label="Site adı">
          <input value={name} onChange={(e) => setName(e.target.value)} required placeholder="Örnek Mağaza" />
        </Field>
        <Field label="Adres" hint="https://ornek.com">
          <input
            value={baseUrl}
            onChange={(e) => setBaseUrl(e.target.value)}
            required
            placeholder="https://ornek.com"
          />
        </Field>
        <button type="submit" className="btn btn-primary" disabled={busy}>
          {busy ? 'Ekleniyor…' : 'Ekle'}
        </button>
      </form>
      {error && <p className="form-error">{error}</p>}
    </Card>
  )
}

function SiteCard({ site, onChanged }: { site: DashboardSite; onChanged: () => void }) {
  const { busy, error, run } = useAction()
  const critical = site.issueCounts.critical ?? 0
  const high = site.issueCounts.high ?? 0

  const crawl = () =>
    void run(async () => {
      const { crawlId } = await startCrawl(site.siteId)
      navigate(`/crawls/${crawlId}`)
    })

  const remove = () =>
    void run(async () => {
      if (!window.confirm(`"${site.name}" ve tüm tarama geçmişi silinecek. Emin misiniz?`)) return
      await deleteSite(site.siteId)
      onChanged()
    })

  return (
    <Card className="site-card">
      <div className="site-card-top">
        <ScoreGauge score={site.lastScore} size={116} label="skor" />
        <div className="site-card-info">
          <h2>{site.name}</h2>
          <a className="site-url" href={site.baseUrl} target="_blank" rel="noreferrer">
            {site.baseUrl}
          </a>
          <div className="site-tags">
            {!site.isActive && <span className="badge">Pasif</span>}
            <TrendArrow delta={site.scoreDelta} />
          </div>
        </div>
      </div>

      <dl className="site-stats">
        <div>
          <dt>Kritik bulgu</dt>
          <dd className={critical > 0 ? 'tone-bad' : 'tone-good'}>{critical}</dd>
        </div>
        <div>
          <dt>Yüksek</dt>
          <dd className={high > 0 ? 'tone-warn' : 'tone-good'}>{high}</dd>
        </div>
        <div>
          <dt>Son tarama</dt>
          <dd className={`tone-${scoreTone(site.lastScore)}`}>{timeAgo(site.lastCrawlAt)}</dd>
        </div>
      </dl>

      {error && <p className="form-error">{error}</p>}

      <div className="site-actions">
        {site.lastCrawlId && (
          <a className="btn btn-ghost btn-sm" href={`#/crawls/${site.lastCrawlId}`}>
            Son tarama
          </a>
        )}
        <button type="button" className="btn btn-primary btn-sm" onClick={crawl} disabled={busy}>
          {busy ? '…' : 'Tara'}
        </button>
        <button type="button" className="btn btn-ghost btn-sm danger" onClick={remove} disabled={busy}>
          Sil
        </button>
      </div>
    </Card>
  )
}
