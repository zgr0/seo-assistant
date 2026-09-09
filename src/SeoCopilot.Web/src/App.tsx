import { useEffect, useSyncExternalStore } from 'react'
import { logout } from './api/client.ts'
import { getSession, subscribe } from './api/session.ts'
import { CrawlPage } from './pages/CrawlPage.tsx'
import { IssuePage } from './pages/IssuePage.tsx'
import { LoginPage } from './pages/LoginPage.tsx'
import { PageDetailPage } from './pages/PageDetailPage.tsx'
import { SitesPage } from './pages/SitesPage.tsx'
import { match, navigate, useRoute } from './router.ts'
import './App.css'

export default function App() {
  const route = useRoute()
  const session = useSyncExternalStore(subscribe, getSession, getSession)

  // Oturum yoksa (ya da token yenilenemeyip dustuyse) giris ekranina don.
  useEffect(() => {
    if (!session && route !== '/login') navigate('/login')
    if (session && route === '/login') navigate('/sites')
  }, [session, route])

  if (!session) return <LoginPage />

  return (
    <div className="shell">
      <header className="topbar">
        <a className="brand" href="#/sites">
          SeoCopilot
        </a>
        <nav>
          <a href="#/sites" className={route.startsWith('/sites') ? 'active' : undefined}>
            Siteler
          </a>
        </nav>
        <div className="topbar-user">
          <span>{session.user.fullName || session.user.email}</span>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={() => void logout().then(() => navigate('/login'))}
          >
            Çıkış
          </button>
        </div>
      </header>

      <main className="content">
        <Routes route={route} />
      </main>
    </div>
  )
}

function Routes({ route }: { route: string }) {
  const rule = match('/crawls/:crawlId/rules/:ruleCode', route)
  // key: baska bir kurala geciste bilesen sifirdan kurulur, sayfalama durumu tasinmaz.
  if (rule) return <IssuePage key={route} crawlId={rule.crawlId} ruleCode={rule.ruleCode} />

  const crawl = match('/crawls/:crawlId', route)
  if (crawl) return <CrawlPage crawlId={crawl.crawlId} />

  const page = match('/pages/:pageId', route)
  if (page) return <PageDetailPage pageId={page.pageId} />

  if (match('/sites', route) || match('/', route)) return <SitesPage />

  return (
    <div className="page">
      <h1>Sayfa bulunamadı</h1>
      <a className="btn btn-primary" href="#/sites">
        Sitelere dön
      </a>
    </div>
  )
}
