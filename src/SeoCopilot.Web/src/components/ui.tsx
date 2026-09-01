import type { ReactNode } from 'react'
import type { Severity } from '../api/types.ts'
import { categoryLabel, crawlStatusLabel, scoreTone, severityLabel } from './format.ts'

export function Spinner({ label = 'Yukleniyor' }: { label?: string }) {
  return (
    <div className="state" role="status">
      <span className="spinner" aria-hidden="true" />
      {label}
    </div>
  )
}

export function ErrorBox({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="state state-error" role="alert">
      <span>{message}</span>
      {onRetry && (
        <button type="button" className="btn btn-ghost btn-sm" onClick={onRetry}>
          Tekrar dene
        </button>
      )}
    </div>
  )
}

export function Empty({ children }: { children: ReactNode }) {
  return <div className="state state-empty">{children}</div>
}

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <div className={`card ${className}`.trim()}>{children}</div>
}

export function Field({
  label,
  hint,
  children,
}: {
  label: string
  hint?: string
  children: ReactNode
}) {
  return (
    <label className="field">
      <span className="field-label">{label}</span>
      {children}
      {hint && <span className="field-hint">{hint}</span>}
    </label>
  )
}

export function SeverityBadge({ severity }: { severity: Severity | string }) {
  return (
    <span className={`badge sev-${String(severity).toLowerCase()}`}>{severityLabel(severity)}</span>
  )
}

export function StatusBadge({ status }: { status: string }) {
  return <span className={`badge status-${status.toLowerCase()}`}>{crawlStatusLabel(status)}</span>
}

export function ScoreGauge({
  score,
  size = 168,
  label = 'Genel skor',
}: {
  score: number | null
  size?: number
  label?: string
}) {
  const value = score ?? 0
  const radius = 54
  const circumference = 2 * Math.PI * radius
  // 3/4 daire (270°) cizilir; kalan bosluk gostergenin acikligi.
  const arc = circumference * 0.75
  const filled = (arc * Math.min(Math.max(value, 0), 100)) / 100

  return (
    <div className={`gauge tone-${scoreTone(score)}`} style={{ width: size, height: size }}>
      <svg viewBox="0 0 140 140" role="img" aria-label={`${label}: ${score ?? 'yok'}`}>
        <circle
          className="gauge-track"
          cx="70"
          cy="70"
          r={radius}
          strokeDasharray={`${arc} ${circumference}`}
        />
        <circle
          className="gauge-value"
          cx="70"
          cy="70"
          r={radius}
          strokeDasharray={`${filled} ${circumference}`}
        />
      </svg>
      <div className="gauge-text">
        <strong>{score === null ? '—' : score.toFixed(1)}</strong>
        <span>{label}</span>
      </div>
    </div>
  )
}

export function TrendArrow({ delta }: { delta: number | null }) {
  if (delta === null || delta === 0) {
    return (
      <span className="trend trend-flat" title="Degisim yok">
        —
      </span>
    )
  }
  const up = delta > 0
  return (
    <span className={`trend ${up ? 'trend-up' : 'trend-down'}`}>
      {up ? '▲' : '▼'} {Math.abs(delta).toFixed(1)}
    </span>
  )
}

export function CategoryBars({ scores }: { scores: Record<string, number> }) {
  const entries = Object.entries(scores).sort(([a], [b]) => a.localeCompare(b))
  if (entries.length === 0) return <Empty>Kategori skoru yok.</Empty>

  return (
    <ul className="bars">
      {entries.map(([key, score]) => (
        <li key={key}>
          <div className="bar-head">
            <span>{categoryLabel(key)}</span>
            <strong className={`tone-${scoreTone(score)}`}>{score.toFixed(1)}</strong>
          </div>
          <div className="bar-track">
            <div
              className={`bar-fill tone-${scoreTone(score)}`}
              style={{ width: `${Math.min(Math.max(score, 0), 100)}%` }}
            />
          </div>
        </li>
      ))}
    </ul>
  )
}

export function Pager({
  page,
  size,
  total,
  onChange,
}: {
  page: number
  size: number
  total: number
  onChange: (page: number) => void
}) {
  const lastPage = Math.max(1, Math.ceil(total / size))
  if (lastPage === 1) return null

  return (
    <div className="pager">
      <button
        type="button"
        className="btn btn-ghost btn-sm"
        disabled={page <= 1}
        onClick={() => onChange(page - 1)}
      >
        Onceki
      </button>
      <span>
        {page} / {lastPage} · {total} kayit
      </span>
      <button
        type="button"
        className="btn btn-ghost btn-sm"
        disabled={page >= lastPage}
        onClick={() => onChange(page + 1)}
      >
        Sonraki
      </button>
    </div>
  )
}
