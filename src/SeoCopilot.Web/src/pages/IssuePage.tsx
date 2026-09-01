import { useEffect, useRef, useState } from 'react'
import { generateContent, getContentJob, ignoreIssue, listIssues, reopenIssue } from '../api/client.ts'
import type { ContentJob, Issue } from '../api/types.ts'
import { categoryLabel, shortUrl } from '../components/format.ts'
import { Card, Empty, ErrorBox, SeverityBadge, Spinner } from '../components/ui.tsx'
import { useAction, useAsync } from '../hooks/useAsync.ts'

/**
 * Bulgu detayi kural bazindadir: ayni kural bir taramada onlarca sayfada tetiklenebilir,
 * bunlarin hepsi "etkilenen sayfalar" olarak tek ekranda toplanir.
 */
export function IssuePage({ crawlId, ruleCode }: { crawlId: string; ruleCode: string }) {
  const { data, error, loading, reload } = useAsync(
    () => listIssues(crawlId, { ruleCode, size: 200 }),
    [crawlId, ruleCode],
  )

  if (loading && !data) return <Spinner />
  if (error) return <ErrorBox message={error} onRetry={reload} />
  if (!data) return null
  if (data.items.length === 0) return <Empty>Bu kurala ait bulgu yok.</Empty>

  const first = data.items[0]
  const openIssues = data.items.filter((i) => i.status !== 'Ignored')

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
            {first.category && ` · ${categoryLabel(first.category)}`} · agirlik {first.weight} ·{' '}
            {data.total} bulgu ({openIssues.length} acik)
          </p>
        </div>
      </header>

      <Card>
        <h2 className="card-title">Kural ne diyor?</h2>
        <p>{first.ruleDescription ?? 'Bu kural icin aciklama tanimlanmamis.'}</p>
      </Card>

      <FixAdvice issue={first} />

      <Card>
        <h2 className="card-title">Etkilenen sayfalar</h2>
        <AffectedTable issues={data.items} onChanged={reload} />
      </Card>
    </div>
  )
}

function AffectedTable({ issues, onChanged }: { issues: Issue[]; onChanged: () => void }) {
  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>Sayfa</th>
            <th>Kanit</th>
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
            <span className="badge">Yoksayildi</span>
          ) : (
            <span className="badge ok">Acik</span>
          )}
        </td>
        <td className="cell-actions">
          {issue.status === 'Ignored' ? (
            <button type="button" className="btn btn-ghost btn-sm" onClick={reopen} disabled={busy}>
              Geri ac
            </button>
          ) : (
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              onClick={() => setIgnoring((v) => !v)}
            >
              {ignoring ? 'Vazgec' : 'Yoksay'}
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
                placeholder="Neden yoksayiliyor? (zorunlu)"
                aria-label="Yoksayma gerekcesi"
              />
              <label className="check">
                <input
                  type="checkbox"
                  checked={applyToSite}
                  onChange={(e) => setApplyToSite(e.target.checked)}
                />
                Tum sitede bu kurali yoksay
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
          baseAdvice: issue.howToFix,
        },
        variantCount: 1,
      })
      setJob(created)
    })

  const pending = job !== null && job.status !== 'Done' && job.status !== 'Failed'

  return (
    <Card>
      <div className="table-head">
        <h2 className="card-title">Nasil duzeltilir?</h2>
        <button type="button" className="btn btn-ghost btn-sm" onClick={ask} disabled={busy || pending}>
          {pending ? 'Uretiliyor…' : 'AI ile detaylandir'}
        </button>
      </div>

      <p>{issue.howToFix ?? 'Bu kural icin duzeltme notu tanimlanmamis.'}</p>

      {error && <p className="form-error">{error}</p>}

      {pending && <div className="state">Model calisiyor, birkac saniye surebilir…</div>}

      {job?.status === 'Failed' && (
        <ErrorBox message={job.errorMessage ?? 'Uretim basarisiz oldu.'} />
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
