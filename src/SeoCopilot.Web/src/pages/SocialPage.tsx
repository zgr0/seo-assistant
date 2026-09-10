import { useEffect, useMemo, useState } from 'react'
import {
  createSocialKit,
  favoriteVariant,
  getAssetBlob,
  getContentJob,
  listBrandProfiles,
  listContentJobs,
  listPlatformProfiles,
  listSites,
} from '../api/client.ts'
import type { ContentJob, ContentVariant, PlatformProfile } from '../api/types.ts'
import { Card, Empty, ErrorBox, Field, Spinner } from '../components/ui.tsx'
import { useAction, useAsync } from '../hooks/useAsync.ts'

const MaxPostCount = 5

export function SocialPage({ siteId }: { siteId?: string }) {
  const sites = useAsync(listSites, [])
  const platforms = useAsync(listPlatformProfiles, [])

  const [chosenSite, setChosenSite] = useState(siteId ?? '')
  const [jobIds, setJobIds] = useState<string[]>([])
  const [pageUrls, setPageUrls] = useState<string[]>([])
  const polled = usePolledJobs(jobIds)

  // Kullanici secmediyse ilk site — turetilir, state'e yazilmaz.
  const selectedSite = chosenSite || sites.data?.[0]?.id || ''

  // Sayfa yenilendiginde onceki uretimler kaybolmasin — sitenin gecmis paketleri.
  const history = useAsync(
    () =>
      selectedSite
        ? listContentJobs({ type: 'social_kit', siteId: selectedSite, size: 12 })
        : Promise.resolve(null),
    [selectedSite],
  )

  const shown = polled.length > 0 ? polled : (history.data?.items ?? [])

  return (
    <div className="page">
      <header className="page-head">
        <div>
          <h1>Sosyal medya gönderileri</h1>
          <p className="muted">
            Taranmış bir siteden örnek gönderiler üretin: metin, açıklama, hashtag ve görsel.
          </p>
        </div>
      </header>

      {(sites.loading || platforms.loading) && !sites.data && <Spinner />}
      {sites.error && <ErrorBox message={sites.error} onRetry={sites.reload} />}
      {platforms.error && <ErrorBox message={platforms.error} onRetry={platforms.reload} />}

      {sites.data && sites.data.length === 0 && (
        <Empty>
          Önce bir site ekleyip taramayı çalıştırın. <a href="#/sites">Sitelere git</a>
        </Empty>
      )}

      {sites.data && sites.data.length > 0 && platforms.data && (
        <KitForm
          sites={sites.data.map((s) => ({ id: s.id, name: s.name }))}
          platforms={platforms.data}
          siteId={selectedSite}
          onSiteChange={(id) => {
            setChosenSite(id)
            // Baska siteye geciste onceki paketin sonuclari kalmasin.
            setJobIds([])
            setPageUrls([])
          }}
          onCreated={(result) => {
            setJobIds(result.jobIds)
            setPageUrls(result.pageUrls)
          }}
        />
      )}

      {history.loading && shown.length === 0 && <Spinner label="Önceki gönderiler yükleniyor" />}

      {shown.length > 0 && (
        <Results
          jobs={shown}
          expected={jobIds.length > 0 ? jobIds.length : shown.length}
          pageUrls={pageUrls}
          platforms={platforms.data ?? []}
          historic={jobIds.length === 0}
        />
      )}

      {!history.loading && shown.length === 0 && selectedSite && (
        <Empty>Bu site için henüz gönderi üretilmedi.</Empty>
      )}
    </div>
  )
}

