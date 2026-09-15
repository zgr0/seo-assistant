import { useEffect, useMemo, useRef, useState } from 'react'
import {
  createSocialKit,
  deleteContentJob,
  favoriteVariant,
  getAssetBlob,
  getContentJob,
  listBrandProfiles,
  listContentAssets,
  listContentJobs,
  listPlatformProfiles,
  listSites,
} from '../api/client.ts'
import type {
  ContentAsset,
  ContentJob,
  ContentVariant,
  ImageTemplate,
  PlatformProfile,
} from '../api/types.ts'
import { timeAgo } from '../components/format.ts'
import { Card, Empty, ErrorBox, Field, Spinner } from '../components/ui.tsx'
import { useAction, useAsync } from '../hooks/useAsync.ts'

const MaxPostCount = 5

export function SocialPage({ siteId }: { siteId?: string }) {
  const sites = useAsync(listSites, [])
  const platforms = useAsync(listPlatformProfiles, [])

  const [chosenSite, setChosenSite] = useState(siteId ?? '')
  const [jobIds, setJobIds] = useState<string[]>([])
  const [pageUrls, setPageUrls] = useState<string[]>([])
  // Silinen isler: gecmis yeniden yuklenene kadar listeden hemen dussun.
  const [deleted, setDeleted] = useState<string[]>([])
  const polled = usePolledJobs(jobIds)

  // Kullanici secmediyse ilk site — turetilir, state'e yazilmaz.
  const selectedSite = chosenSite || sites.data?.[0]?.id || ''

  // Yeni paketin tum isleri bitti mi — gecmis ve galeri o an tazelenir.
  const doneCount = polled.filter((j) => j.status === 'Done' || j.status === 'Failed').length
  const generating = jobIds.length > 0 && doneCount < jobIds.length

  // Sayfa yenilendiginde onceki uretimler kaybolmasin — sitenin gecmis paketleri.
  const history = useAsync(
    () =>
      selectedSite
        ? listContentJobs({ type: 'social_kit', siteId: selectedSite, size: 12 })
        : Promise.resolve(null),
    [selectedSite, generating],
  )

  // Yeni isler ustte, gecmis altta; ayni is iki kez gosterilmez.
  const shown = useMemo(() => {
    const fresh = new Set(polled.map((j) => j.id))
    const gone = new Set(deleted)
    return [...polled, ...(history.data?.items ?? []).filter((j) => !fresh.has(j.id))].filter(
      (j) => !gone.has(j.id),
    )
  }, [polled, history.data, deleted])

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
          progress={jobIds.length > 0 ? { done: doneCount, expected: jobIds.length } : null}
          pageUrls={pageUrls}
          platforms={platforms.data ?? []}
          onDeleted={(jobId) => setDeleted((prev) => [...prev, jobId])}
        />
      )}

      {!history.loading && shown.length === 0 && selectedSite && (
        <Empty>Bu site için henüz gönderi üretilmedi.</Empty>
      )}

      {selectedSite && (
        // Anahtar: site degisince, uretim bitince ya da gonderi silinince galeri sifirdan yuklenir.
        <Gallery
          key={`${selectedSite}:${generating ? 'uretiliyor' : jobIds.join(',')}:${deleted.length}`}
          siteId={selectedSite}
          platforms={platforms.data ?? []}
        />
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
  const [postCount, setPostCount] = useState(1)
  const [brandProfileId, setBrandProfileId] = useState('')
  // Bos: tum sablonlar donusumlu.
  const [templates, setTemplates] = useState<ImageTemplate[]>([])
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
        imageTemplates: templates.length > 0 ? templates : undefined,
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

        <TemplatePicker value={templates} onChange={setTemplates} />

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

const TemplateOptions: {
  value: ImageTemplate
  label: string
  description: string
  photo: boolean
}[] = [
  { value: 'Overlay', label: 'Perde', description: 'Tam ekran fotoğraf, altta başlık', photo: true },
  { value: 'Split', label: 'Bölünmüş', description: 'Fotoğraf ve renkli metin paneli', photo: true },
  { value: 'Framed', label: 'Çerçeve', description: 'Koyu zeminde çerçeveli fotoğraf', photo: true },
  { value: 'Label', label: 'Etiket', description: 'Fotoğraf üstünde beyaz başlık kartı', photo: true },
  { value: 'Poster', label: 'Afiş', description: 'Fotoğrafsız, desenli zeminde büyük başlık', photo: false },
  { value: 'Quote', label: 'Alıntı', description: 'Fotoğrafsız, sayfadan bir cümle alıntı olarak', photo: false },
]

/**
 * Gorsel sablonu secimi. Hic secilmezse "Otomatik": sablonlar gonderiler arasinda donusumlu.
 * Birden fazla secilirse yalniz secilenler donusumlu kullanilir.
 */
function TemplatePicker({
  value,
  onChange,
}: {
  value: ImageTemplate[]
  onChange: (value: ImageTemplate[]) => void
}) {
  const toggle = (template: ImageTemplate) =>
    onChange(
      value.includes(template) ? value.filter((t) => t !== template) : [...value, template],
    )

  const onlyCards = value.length > 0 && value.every((t) => !TemplateOptions.find((o) => o.value === t)?.photo)

  return (
    <div className="field">
      <span className="field-label">Görsel şablonu</span>
      <div className="template-grid">
        <button
          type="button"
          className={`template-option ${value.length === 0 ? 'template-on' : ''}`.trim()}
          onClick={() => onChange([])}
          aria-pressed={value.length === 0}
          title="Şablonlar gönderiler arasında dönüşümlü kullanılır"
        >
          <TemplateIcon template="auto" />
          <span className="template-label">Otomatik</span>
        </button>

        {TemplateOptions.map((option) => (
          <button
            key={option.value}
            type="button"
            className={`template-option ${value.includes(option.value) ? 'template-on' : ''}`.trim()}
            onClick={() => toggle(option.value)}
            aria-pressed={value.includes(option.value)}
            title={option.description}
          >
            <TemplateIcon template={option.value} />
            <span className="template-label">{option.label}</span>
          </button>
        ))}
      </div>
      <span className="field-hint">
        {value.length === 0
          ? 'Tüm şablonlar gönderiler arasında dönüşümlü kullanılır.'
          : onlyCards
            ? 'Yalnız fotoğrafsız şablon seçildi — site fotoğrafı kullanılmaz, marka kartı çizilir.'
            : 'Birden fazla seçilirse gönderiler arasında dönüşümlü kullanılır. Alıntı, sayfada açıklama yoksa afişe döner.'}
      </span>
    </div>
  )
}

/** Sablonun kucuk yerlesim cizimi — renkler temadan (currentColor) gelir. */
function TemplateIcon({ template }: { template: ImageTemplate | 'auto' }) {
  const photo = { fill: 'currentColor', opacity: 0.28 }
  const panel = { fill: 'currentColor', opacity: 0.85 }
  const line = { fill: 'var(--surface)' }

  return (
    <svg className="template-icon" viewBox="0 0 48 48" aria-hidden="true">
      {template === 'auto' && (
        <>
          <rect x="4" y="4" width="18" height="18" rx="3" style={photo} />
          <rect x="26" y="4" width="18" height="18" rx="3" style={panel} />
          <rect x="4" y="26" width="18" height="18" rx="3" style={panel} />
          <rect x="26" y="26" width="18" height="18" rx="3" style={photo} />
        </>
      )}
      {template === 'Overlay' && (
        <>
          <rect x="4" y="4" width="40" height="40" rx="4" style={photo} />
          <rect x="4" y="28" width="40" height="16" rx="4" style={panel} />
          <rect x="9" y="33" width="24" height="3" rx="1.5" style={line} />
          <rect x="9" y="38" width="14" height="2" rx="1" style={line} />
        </>
      )}
      {template === 'Split' && (
        <>
          <rect x="4" y="4" width="40" height="22" rx="4" style={photo} />
          <rect x="4" y="26" width="40" height="18" rx="4" style={panel} />
          <rect x="9" y="31" width="26" height="3" rx="1.5" style={line} />
          <rect x="9" y="37" width="18" height="2" rx="1" style={line} />
        </>
      )}
      {template === 'Framed' && (
        <>
          <rect x="4" y="4" width="40" height="40" rx="4" style={panel} />
          <rect x="9" y="9" width="30" height="18" rx="3" style={{ fill: 'var(--surface)', opacity: 0.55 }} />
          <rect x="9" y="32" width="24" height="3" rx="1.5" style={line} />
          <rect x="9" y="38" width="14" height="2" rx="1" style={line} />
        </>
      )}
      {template === 'Label' && (
        <>
          <rect x="4" y="4" width="40" height="40" rx="4" style={photo} />
          <rect x="8" y="28" width="32" height="12" rx="2.5" style={{ fill: 'var(--surface)' }} />
          <rect x="12" y="31.5" width="20" height="2.5" rx="1.25" style={panel} />
          <rect x="12" y="36" width="12" height="1.8" rx="0.9" style={panel} />
        </>
      )}
      {template === 'Poster' && (
        <>
          <rect x="4" y="4" width="40" height="40" rx="4" style={panel} />
          <circle cx="44" cy="44" r="14" style={{ fill: 'none', stroke: 'var(--surface)', strokeWidth: 1.2, opacity: 0.35 }} />
          <rect x="9" y="10" width="8" height="2" rx="1" style={line} />
          <rect x="9" y="17" width="28" height="5" rx="2" style={line} />
          <rect x="9" y="25" width="20" height="5" rx="2" style={line} />
          <rect x="9" y="37" width="12" height="2" rx="1" style={{ ...line, opacity: 0.7 }} />
        </>
      )}
      {template === 'Quote' && (
        <>
          <rect x="4" y="4" width="40" height="40" rx="4" style={panel} />
          <text x="8" y="22" style={{ fill: 'var(--surface)', fontSize: 18, fontWeight: 700 }}>
            “
          </text>
          <rect x="9" y="24" width="30" height="3" rx="1.5" style={line} />
          <rect x="9" y="30" width="24" height="3" rx="1.5" style={line} />
          <rect x="9" y="37" width="14" height="2" rx="1" style={{ ...line, opacity: 0.7 }} />
        </>
      )}
    </svg>
  )
}

function Results({
  jobs,
  progress,
  pageUrls,
  platforms,
  onDeleted,
}: {
  jobs: ContentJob[]
  /** Bu oturumda baslatilan paketin ilerlemesi; yalniz gecmis gosteriliyorsa null. */
  progress: { done: number; expected: number } | null
  pageUrls: string[]
  platforms: PlatformProfile[]
  onDeleted: (jobId: string) => void
}) {
  const running = progress !== null && progress.done < progress.expected

  return (
    <section className="kit-results">
      <div className="kit-progress">
        {running ? (
          <Spinner label={`Üretiliyor… ${progress.done}/${progress.expected}`} />
        ) : (
          <p className="muted">
            {progress
              ? `${progress.expected} iş tamamlandı · kaynak sayfalar: ${pageUrls.length} · toplam ${jobs.length} gönderi`
              : `Önceki üretimler · ${jobs.length} gönderi`}
          </p>
        )}
      </div>

      <div className="post-grid">
        {jobs.flatMap((job) =>
          job.status === 'Failed' ? (
            <Card key={job.id} className="post-card">
              <ErrorBox message={job.errorMessage ?? 'Üretim başarısız'} />
              <p className="muted post-source">{job.pageUrl}</p>
              <DeletePostButton jobId={job.id} onDeleted={onDeleted} />
            </Card>
          ) : (
            job.variants.map((variant) => (
              <PostCard
                key={variant.id}
                job={job}
                variant={variant}
                platform={platforms.find((p) => p.code === job.platformCode)}
                onDeleted={onDeleted}
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
  onDeleted,
}: {
  job: ContentJob
  variant: ContentVariant
  platform?: PlatformProfile
  onDeleted: (jobId: string) => void
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
        <AssetImage
          assetId={variant.imageAssetId}
          rawAssetId={variant.rawImageAssetId}
          alt={variant.imageAlt ?? ''}
        />
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
        <div className="post-foot-actions">
          <button type="button" className="btn btn-ghost btn-sm" onClick={copy}>
            {copied ? 'Kopyalandı' : 'Kopyala'}
          </button>
          <DeletePostButton jobId={job.id} onDeleted={onDeleted} />
        </div>
      </div>

      {job.pageUrl && (
        <a className="post-source muted" href={job.pageUrl} target="_blank" rel="noreferrer">
          {job.pageUrl}
        </a>
      )}
    </Card>
  )
}

/** Gonderiyi ve gorsellerini siler; onay ister, hata olursa butonun yaninda gosterir. */
function DeletePostButton({
  jobId,
  onDeleted,
}: {
  jobId: string
  onDeleted: (jobId: string) => void
}) {
  const { busy, error, run } = useAction()

  const remove = () =>
    void run(async () => {
      if (!window.confirm('Bu gönderi ve görselleri silinecek. Emin misiniz?')) return
      await deleteContentJob(jobId)
      onDeleted(jobId)
    })

  return (
    <>
      <button
        type="button"
        className="btn btn-ghost btn-sm tone-bad"
        onClick={remove}
        disabled={busy}
        title="Gönderiyi sil"
      >
        {busy ? 'Siliniyor…' : 'Sil'}
      </button>
      {error && <span className="form-error">{error}</span>}
    </>
  )
}

/** Gorsel yetkili uctan gelir; blob'u object URL'e cevirip sokulunce serbest birakir. */
function AssetImage({
  assetId,
  rawAssetId,
  alt,
}: {
  assetId: string
  /** Yazisiz surum; yalniz indirme baglantisi icin yuklenir. */
  rawAssetId?: string | null
  alt: string
}) {
  const { url, failed } = useAssetUrl(assetId, true)

  if (failed) return null
  if (!url) return <div className="post-image post-image-loading" />

  return (
    <figure className="post-image">
      <img src={url} alt={alt} />
      <div className="post-image-actions">
        <a className="btn btn-ghost btn-sm" href={url} download={`gonderi-${assetId}.jpg`}>
          Görseli indir
        </a>
        {rawAssetId && <RawImageLink assetId={rawAssetId} />}
      </div>
    </figure>
  )
}

/** Yazisiz surum: ayni FLUX uretiminden gelir, ek ucret dogurmaz. */
function RawImageLink({ assetId }: { assetId: string }) {
  const [url, setUrl] = useState<string | null>(null)
  const { busy, run } = useAction()

  if (url) {
    return (
      <a className="btn btn-ghost btn-sm" href={url} download={`gonderi-${assetId}-yazisiz.jpg`}>
        Yazısız indir ↓
      </a>
    )
  }

  // Yazisiz surum cogu zaman istenmez — blob yalniz tiklaninca cekilir.
  return (
    <button
      type="button"
      className="btn btn-ghost btn-sm"
      disabled={busy}
      onClick={() =>
        void run(async () => {
          setUrl(URL.createObjectURL(await getAssetBlob(assetId)))
        })
      }
    >
      {busy ? '…' : 'Yazısız sürüm'}
    </button>
  )
}

/**
 * Yetkili uctan gorsel baytlarini cekip object URL'e cevirir; sokulunce serbest birakir.
 * <img src> dogrudan kullanilamaz — istek Bearer basligi ister.
 */
function useAssetUrl(assetId: string, enabled: boolean) {
  const [state, setState] = useState<{ id: string; url: string | null; failed: boolean }>({
    id: assetId,
    url: null,
    failed: false,
  })

  useEffect(() => {
    if (!enabled) return

    let active = true
    let objectUrl: string | null = null

    getAssetBlob(assetId)
      .then((blob) => {
        if (!active) return
        objectUrl = URL.createObjectURL(blob)
        setState({ id: assetId, url: objectUrl, failed: false })
      })
      .catch(() => {
        if (active) setState({ id: assetId, url: null, failed: true })
      })

    return () => {
      active = false
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [assetId, enabled])

  // Kimlik degistiyse onceki gorselin URL'i gosterilmesin.
  return state.id === assetId ? state : { url: null, failed: false }
}

const SourceLabel: Record<ContentAsset['source'], string> = {
  site: 'site fotoğrafı',
  card: 'marka kartı',
  ai: 'yapay zekâ',
}

/** Galeri sayfa boyutu — her kucuk resim ayri bir istek oldugu icin olculu tutulur. */
const GalleryPageSize = 12

/**
 * Sitede daha once uretilen gorseller. Her gorsel bir kez listelenir: yazili surum varsa o,
 * yoksa ham gorsel. Kucuk resimler ekrana girince yuklenir.
 */
function Gallery({ siteId, platforms }: { siteId: string; platforms: PlatformProfile[] }) {
  const first = useAsync(() => listContentAssets({ siteId, size: GalleryPageSize }), [siteId])
  const [more, setMore] = useState<ContentAsset[]>([])
  const [page, setPage] = useState(1)
  const { busy, error, run } = useAction()

  const items = [...(first.data?.items ?? []), ...more]
  const total = first.data?.total ?? 0

  const loadMore = () =>
    void run(async () => {
      const next = await listContentAssets({ siteId, page: page + 1, size: GalleryPageSize })
      setMore((prev) => [...prev, ...next.items])
      setPage((p) => p + 1)
    })

  if (first.loading && !first.data) return <Spinner label="Görseller yükleniyor" />
  if (first.error) return <ErrorBox message={first.error} onRetry={first.reload} />
  if (total === 0) return null

  return (
    <section className="gallery">
      <h2 className="gallery-title">
        Önceki görseller <span className="muted">· {total}</span>
      </h2>

      <div className="gallery-grid">
        {items.map((asset) => (
          <GalleryItem
            key={asset.id}
            asset={asset}
            platform={platforms.find((p) => p.code === asset.platformCode)}
          />
        ))}
      </div>

      {error && <p className="form-error">{error}</p>}

      {items.length < total && (
        <button type="button" className="btn btn-ghost" onClick={loadMore} disabled={busy}>
          {busy ? 'Yükleniyor…' : `Daha fazla göster (${total - items.length})`}
        </button>
      )}
    </section>
  )
}

function GalleryItem({ asset, platform }: { asset: ContentAsset; platform?: PlatformProfile }) {
  const ref = useRef<HTMLElement>(null)
  const visible = useInView(ref)
  const { url, failed } = useAssetUrl(asset.id, visible)

  const path = asset.pageUrl ? pathOf(asset.pageUrl) : null

  return (
    <figure ref={ref} className="gallery-item">
      {url ? (
        <a href={url} target="_blank" rel="noreferrer" title="Tam boyutta aç">
          {/* loading="lazy" yok: baytlar zaten gorunur olunca cekiliyor, ikinci erteleme gereksiz. */}
          <img className="gallery-thumb" src={url} alt={asset.alt ?? ''} />
        </a>
      ) : (
        <div className={`gallery-thumb ${failed ? 'gallery-thumb-failed' : 'post-image-loading'}`}>
          {failed && <span className="muted">Görsel açılamadı</span>}
        </div>
      )}

      <figcaption className="gallery-meta">
        <div className="gallery-tags">
          {platform && <span className="badge">{platform.displayName}</span>}
          <span className="badge" title="Görselin kaynağı">
            {SourceLabel[asset.source]}
          </span>
          {asset.kind === 'Raw' && <span className="badge">yazısız</span>}
          <span className="muted">{timeAgo(asset.createdAt)}</span>
        </div>

        {asset.pageUrl && path && (
          <a className="post-source muted" href={asset.pageUrl} target="_blank" rel="noreferrer">
            {path}
          </a>
        )}

        {url && (
          <div className="post-image-actions">
            <a className="btn btn-ghost btn-sm" href={url} download={`gorsel-${asset.id}.jpg`}>
              İndir
            </a>
            {asset.rawAssetId && <RawImageLink assetId={asset.rawAssetId} />}
          </div>
        )}
      </figcaption>
    </figure>
  )
}

/** Oge bir kez gorunur olunca true doner ve oyle kalir — gorsel yeniden cekilmez. */
function useInView(ref: React.RefObject<HTMLElement | null>) {
  const [inView, setInView] = useState(false)
  // Ortam desteklemiyorsa (eski tarayici, test) gozlem yapilmaz, hemen yuklenir.
  const supported = typeof IntersectionObserver !== 'undefined'

  useEffect(() => {
    const element = ref.current
    if (!element || inView || !supported) return

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          setInView(true)
          observer.disconnect()
        }
      },
      { rootMargin: '200px' },
    )

    observer.observe(element)
    return () => observer.disconnect()
  }, [ref, inView, supported])

  return inView || !supported
}

function pathOf(url: string): string {
  try {
    const { pathname } = new URL(url)
    return pathname === '/' ? 'Ana sayfa' : decodeURIComponent(pathname)
  } catch {
    return url
  }
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
