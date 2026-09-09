import { useEffect, useRef, useState } from 'react'
import { generateContent, getContentJob, ignoreIssue, listIssues, reopenIssue } from '../api/client.ts'
import type { ContentJob, Issue } from '../api/types.ts'
import { categoryLabel, hostOf, shortUrl, weightNote } from '../components/format.ts'
import { Card, Empty, ErrorBox, Pager, SeverityBadge, Spinner } from '../components/ui.tsx'
import { useAction, useAsync } from '../hooks/useAsync.ts'

const pageSize = 50

/**
 * Bulgu detayi kural bazindadir: ayni kural bir taramada onlarca sayfada tetiklenebilir,
 * bunlarin hepsi "etkilenen sayfalar" olarak sayfalanmis halde listelenir.
 */
export function IssuePage({ crawlId, ruleCode }: { crawlId: string; ruleCode: string }) {
  const [pageNumber, setPageNumber] = useState(1)

  // Acik adedi ayri bir sayimla gelir: yuklenen dilime bakip saymak, liste sayfalandigi
  // icin yalnizca o sayfayi sayardi.
  const { data, error, loading, reload } = useAsync(async () => {
    const [affected, open] = await Promise.all([
      listIssues(crawlId, { ruleCode, page: pageNumber, size: pageSize }),
      listIssues(crawlId, { ruleCode, status: 'Open', size: 1 }),
    ])
    return { affected, openTotal: open.total }
  }, [crawlId, ruleCode, pageNumber])

  if (loading && !data) return <Spinner />
  if (error) return <ErrorBox message={error} onRetry={reload} />
  if (!data) return null
  if (data.affected.items.length === 0) return <Empty>Bu kurala ait bulgu yok.</Empty>

  const first = data.affected.items[0]

  return (
    <div className="page">
      <nav className="crumbs">
        <a href="#/sites">Siteler</a> <span>/</span>{' '}
        <a href={`#/crawls/${crawlId}`}>Tarama</a> <span>/</span> <strong>{ruleCode}</strong>
      </nav>

      <header className="page-head">
        <div>
          <h1>
            {first.ruleTitle ?? ruleCode} <SeverityBadge severity={first.severity} />
          </h1>
          <p className="muted">
            <code>{ruleCode}</code>
            {first.category && ` · ${categoryLabel(first.category)}`} ·{' '}
            {data.affected.total} bulgu ({data.openTotal} açık)
          </p>
          <p className="muted small">
            Ağırlık {first.weight}/10 — {weightNote(first.weight)}
          </p>
        </div>
      </header>

      <Card>
        <h2 className="card-title">Kural ne diyor?</h2>
        <p className="prose">{first.ruleDescription ?? 'Bu kural için açıklama tanımlanmamış.'}</p>
        <DocLink url={first.docUrl} />
      </Card>

      <FixAdvice issue={first} />

      {first.whenToIgnore && (
        <Card>
          <h2 className="card-title">Yoksaymalı mıyım?</h2>
          <p className="prose">{first.whenToIgnore}</p>
        </Card>
      )}

      <Card>
        <h2 className="card-title">Etkilenen sayfalar</h2>
        <AffectedTable issues={data.affected.items} onChanged={reload} />
        <Pager
          page={data.affected.page}
          size={data.affected.size}
          total={data.affected.total}
          onChange={setPageNumber}
        />
      </Card>
    </div>
  )
}

/** Kural katalogundaki birincil kaynak. Seed'te tanimsizsa hicbir sey basilmaz. */
function DocLink({ url }: { url: string | null }) {
  if (!url) return null

  return (
    <p className="doc-link muted">
      Kaynak:{' '}
      <a href={url} target="_blank" rel="noreferrer">
        {hostOf(url)} ↗
      </a>
    </p>
  )
}

function AffectedTable({ issues, onChanged }: { issues: Issue[]; onChanged: () => void }) {
  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>Sayfa</th>
            <th>Kanıt</th>
            <th>Durum</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {issues.map((issue) => (
            <IssueRow key={issue.id} issue={issue} onChanged={onChanged} />
          ))}
        </tbody>
      </table>
    </div>
  )
}