function KitForm({
  sites,
  platforms,
  siteId,
  onSiteChange,
  onCreated,
}: {
  sites: { id: string; name: string }[]
  platforms: PlatformProfile[]
  siteId: string
  onSiteChange: (siteId: string) => void
  onCreated: (result: { jobIds: string[]; pageUrls: string[] }) => void
}) {
  const selectedSite = siteId || sites[0]?.id || ''
  const [selected, setSelected] = useState<string[]>(() =>
    platforms.length > 0 ? [platforms[0].code] : [],
  )
  const [postCount, setPostCount] = useState(3)
  const [brandProfileId, setBrandProfileId] = useState('')
  const { busy, error, run } = useAction()

  // Marka profilleri siteye bagli olabilir — site degisince listeyi tazele.
  const brands = useAsync(() => listBrandProfiles(selectedSite), [selectedSite])

  const changeSite = (id: string) => {
    // Secili profil baska siteye ait olabilir.
    setBrandProfileId('')
    onSiteChange(id)
  }

  const toggle = (code: string) =>
    setSelected((prev) => (prev.includes(code) ? prev.filter((c) => c !== code) : [...prev, code]))

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    void run(async () => {
      const result = await createSocialKit({
        siteId: selectedSite,
        platformCodes: selected,
        postCount,
        brandProfileId: brandProfileId || undefined,
      })
      onCreated(result)
    })
  }

  return (
    <Card className="kit-form">
      <form className="form" onSubmit={submit}>
        <div className="form-row">
          <Field label="Site">
            <select value={selectedSite} onChange={(e) => changeSite(e.target.value)} required>
              {sites.map((site) => (
                <option key={site.id} value={site.id}>
                  {site.name}
                </option>
              ))}
            </select>
          </Field>

          <Field label="Gönderi sayısı" hint="Her platform için ayrı sayfadan üretilir">
            <select value={postCount} onChange={(e) => setPostCount(Number(e.target.value))}>
              {Array.from({ length: MaxPostCount }, (_, i) => i + 1).map((n) => (
                <option key={n} value={n}>
                  {n}
                </option>
              ))}
            </select>
          </Field>

          <Field label="Marka profili" hint="Boş bırakılırsa varsayılan ton kullanılır">
            <select value={brandProfileId} onChange={(e) => setBrandProfileId(e.target.value)}>
              <option value="">— yok —</option>
              {brands.data?.map((brand) => (
                <option key={brand.id} value={brand.id}>
                  {brand.name}
                  {brand.isDefault ? ' (varsayılan)' : ''}
                </option>
              ))}
            </select>
          </Field>
        </div>

        <div className="field">
          <span className="field-label">Platformlar</span>
          <div className="chip-row">
            {platforms.map((platform) => (
              <button
                key={platform.code}
                type="button"
                className={`chip ${selected.includes(platform.code) ? 'chip-on' : ''}`.trim()}
                onClick={() => toggle(platform.code)}
                title={platform.guidanceTr}
              >
                {platform.displayName}
              </button>
            ))}
          </div>
        </div>

        <button
          type="submit"
          className="btn btn-primary"
          disabled={busy || selected.length === 0 || !selectedSite}
        >
          {busy ? 'Üretim başlatılıyor…' : 'Gönderileri üret'}
        </button>
      </form>

      {error && <p className="form-error">{error}</p>}
    </Card>
  )
}

function Results({
  jobs,
  expected,
  pageUrls,
  platforms,
  historic,
}: {
  jobs: ContentJob[]
  expected: number
  pageUrls: string[]
  platforms: PlatformProfile[]
  /** Gecmisten yuklendi — ilerleme gostergesi gosterilmez. */
  historic?: boolean
}) {
  const done = jobs.filter((j) => j.status === 'Done' || j.status === 'Failed').length
  const running = !historic && done < expected

  return (
    <section className="kit-results">
      <div className="kit-progress">
        {running ? (
          <Spinner label={`Üretiliyor… ${done}/${expected}`} />
        ) : (
          <p className="muted">
            {historic
              ? `Önceki üretimler · ${jobs.length} gönderi`
              : `${expected} iş tamamlandı · kaynak sayfalar: ${pageUrls.length}`}
          </p>
        )}
      </div>

      <div className="post-grid">
        {jobs.flatMap((job) =>
          job.status === 'Failed' ? (
            <Card key={job.id} className="post-card">
              <ErrorBox message={job.errorMessage ?? 'Üretim başarısız'} />
              <p className="muted post-source">{job.pageUrl}</p>
            </Card>
          ) : (
            job.variants.map((variant) => (
              <PostCard
                key={variant.id}
                job={job}
                variant={variant}
                platform={platforms.find((p) => p.code === job.platformCode)}
              />
            ))
          ),
        )}
      </div>
    </section>
  )
}

