// Saf bicimlendirme yardimcilari — bilesen dosyalarindan ayri tutulur ki
// fast refresh bozulmasin.

export function crawlStatusLabel(status: string): string {
  switch (status) {
    case 'Queued':
      return 'Kuyrukta'
    case 'Running':
      return 'Calisiyor'
    case 'Completed':
      return 'Tamamlandi'
    case 'Partial':
      return 'Kismi'
    case 'Failed':
      return 'Basarisiz'
    case 'Cancelled':
      return 'Iptal'
    default:
      return status
  }
}

export const severityLabels: Record<string, string> = {
  Critical: 'Kritik',
  High: 'Yuksek',
  Medium: 'Orta',
  Low: 'Dusuk',
}

export function severityLabel(severity: string): string {
  return severityLabels[severity] ?? severity
}

const categoryLabels: Record<string, string> = {
  indexability: 'Dizinlenebilirlik',
  meta: 'Meta',
  content: 'Icerik',
  links: 'Linkler',
  performance: 'Performans',
  structureddata: 'Yapisal veri',
  images: 'Gorseller',
  i18n: 'Dil',
}

export function categoryLabel(key: string): string {
  return categoryLabels[key.toLowerCase().replace(/[_-]/g, '')] ?? key
}

/** Skor rengi: 80+ iyi, 50+ orta, altisi kotu. */
export function scoreTone(score: number | null): 'good' | 'warn' | 'bad' | 'none' {
  if (score === null) return 'none'
  if (score >= 80) return 'good'
  if (score >= 50) return 'warn'
  return 'bad'
}

export function formatDate(value: string | null): string {
  if (!value) return '—'
  return new Date(value).toLocaleString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/** "3 saat once" gibi kisa goreli zaman. */
export function timeAgo(value: string | null): string {
  if (!value) return 'hic'
  const diff = Date.now() - new Date(value).getTime()
  const minutes = Math.round(diff / 60000)
  if (minutes < 1) return 'az once'
  if (minutes < 60) return `${minutes} dk once`
  const hours = Math.round(minutes / 60)
  if (hours < 24) return `${hours} saat once`
  const days = Math.round(hours / 24)
  if (days < 30) return `${days} gun once`
  return formatDate(value)
}

/** Tablolarda tam URL yerine yol gosterilir. */
export function shortUrl(url: string | null): string {
  if (!url) return '—'
  try {
    const parsed = new URL(url)
    return parsed.pathname + parsed.search || '/'
  } catch {
    return url
  }
}
