import { useCallback, useEffect, useRef, useState } from 'react'

interface AsyncState<T> {
  data: T | null
  error: string | null
  loading: boolean
}

/**
 * Veri cekme + yeniden yukleme. `deps` degistiginde tekrar calisir; sokulmus
 * bilesene set yapmamak icin son istegin sonucu haricindekiler yok sayilir.
 */
export function useAsync<T>(
  fetcher: () => Promise<T>,
  deps: unknown[],
): AsyncState<T> & { reload: () => void } {
  const [state, setState] = useState<AsyncState<T>>({ data: null, error: null, loading: true })
  const [nonce, setNonce] = useState(0)
  const fetcherRef = useRef(fetcher)
  fetcherRef.current = fetcher

  useEffect(() => {
    let active = true
    setState((prev) => ({ ...prev, loading: true, error: null }))

    fetcherRef
      .current()
      .then((data) => {
        if (active) setState({ data, error: null, loading: false })
      })
      .catch((error: unknown) => {
        if (active) {
          setState({
            data: null,
            error: error instanceof Error ? error.message : 'Beklenmeyen hata',
            loading: false,
          })
        }
      })

    return () => {
      active = false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, nonce])

  const reload = useCallback(() => setNonce((n) => n + 1), [])
  return { ...state, reload }
}

/** Bir kereye mahsus islemler (buton eylemleri) icin calisma durumu. */
export function useAction() {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const run = useCallback(async (action: () => Promise<void>) => {
    setBusy(true)
    setError(null)
    try {
      await action()
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Beklenmeyen hata')
    } finally {
      setBusy(false)
    }
  }, [])

  return { busy, error, run, setError }
}