function IssueRow({ issue, onChanged }: { issue: Issue; onChanged: () => void }) {
  const [ignoring, setIgnoring] = useState(false)
  const [reason, setReason] = useState('')
  const [applyToSite, setApplyToSite] = useState(false)
  const { busy, error, run } = useAction()

  const confirmIgnore = () =>
    void run(async () => {
      await ignoreIssue(issue.id, reason, applyToSite)
      setIgnoring(false)
      setReason('')
      onChanged()
    })

  const reopen = () =>
    void run(async () => {
      await reopenIssue(issue.id)
      onChanged()
    })

  const evidence = [issue.found, issue.expected && `Beklenen: ${issue.expected}`]
    .filter(Boolean)
    .join(' · ')

  return (
    <>
      <tr className={issue.status === 'Ignored' ? 'muted-row' : undefined}>
        <td className="cell-url">
          {issue.pageId ? (
            <a href={`#/pages/${issue.pageId}`}>{shortUrl(issue.pageUrl)}</a>
          ) : (
            <span className="muted">site geneli</span>
          )}
          {issue.sampleUrls.length > 0 && (
            <div className="cell-sub">
              {issue.sampleUrls.slice(0, 3).map(shortUrl).join(', ')}
              {issue.sampleUrls.length > 3 && ` +${issue.sampleUrls.length - 3}`}
            </div>
          )}
        </td>
        <td className="cell-evidence">{evidence || '—'}</td>
        <td>
          {issue.status === 'Ignored' ? (
            <span className="badge">Yoksayıldı</span>
          ) : (
            <span className="badge ok">Açık</span>
          )}
        </td>
        <td className="cell-actions">
          {issue.status === 'Ignored' ? (
            <button type="button" className="btn btn-ghost btn-sm" onClick={reopen} disabled={busy}>
              Geri aç
            </button>
          ) : (
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={() => setIgnoring((v) => !v)}
            >
              {ignoring ? 'Vazgeç' : 'Yoksay'}
            </button>
          )}
        </td>
      </tr>

      {ignoring && (
        <tr className="inline-form-row">
          <td colSpan={4}>
            <div className="inline-form">
              <input
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                placeholder="Neden yoksayılıyor? (zorunlu)"
                aria-label="Yoksayma gerekçesi"
              />
              <label className="check">
                <input
                  type="checkbox"
                  checked={applyToSite}
                  onChange={(e) => setApplyToSite(e.target.checked)}
                />
                Tüm sitede bu kuralı yoksay
              </label>
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={confirmIgnore}
                disabled={busy || reason.trim().length === 0}
              >
                {busy ? '…' : 'Yoksay'}
              </button>
            </div>
            {error && <p className="form-error">{error}</p>}
          </td>
        </tr>
      )}
    </>
  )
}

/** Statik "nasil duzeltilir" metni + istege bagli LLM detaylandirmasi. */
function FixAdvice({ issue }: { issue: Issue }) {
  const [job, setJob] = useState<ContentJob | null>(null)
  const { busy, error, run } = useAction()
  const pollRef = useRef<number | null>(null)

  useEffect(() => {
    if (!job || job.status === 'Done' || job.status === 'Failed') return

    pollRef.current = window.setInterval(async () => {
      try {
        setJob(await getContentJob(job.id))
      } catch {
        // gecici hata — sonraki turda tekrar denenir
      }
    }, 2000)

    return () => {
      if (pollRef.current !== null) window.clearInterval(pollRef.current)
    }
  }, [job])

  const ask = () =>
    void run(async () => {
      const created = await generateContent({
        type: 'fix_advice',
        pageId: issue.pageId ?? undefined,
        input: {
          ruleCode: issue.ruleCode,
          ruleTitle: issue.ruleTitle,
          severity: issue.severity,
          evidence: issue.found,
          expected: issue.expected,
          sampleUrls: issue.sampleUrls.slice(0, 5),
          ruleDescription: issue.ruleDescription,
          whenToIgnore: issue.whenToIgnore,
          baseAdvice: issue.howToFix,
          docUrl: issue.docUrl,
        },
        variantCount: 1,
      })
      setJob(created)
    })

  const pending = job !== null && job.status !== 'Done' && job.status !== 'Failed'

  return (
    <Card>
      <div className="table-head">
        <h2 className="card-title">Nasıl düzeltilir?</h2>
        <button type="button" className="btn btn-ghost btn-sm" onClick={ask} disabled={busy || pending}>
          {pending ? 'Üretiliyor…' : 'AI ile detaylandır'}
        </button>
      </div>

      <p className="prose">{issue.howToFix ?? 'Bu kural için düzeltme notu tanımlanmamış.'}</p>

      {error && <p className="form-error">{error}</p>}

      {pending && <div className="state">Model çalışıyor, birkaç saniye sürebilir…</div>}

      {job?.status === 'Failed' && (
        <ErrorBox message={job.errorMessage ?? 'Üretim başarısız oldu.'} />
      )}

      {job?.status === 'Done' &&
        job.variants.map((variant) => (
          <div key={variant.id} className="advice">
            <pre>{variant.body}</pre>
          </div>
        ))}
    </Card>
  )
}
