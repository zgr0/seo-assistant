import { useState } from 'react'
import { login, register } from '../api/client.ts'
import { Field } from '../components/ui.tsx'
import { useAction } from '../hooks/useAsync.ts'
import { navigate } from '../router.ts'

type Mode = 'login' | 'register'

export function LoginPage() {
  const [mode, setMode] = useState<Mode>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fullName, setFullName] = useState('')
  const [tenantName, setTenantName] = useState('')
  const { busy, error, run, setError } = useAction()

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    void run(async () => {
      if (mode === 'login') {
        await login(email, password)
      } else {
        await register({ email, password, fullName, tenantName })
      }
      navigate('/sites')
    })
  }

  const switchMode = (next: Mode) => {
    setMode(next)
    setError(null)
  }

  return (
    <main className="auth">
      <div className="auth-card">
        <header className="auth-head">
          <h1>SeoCopilot</h1>
          <p>Site tarar, SEO skorunu çıkarır, düzeltmeyi söyler.</p>
        </header>

        <div className="tabs" role="tablist">
          <button
            type="button"
            role="tab"
            aria-selected={mode === 'login'}
            className={mode === 'login' ? 'tab active' : 'tab'}
            onClick={() => switchMode('login')}
          >
            Giriş
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={mode === 'register'}
            className={mode === 'register' ? 'tab active' : 'tab'}
            onClick={() => switchMode('register')}
          >
            Kayıt
          </button>
        </div>

        <form onSubmit={submit} className="form">
          {mode === 'register' && (
            <>
              <Field label="Ad soyad">
                <input
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  required
                  autoComplete="name"
                />
              </Field>
              <Field label="Kiracı adı" hint="Ajans veya şirket adı">
                <input
                  value={tenantName}
                  onChange={(e) => setTenantName(e.target.value)}
                  required
                  autoComplete="organization"
                />
              </Field>
            </>
          )}

          <Field label="E-posta">
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
            />
          </Field>

          <Field label="Şifre" hint={mode === 'register' ? 'En az 8 karakter' : undefined}>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={mode === 'register' ? 8 : undefined}
              autoComplete={mode === 'register' ? 'new-password' : 'current-password'}
            />
          </Field>

          {error && (
            <p className="form-error" role="alert">
              {error}
            </p>
          )}

          <button type="submit" className="btn btn-primary btn-block" disabled={busy}>
            {busy ? 'Gönderiliyor…' : mode === 'login' ? 'Giriş yap' : 'Hesap oluştur'}
          </button>
        </form>
      </div>
    </main>
  )
}
