import { useEffect, useRef, useState } from 'react'
import {
  createBrandProfile,
  deleteBrandLogo,
  deleteBrandProfile,
  getBrandLogoBlob,
  updateBrandProfile,
  uploadBrandLogo,
} from '../api/client.ts'
import type {
  AddressForm,
  BrandProfile,
  BrandProfileInput,
  BrandTone,
  EmojiUsage,
  PlatformProfile,
} from '../api/types.ts'
import { useAction } from '../hooks/useAsync.ts'
import { type BrandPreset, BrandPresets } from './brandPresets.ts'
import { Empty, Field } from './ui.tsx'

const ToneOptions: { value: BrandTone; label: string }[] = [
  { value: 'Kurumsal', label: 'Kurumsal' },
  { value: 'Samimi', label: 'Samimi' },
  { value: 'Teknik', label: 'Teknik' },
  { value: 'SatisOdakli', label: 'Satış odaklı' },
]

const AddressOptions: { value: AddressForm; label: string }[] = [
  { value: 'Siz', label: 'Siz dili' },
  { value: 'Sen', label: 'Sen dili' },
]

const EmojiOptions: { value: EmojiUsage; label: string }[] = [
  { value: 'None', label: 'Kullanma' },
  { value: 'Light', label: 'Az' },
  { value: 'Heavy', label: 'Bol' },
]

/** Şablon kartındaki kısa özet — seçim kutusundaki "Kullanma" emir kipi gibi okunmasın. */
const EmojiSummary: Record<EmojiUsage, string> = {
  None: 'emojisiz',
  Light: 'az emoji',
  Heavy: 'bol emoji',
}

/** Sunucudaki sınırlar (BrandProfileRules) — formda erken gösterilir, asıl denetim sunucuda. */
const Limits = {
  name: 200,
  targetAudience: 1024,
  extraContext: 2000,
  bannedPhrases: 50,
  hashtags: 10,
  logoBytes: 2 * 1024 * 1024,
}

const LogoTypes = ['image/png', 'image/jpeg', 'image/webp']

function toneLabel(tone: BrandTone) {
  return ToneOptions.find((o) => o.value === tone)?.label ?? tone
}

/**
 * Sitenin marka profilleri: liste, oluşturma, düzenleme, silme ve logo. Liste üst bileşenden
 * gelir (profil seçim kutusuyla aynı veri); her değişiklikten sonra {@link onChanged} ile tazelenir.
 */
