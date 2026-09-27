import type { BrandProfileInput } from '../api/types.ts'

/**
 * Herhangi bir sitede başlangıç noktası olarak kullanılabilecek marka sesleri. Veritabanına
 * kendiliğinden yazılmaz: seçilen şablon profil formunu doldurur, kullanıcı düzenleyip kaydeder.
 * Ad, renk, logo ve hashtag'ler markaya özel olduğundan şablonda yoktur — formda doldurulur.
 */
export interface BrandPreset {
  key: string
  label: string
  /** Kartta gösterilen kısa açıklama: hangi tür site için uygun. */
  description: string
  values: Pick<
    BrandProfileInput,
    'tone' | 'addressForm' | 'emojiUsage' | 'targetAudience' | 'extraContext' | 'bannedPhrases'
  >
}

export const BrandPresets: BrandPreset[] = [
  {
    key: 'kurumsal-b2b',
    label: 'Kurumsal B2B',
    description: 'Üretici, sanayi, hizmet firması — güven veren, ölçülü dil',
    values: {
      tone: 'Kurumsal',
      addressForm: 'Siz',
      emojiUsage: 'None',
      targetAudience: 'Satın alma sorumluları, işletme sahipleri ve teknik yöneticiler',
      extraContext:
        'Güven veren, ölçülü bir dil kullan. Somut fayda, kalite ve süreklilik vurgula; abartılı ' +
        'vaatlerden kaçın. Eylem çağrısı teklif almaya ya da iletişime geçmeye yönlendirsin.',
      bannedPhrases: ['en ucuz', 'kaçırmayın', 'şok fiyat', 'bedava', 'garantili kazanç'],
    },
  },
  {
    key: 'samimi-perakende',
    label: 'Samimi perakende',
    description: 'E-ticaret, mağaza, kafe — sıcak, enerjik, az emojili',
    values: {
      tone: 'Samimi',
      addressForm: 'Sen',
      emojiUsage: 'Light',
      targetAudience: 'Bireysel müşteriler ve online alışveriş yapanlar',
      extraContext:
        'Sıcak ve günlük bir dil kullan; ürünün hayata kattığı faydayı anlat. Kampanya ya da ' +
        'indirim sayfada yoksa uydurma. Eylem çağrısı kısa ve davetkâr olsun.',
      bannedPhrases: ['son şans', 'stoklar tükeniyor', 'mucize', 'hemen almazsan'],
    },
  },
  {
    key: 'teknik-uzman',
    label: 'Teknik uzman',
    description: 'Yazılım, mühendislik, sağlık, eğitim — kesin, bilgi odaklı',
    values: {
      tone: 'Teknik',
      addressForm: 'Siz',
      emojiUsage: 'None',
      targetAudience: 'Mühendisler, teknik ekipler ve konuya hâkim son kullanıcılar',
      extraContext:
        'Kesin ve ölçülebilir ifadeler kullan; sayfada olmayan değer, oran ya da sertifika ' +
        'uydurma. Pazarlama dili yerine kullanım alanını, özellikleri ve standartları öne çıkar.',
      bannedPhrases: ['devrim niteliğinde', 'sınırsız', 'mükemmel', 'eşsiz'],
    },
  },
]
