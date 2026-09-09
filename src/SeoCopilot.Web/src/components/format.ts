// Saf bicimlendirme yardimcilari — bilesen dosyalarindan ayri tutulur ki
// fast refresh bozulmasin.

export function crawlStatusLabel(status: string): string {
  switch (status) {
    case 'Queued':
      return 'Kuyrukta'
    case 'Running':
      return 'Çalışıyor'
    case 'Completed':
      return 'Tamamlandı'
    case 'Partial':
      return 'Kısmi'
    case 'Failed':
      return 'Başarısız'
    case 'Cancelled':
      return 'İptal'
    default:
      return status
  }
}

export const severityLabels: Record<string, string> = {
  Critical: 'Kritik',
  High: 'Yüksek',
  Medium: 'Orta',
  Low: 'Düşük',
}

export function severityLabel(severity: string): string {
  return severityLabels[severity] ?? severity
}

/**
 * Kural agirliginin (1-10) skora etkisini duz dille anlatir — ham sayi tek basina
 * kullaniciya bir sey soylemiyor.
 */
export function weightNote(weight: number): string {
  if (weight >= 8) return 'skoru en çok düşüren gruptan, öncelikle bunu kapat'
  if (weight >= 5) return 'skora orta düzeyde etkisi var'
  return 'skora etkisi sınırlı, sırası geldiğinde ele al'
}

const categoryLabels: Record<string, string> = {
  indexability: 'Dizinlenebilirlik',
  meta: 'Meta',
  content: 'İçerik',
  links: 'Linkler',
  performance: 'Performans',
  structureddata: 'Yapısal veri',
  images: 'Görseller',
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
  if (!value) return 'hiç'
  const diff = Date.now() - new Date(value).getTime()
  const minutes = Math.round(diff / 60000)
  if (minutes < 1) return 'az önce'
  if (minutes < 60) return `${minutes} dk önce`
  const hours = Math.round(minutes / 60)
  if (hours < 24) return `${hours} saat önce`
  const days = Math.round(hours / 24)
  if (days < 30) return `${days} gün önce`
  return formatDate(value)
}

/** Dis baglantilarda kullanicinin nereye gidecegini gormesi icin yalniz alan adi. */
export function hostOf(url: string): string {
  try {
    return new URL(url).hostname.replace(/^www\./, '')
  } catch {
    return url
  }
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