function PostCard({
  job,
  variant,
  platform,
}: {
  job: ContentJob
  variant: ContentVariant
  platform?: PlatformProfile
}) {
  const [favorite, setFavorite] = useState(variant.isFavorite)
  const [copied, setCopied] = useState(false)

  const text = useMemo(() => {
    const parts = [variant.body]
    if (variant.cta) parts.push(variant.cta)
    if (variant.hashtags.length > 0) parts.push(variant.hashtags.join(' '))
    return parts.join('\n\n')
  }, [variant])

  const overLimit = platform ? text.length > platform.maxChars : false

  const copy = () => {
    void navigator.clipboard.writeText(text).then(() => {
      setCopied(true)
      window.setTimeout(() => setCopied(false), 1500)
    })
  }

  const toggleFavorite = () => {
    const next = !favorite
    setFavorite(next)
    // Iyimser guncelleme; hata olursa eski duruma don.
    void favoriteVariant(variant.id, next).catch(() => setFavorite(!next))
  }

  return (
    <Card className="post-card">
      <div className="post-head">
        <span className="badge">{platform?.displayName ?? job.platformCode}</span>
        {variant.angle && <span className="badge">{variant.angle.replace(/_/g, ' ')}</span>}
        <button
          type="button"
          className={`btn btn-ghost btn-sm ${favorite ? 'fav-on' : ''}`.trim()}
          onClick={toggleFavorite}
          title={favorite ? 'Favoriden çıkar' : 'Favoriye ekle'}
        >
          {favorite ? '★' : '☆'}
        </button>
      </div>

      {variant.imageAssetId && (
        <AssetImage assetId={variant.imageAssetId} alt={variant.imageAlt ?? ''} />
      )}

      <p className="post-body">{variant.body}</p>

      {variant.description && <p className="muted post-description">{variant.description}</p>}

      {variant.cta && <p className="post-cta">{variant.cta}</p>}

      {variant.hashtags.length > 0 && (
        <div className="chip-row">
          {variant.hashtags.map((tag) => (
            <span key={tag} className="chip">
              {tag}
            </span>
          ))}
        </div>
      )}

      <div className="post-foot">
        <span className={overLimit ? 'tone-bad' : 'muted'}>
          {text.length}
          {platform ? ` / ${platform.maxChars}` : ''} karakter
        </span>
        <button type="button" className="btn btn-ghost btn-sm" onClick={copy}>
          {copied ? 'Kopyalandı' : 'Kopyala'}
        </button>
      </div>

      {job.pageUrl && (
        <a className="post-source muted" href={job.pageUrl} target="_blank" rel="noreferrer">
          {job.pageUrl}
        </a>
      )}
    </Card>
  )
}

/** Gorsel yetkili uctan gelir; blob'u object URL'e cevirip sokulunce serbest birakir. */
function AssetImage({ assetId, alt }: { assetId: string; alt: string }) {
  const [url, setUrl] = useState<string | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    let active = true
    let objectUrl: string | null = null

    getAssetBlob(assetId)
      .then((blob) => {
        if (!active) return
        objectUrl = URL.createObjectURL(blob)
        setUrl(objectUrl)
      })
      .catch(() => {
        if (active) setFailed(true)
      })

    return () => {
      active = false
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [assetId])

  if (failed) return null
  if (!url) return <div className="post-image post-image-loading" />

  return (
    <figure className="post-image">
      <img src={url} alt={alt} />
      <a className="btn btn-ghost btn-sm" href={url} download={`gonderi-${assetId}.jpg`}>
        Görseli indir
      </a>
    </figure>
  )
}

/**
 * Isler bitene kadar yoklar. Uretim asenkron: is 'queued' yazilir, worker LLM ve
 * gorsel adimlarini calistirir.
 */
function usePolledJobs(jobIds: string[]): ContentJob[] {
  const [jobs, setJobs] = useState<ContentJob[]>([])
  const key = jobIds.join(',')

  useEffect(() => {
    if (jobIds.length === 0) {
      setJobs([])
      return
    }

    let active = true
    let timer = 0

    const tick = async () => {
      try {
        const next = await Promise.all(jobIds.map(getContentJob))
        if (!active) return
        setJobs(next)
        if (next.some((job) => job.status === 'Queued' || job.status === 'Running')) {
          timer = window.setTimeout(() => void tick(), 2500)
        }
      } catch {
        // Gecici hata — daha seyrek yeniden dene.
        if (active) timer = window.setTimeout(() => void tick(), 5000)
      }
    }

    void tick()

    return () => {
      active = false
      window.clearTimeout(timer)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key])

  return jobs
}