export function BrandProfilePanel({
  siteId,
  siteName,
  profiles,
  platforms,
  onChanged,
  onClose,
}: {
  siteId: string
  siteName: string
  /** Bu siteye özel ve kiracı geneli profiller. */
  profiles: BrandProfile[]
  platforms: PlatformProfile[]
  /** Yeni profil oluşturulduysa o profil verilir — seçim kutusu ona geçebilsin. */
  onChanged: (created?: BrandProfile) => void
  onClose: () => void
}) {
  // null: liste; profile null: yeni profil (boş ya da şablondan); profile dolu: düzenleme.
  const [editing, setEditing] = useState<{ profile: BrandProfile | null; preset?: BrandPreset } | null>(
    null,
  )
  const remove = useAction()

  const removeProfile = (profile: BrandProfile) =>
    void remove.run(async () => {
      if (
        !window.confirm(
          `"${profile.name}" profili ve logosu silinecek. Bu profille üretilmiş gönderiler kalır. Emin misiniz?`,
        )
      )
        return
      await deleteBrandProfile(profile.id)
      onChanged()
    })

  return (
    <section className="brand-panel" aria-label="Marka profilleri">
      <div className="brand-panel-head">
        <div>
          <h2 className="brand-panel-title">Marka profilleri</h2>
          <p className="muted">
            Ton, hitap, yasaklı ifadeler ve hashtag'ler gönderi metnine; renk ve logo görsele uygulanır.
          </p>
        </div>
        <button type="button" className="btn btn-ghost btn-sm" onClick={onClose}>
          Kapat
        </button>
      </div>

      {editing ? (
        <BrandProfileEditor
          key={editing.profile?.id ?? `new:${editing.preset?.key ?? ''}`}
          profile={editing.profile}
          preset={editing.preset}
          siteId={siteId}
          siteName={siteName}
          platforms={platforms}
          onCancel={() => setEditing(null)}
          onSaved={(created) => {
            setEditing(null)
            onChanged(created)
          }}
        />
      ) : (
        <>
          {profiles.length === 0 ? (
            <Empty>Bu site için henüz marka profili yok — aşağıdaki şablonlardan biriyle başlayabilirsiniz.</Empty>
          ) : (
            <ul className="brand-list">
              {profiles.map((profile) => (
                <li key={profile.id} className="brand-row">
                  <Swatches primary={profile.primaryColor} accent={profile.accentColor} />
                  <div className="brand-row-main">
                    <span className="brand-row-name">{profile.name}</span>
                    <span className="brand-row-meta">
                      {toneLabel(profile.tone)} · {profile.addressForm === 'Sen' ? 'sen dili' : 'siz dili'}
                      {profile.hasLogo ? ' · logolu' : ''}
                    </span>
                  </div>
                  <span className="badge" title="Profilin seçilebildiği yer">
                    {profile.siteId ? 'Bu site' : 'Tüm siteler'}
                  </span>
                  {profile.isDefault && <span className="badge ok">varsayılan</span>}
                  <div className="brand-row-actions">
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm"
                      onClick={() => setEditing({ profile })}
                    >
                      Düzenle
                    </button>
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm tone-bad"
                      onClick={() => removeProfile(profile)}
                      disabled={remove.busy}
                    >
                      Sil
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}

          {remove.error && <p className="form-error">{remove.error}</p>}

          <div>
            <button
              type="button"
              className="btn btn-primary btn-sm"
              onClick={() => setEditing({ profile: null })}
            >
              + Yeni profil
            </button>
          </div>

          <div className="field">
            <span className="field-label">Hazır şablondan başlat</span>
            <div className="preset-grid">
              {BrandPresets.map((preset) => (
                <button
                  key={preset.key}
                  type="button"
                  className="preset-card"
                  onClick={() => setEditing({ profile: null, preset })}
                >
                  <span className="preset-title">{preset.label}</span>
                  <span className="preset-desc">{preset.description}</span>
                  <span className="preset-meta">
                    {toneLabel(preset.values.tone)} ·{' '}
                    {preset.values.addressForm === 'Sen' ? 'sen dili' : 'siz dili'} ·{' '}
                    {EmojiSummary[preset.values.emojiUsage]}
                  </span>
                </button>
              ))}
            </div>
            <span className="field-hint">
              Şablon formu doldurur; ad, renk, logo ve hashtag'leri markanıza göre tamamlayıp kaydedin.
            </span>
          </div>
        </>
      )}
    </section>
  )
}

/** Ana ve vurgu rengi; renk yoksa kesikli boş daire. */
function Swatches({ primary, accent }: { primary: string | null; accent: string | null }) {
  return (
    <span className="swatches" aria-hidden="true">
      <span
        className={`swatch ${primary ? '' : 'swatch-empty'}`.trim()}
        style={primary ? { background: primary } : undefined}
      />
      {accent && <span className="swatch" style={{ background: accent }} />}
    </span>
  )
}

/**
 * Formun başlangıç değeri. Şablondan başlanıyorsa ses alanları şablondan gelir; ad sitenin adıyla
 * dolar — görselde marka satırı olarak basıldığı için şablonun adı değil markanın adı olmalı.
 */
function toInput(
  profile: BrandProfile | null,
  siteId: string,
  siteName: string,
  preset?: BrandPreset,
): BrandProfileInput {
  if (!profile) {
    return {
      name: preset ? siteName : '',
      siteId,
      tone: preset?.values.tone ?? 'Kurumsal',
      addressForm: preset?.values.addressForm ?? 'Siz',
      emojiUsage: preset?.values.emojiUsage ?? 'None',
      bannedPhrases: [...(preset?.values.bannedPhrases ?? [])],
      defaultHashtags: [],
      targetAudience: preset?.values.targetAudience ?? '',
      extraContext: preset?.values.extraContext ?? '',
      isDefault: false,
      socialHandles: {},
      primaryColor: '',
      accentColor: '',
    }
  }

  return {
    name: profile.name,
    siteId: profile.siteId,
    tone: profile.tone,
    addressForm: profile.addressForm,
    emojiUsage: profile.emojiUsage,
    bannedPhrases: [...profile.bannedPhrases],
    defaultHashtags: [...profile.defaultHashtags],
    targetAudience: profile.targetAudience ?? '',
    extraContext: profile.extraContext ?? '',
    isDefault: profile.isDefault,
    socialHandles: { ...profile.socialHandles },
    primaryColor: profile.primaryColor ?? '',
    accentColor: profile.accentColor ?? '',
  }
}

/** Sunucuyla aynı kural: '#' eklenir, boşluk ve noktalama atılır, yazım korunur. */
function toHashtag(value: string) {
  const bare = value.trim().replace(/^#+/, '').replace(/[^\p{L}\p{N}_]/gu, '')
  return bare ? `#${bare}` : ''
}

function BrandProfileEditor({
  profile,
  preset,
  siteId,
  siteName,
  platforms,
  onCancel,
  onSaved,
}: {
  profile: BrandProfile | null
  /** Yeni profil hazır şablondan başlatıldıysa. */
  preset?: BrandPreset
  siteId: string
  siteName: string
  platforms: PlatformProfile[]
  onCancel: () => void
  onSaved: (created?: BrandProfile) => void
}) {
  const [form, setForm] = useState(() => toInput(profile, siteId, siteName, preset))
  const [logo, pickFile] = usePickedFile()
  const [logoRemoved, setLogoRemoved] = useState(false)
  const { busy, error, run, setError } = useAction()

  // Yeni profil kaydedilip logo yüklemesi düşerse ikinci deneme profili yeniden oluşturmasın.
  const created = useRef<BrandProfile | null>(null)

  const set = <K extends keyof BrandProfileInput>(key: K, value: BrandProfileInput[K]) =>
    setForm((prev) => ({ ...prev, [key]: value }))

  const setHandle = (code: string, value: string) =>
    setForm((prev) => ({
      ...prev,
      socialHandles: { ...prev.socialHandles, [code]: value.replace(/^@+/, '') },
    }))

  const pickLogo = (file: File | undefined) => {
    if (!file) return
    if (!LogoTypes.includes(file.type)) {
      setError('Logo PNG, JPEG ya da WebP olmalı (SVG desteklenmiyor)')
      return
    }
    if (file.size > Limits.logoBytes) {
      setError('Logo en fazla 2 MB olabilir')
      return
    }
    setError(null)
    pickFile(file)
    setLogoRemoved(false)
  }

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    void run(async () => {
      const id = profile?.id ?? created.current?.id
      const saved = id ? await updateBrandProfile(id, form) : await createBrandProfile(form)
      if (!id) created.current = saved

      // Logo profilden sonra: yeni profilin kimliği ancak şimdi belli.
      if (logo) await uploadBrandLogo(saved.id, logo.file)
      else if (logoRemoved && profile?.hasLogo) await deleteBrandLogo(saved.id)

      onSaved(created.current ?? undefined)
    })
  }

  return (
    <form className="form brand-editor" onSubmit={submit}>
      <h3 className="brand-editor-title">
        {profile
          ? `"${profile.name}" profilini düzenle`
          : preset
            ? `Yeni marka profili · ${preset.label} şablonu`
            : 'Yeni marka profili'}
      </h3>

      <div className="form-grid">
        <Field label="Profil adı" hint="Görselde marka satırı olarak da basılır">
          <input
            value={form.name}
            onChange={(e) => set('name', e.target.value)}
            maxLength={Limits.name}
            required
            autoFocus
          />
        </Field>

        <Field label="Kapsam" hint="Tüm siteler: her sitenin formunda seçilebilir">
          <select value={form.siteId ?? ''} onChange={(e) => set('siteId', e.target.value || null)}>
            <option value={siteId}>Yalnız {siteName}</option>
            <option value="">Tüm siteler</option>
          </select>
        </Field>

        <Field label="Ton">
          <select value={form.tone} onChange={(e) => set('tone', e.target.value as BrandTone)}>
            {ToneOptions.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
        </Field>

        <Field label="Hitap">
          <select
            value={form.addressForm}
            onChange={(e) => set('addressForm', e.target.value as AddressForm)}
          >
            {AddressOptions.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
        </Field>

        <Field label="Emoji">
          <select
            value={form.emojiUsage}
            onChange={(e) => set('emojiUsage', e.target.value as EmojiUsage)}
          >
            {EmojiOptions.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
        </Field>

        <Field label="Hedef kitle" hint="Ör. KOBİ sahipleri, üretim müdürleri">
          <input
            value={form.targetAudience}
            onChange={(e) => set('targetAudience', e.target.value)}
            maxLength={Limits.targetAudience}
          />
        </Field>
      </div>

      <Field
        label="Marka tanıtımı"
        hint={`Ne yaparsınız, farkınız ne? Modele bağlam olarak verilir · ${form.extraContext.length}/${Limits.extraContext}`}
      >
        <textarea
          rows={3}
          value={form.extraContext}
          onChange={(e) => set('extraContext', e.target.value)}
          maxLength={Limits.extraContext}
        />
      </Field>

      <TagField
        label="Yasaklı ifadeler"
        hint="Bu ifadeleri içeren gönderi kullanılmaz; büyük/küçük harf fark etmez"
        values={form.bannedPhrases}
        onChange={(values) => set('bannedPhrases', values)}
        max={Limits.bannedPhrases}
        placeholder="ör. en ucuz — Enter ile ekleyin"
      />

      <TagField
        label="Varsayılan hashtag'ler"
        hint="Her gönderide önce bunlar kullanılır"
        values={form.defaultHashtags}
        onChange={(values) => set('defaultHashtags', values)}
        max={Limits.hashtags}
        placeholder="#markaniz"
        normalize={toHashtag}
      />

      <div className="field">
        <span className="field-label">Sosyal medya hesapları</span>
        <div className="form-grid">
          {platforms.map((platform) => (
            <label key={platform.code} className="field">
              <span className="field-hint">{platform.displayName}</span>
              <span className="input-prefix">
                <span aria-hidden="true">@</span>
                <input
                  value={form.socialHandles[platform.code] ?? ''}
                  onChange={(e) => setHandle(platform.code, e.target.value)}
                  placeholder="hesapadi"
                />
              </span>
            </label>
          ))}
        </div>
        <span className="field-hint">
          Bağlantı konamayan platformlarda (Instagram) eylem çağrısı bu hesaba yönlendirir.
        </span>
      </div>

      <div className="form-grid">
        <ColorField
          label="Ana renk"
          hint="Görsel paneli ve marka kartı; beyaz yazı okunsun diye koyulaştırılabilir"
          value={form.primaryColor}
          onChange={(value) => set('primaryColor', value)}
        />
        <ColorField
          label="Vurgu rengi"
          hint="Çizgi ve nokta; boşsa ana renkten türetilir"
          value={form.accentColor}
          onChange={(value) => set('accentColor', value)}
        />
      </div>

      <LogoField
        profileId={profile?.hasLogo && !logoRemoved ? profile.id : null}
        pickedUrl={logo?.url ?? null}
        onPick={pickLogo}
        onRemove={() => {
          pickFile(null)
          setLogoRemoved(true)
        }}
      />

      <div className="field">
        <label className="check">
          <input
            type="checkbox"
            checked={form.isDefault}
            onChange={(e) => set('isDefault', e.target.checked)}
          />
          Bu kapsamın varsayılanı
        </label>
        <span className="field-hint">Profil seçilmeden başlatılan üretimlerde kullanılır</span>
      </div>

      <div className="row-actions">
        <button type="submit" className="btn btn-primary" disabled={busy}>
          {busy ? 'Kaydediliyor…' : 'Kaydet'}
        </button>
        <button type="button" className="btn btn-ghost" onClick={onCancel} disabled={busy}>
          Vazgeç
        </button>
      </div>

      {error && <p className="form-error">{error}</p>}
    </form>
  )
}

/**
 * Etiket girişi: Enter ya da virgül ekler, Backspace son etiketi siler, yapıştırılan virgüllü
 * metin bölünür. Tekrarlar (Türkçe büyük/küçük harf farksız) eklenmez.
 */
function TagField({
  label,
  hint,
  values,
  onChange,
  max,
  placeholder,
  normalize = (value) => value.trim().replace(/\s+/g, ' '),
}: {
  label: string
  hint: string
  values: string[]
  onChange: (values: string[]) => void
  max: number
  placeholder: string
  normalize?: (value: string) => string
}) {
  const [draft, setDraft] = useState('')

  const add = (raw: string) => {
    const next = [...values]
    for (const part of raw.split(',')) {
      const value = normalize(part)
      const key = value.toLocaleLowerCase('tr')
      if (value && !next.some((v) => v.toLocaleLowerCase('tr') === key)) next.push(value)
    }
    onChange(next.slice(0, max))
    setDraft('')
  }

  const onKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter' || event.key === ',') {
      // Enter formu göndermesin.
      event.preventDefault()
      if (draft.trim()) add(draft)
    } else if (event.key === 'Backspace' && !draft && values.length > 0) {
      onChange(values.slice(0, -1))
    }
  }

  return (
    <div className="field">
      <span className="field-label">{label}</span>
      <div className="tag-input">
        {values.map((value) => (
          <span key={value} className="tag">
            {value}
            <button
              type="button"
              aria-label={`${value} kaldır`}
              onClick={() => onChange(values.filter((v) => v !== value))}
            >
              ×
            </button>
          </span>
        ))}
        {values.length < max && (
          <input
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={onKeyDown}
            onBlur={() => draft.trim() && add(draft)}
            placeholder={values.length === 0 ? placeholder : ''}
            aria-label={label}
          />
        )}
      </div>
      <span className="field-hint">
        {hint} · {values.length}/{max}
      </span>
    </div>
  )
}

const HexColor = /^#[0-9a-fA-F]{6}$/

/** Renk seçici + onaltılık metin; boş bırakılabilir (renk temizlenir). */
function ColorField({
  label,
  hint,
  value,
  onChange,
}: {
  label: string
  hint: string
  value: string
  onChange: (value: string) => void
}) {
  const invalid = value !== '' && !HexColor.test(value)

  return (
    <div className="field">
      <span className="field-label">{label}</span>
      <div className="color-field">
        <input
          type="color"
          value={HexColor.test(value) ? value : '#000000'}
          onChange={(e) => onChange(e.target.value.toUpperCase())}
          aria-label={`${label} seç`}
        />
        <input
          value={value}
          onChange={(e) => onChange(e.target.value.trim())}
          placeholder="#RRGGBB"
          maxLength={7}
          aria-label={`${label} (onaltılık)`}
          aria-invalid={invalid}
        />
        {value && (
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => onChange('')}>
            Temizle
          </button>
        )}
      </div>
      <span className={invalid ? 'field-hint tone-bad' : 'field-hint'}>
        {invalid ? '#RRGGBB biçiminde olmalı' : hint}
      </span>
    </div>
  )
}

/** Mevcut logo (yetkili uçtan) ya da yeni seçilen dosyanın önizlemesi. */
function LogoField({
  profileId,
  pickedUrl,
  onPick,
  onRemove,
}: {
  /** Kayıtlı logo gösterilecekse profilin kimliği; yoksa null. */
  profileId: string | null
  /** Yeni seçilen dosyanın önizleme adresi; seçilmediyse null. */
  pickedUrl: string | null
  onPick: (file: File | undefined) => void
  onRemove: () => void
}) {
  const stored = useLogoUrl(pickedUrl ? null : profileId)
  const url = pickedUrl ?? stored

  return (
    <div className="field">
      <span className="field-label">Logo</span>
      <div className="logo-field">
        <div className="logo-preview">
          {url ? <img src={url} alt="Logo önizlemesi" /> : <span className="muted">Logo yok</span>}
        </div>
        <label className="btn btn-ghost btn-sm">
          {url ? 'Değiştir' : 'Dosya seç'}
          <input
            type="file"
            accept={LogoTypes.join(',')}
            hidden
            onChange={(e) => {
              onPick(e.target.files?.[0])
              // Aynı dosya yeniden seçilebilsin.
              e.target.value = ''
            }}
          />
        </label>
        {url && (
          <button type="button" className="btn btn-ghost btn-sm tone-bad" onClick={onRemove}>
            Kaldır
          </button>
        )}
      </div>
      <span className="field-hint">
        PNG, JPEG ya da WebP, en fazla 2 MB. Saydam PNG önerilir; görselde marka adının yanında beyaz
        rozet içinde basılır.
      </span>
    </div>
  )
}

/**
 * Kayıtlı logonun object URL'i — istek Bearer başlığı ister, <img src> doğrudan kullanılamaz.
 * Kimlik değişince ya da bileşen sökülünce URL serbest bırakılır.
 */
function useLogoUrl(profileId: string | null) {
  const [state, setState] = useState<{ id: string; url: string | null } | null>(null)

  useEffect(() => {
    if (!profileId) return

    let active = true
    let objectUrl: string | null = null

    getBrandLogoBlob(profileId)
      .then((blob) => {
        if (!active) return
        objectUrl = URL.createObjectURL(blob)
        setState({ id: profileId, url: objectUrl })
      })
      .catch(() => {
        if (active) setState({ id: profileId, url: null })
      })

    return () => {
      active = false
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [profileId])

  // Kimlik değiştiyse (ya da logo kaldırıldıysa) önceki logo gösterilmesin.
  return profileId && state?.id === profileId ? state.url : null
}

/**
 * Seçilen logo dosyası ve önizleme adresi. Adres seçim anında üretilir; yeni seçimde, temizlikte
 * ve bileşen sökülünce serbest bırakılır.
 */
function usePickedFile() {
  const [picked, setPicked] = useState<{ file: File; url: string } | null>(null)
  const current = useRef<string | null>(null)

  const replace = (file: File | null) => {
    if (current.current) URL.revokeObjectURL(current.current)
    const next = file ? { file, url: URL.createObjectURL(file) } : null
    current.current = next?.url ?? null
    setPicked(next)
  }

  useEffect(
    () => () => {
      if (current.current) URL.revokeObjectURL(current.current)
    },
    [],
  )

  return [picked, replace] as const
}
