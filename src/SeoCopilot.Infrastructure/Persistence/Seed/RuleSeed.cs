using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Infrastructure.Persistence.Seed;

/// <summary>
/// rules tablosu seed'i. Kural motoru handler kodlariyla birebir hizali olmak zorunda —
/// issues.rule_code buraya FK.
/// </summary>
/// <remarks>
/// Metin sozlesmesi (arayuzde bulgu detay sayfasi bunlari oldugu gibi basar):
/// <list type="bullet">
///   <item>DescriptionTr: duz paragraf — ne tetikledi, neden onemli, en sik sebep.</item>
///   <item>WhenToIgnoreTr: tek paragraf — mesru istisnalar ya da istisnasi olmadigi bilgisi.</item>
///   <item>HowToFixTr: "1) ... \n2) ..." numarali adimlar; satir sonlari anlamli, pre-line ile basilir.</item>
///   <item>DocUrl: birincil kaynak (Search Central / web.dev / spesifikasyon).</item>
/// </list>
/// Metinler gercek Turkce yazilir (aksanli harfler dahil); kod ornekleri ve URL'ler ASCII kalir.
/// </remarks>
internal sealed class RuleSeed : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> b)
    {
        b.HasData(
            // --- Indexability ---
            R("ROBOTS_NOINDEX", RuleCategory.Indexability, Severity.Critical, 9,
                "noindex etiketi",
                "Sayfanın robots meta etiketinde ya da X-Robots-Tag yanıt başlığında noindex var. "
                + "Bu, arama motorlarına \"bu sayfayı dizine ekleme\" demektir; sayfa taransa bile "
                + "sonuçlarda hiç görünmez ve aldığı iç linklerin değeri bir yere aktarılmaz. "
                + "En sık sebep, test ortamından canlıya taşınan tema veya CMS ayarıdır.",
                "Sepet, hesap, ödeme ve teşekkür sayfalarında noindex bilinçli ve doğru bir "
                + "tercihtir. Trafik beklenen bir içerik sayfasında ise yoksayma; bu, en ağır "
                + "hatalardan biridir.",
                "1) Sayfa kaynağındaki <meta name=\"robots\"> etiketini ve HTTP yanıtındaki "
                + "X-Robots-Tag başlığını birlikte kontrol et; noindex ikisinden birinde olabilir.\n"
                + "2) Sayfa dizine girmeliyse değeri index,follow yap veya etiketi tamamen kaldır.\n"
                + "3) CMS/tema ayarlarında \"arama motorlarını engelle\" seçeneği açıksa kapat.\n"
                + "4) Düzeltmeden sonra Search Console > URL Denetimi ile sayfayı yeniden tara.",
                "https://developers.google.com/search/docs/crawling-indexing/block-indexing"),

            R("BLOCKED_BY_ROBOTS_TXT", RuleCategory.Indexability, Severity.High, 7,
                "robots.txt engeli",
                "robots.txt içindeki bir Disallow kuralı, taranması gereken iç adresleri kapatıyor. "
                + "Engellenen adresi arama motoru indiremez; içeriği okunamadığı için sayfa ya hiç "
                + "dizine girmez ya da başlıksız-özetsiz boş bir kayıt olarak listelenir. Dikkat: "
                + "robots.txt engeli noindex ile aynı şey değildir — engellenen sayfa dış linkler "
                + "üzerinden yine de dizine düşebilir.",
                "Yönetim paneli, iç arama sonuçları ve tekrar eden filtre adresleri için engel "
                + "doğrudur; bu adreslerde bulguyu yoksayabilirsin. İçerik sayfalarında ise "
                + "doğrudan trafik kaybı demektir.",
                "1) robots.txt dosyasını aç ve engellenen yolu hangi Disallow satırının "
                + "yakaladığını bul.\n"
                + "2) Kuralı daralt: tüm dizini kapatmak yerine yalnızca gizlenmesi gereken yolu "
                + "yaz (örn. Disallow: /admin/ yerine Disallow: /admin/logs/).\n"
                + "3) Amacın taramayı değil dizine girmeyi engellemekse robots.txt yerine noindex "
                + "kullan; ikisini aynı sayfada birlikte kullanma — engellenen sayfada noindex "
                + "hiç okunamaz.\n"
                + "4) Search Console'un robots.txt raporuyla doğrula, sonra URL Denetimi'nden "
                + "yeniden tara.",
                "https://developers.google.com/search/docs/crawling-indexing/robots/intro"),

            R("BROKEN_PAGE_4XX", RuleCategory.Indexability, Severity.Critical, 10,
                "Sayfa 4xx dönüyor",
                "Sayfa 4xx (çoğunlukla 404 veya 410) dönüyor; içerik sunucuda yok. Kullanıcı hata "
                + "sayfasına düşer, arama motoru da adresi dizinden çıkarır. Sayfaya iç link veya "
                + "dış backlink geliyorsa o bağlantıların biriktirdiği değer de boşa gider.",
                "Silinmiş kampanya adresleri için 410 bilinçli bir tercih olabilir. Ancak adres "
                + "hâlâ bir yerden linkleniyorsa yoksayma — önce linki temizlemek gerekir.",
                "1) Adres kalıcı olarak kaldırıldıysa en yakın ilgili sayfaya 301 yönlendirme koy; "
                + "her şeyi ana sayfaya yönlendirme — alakasız hedef yumuşak 404 sayılır.\n"
                + "2) Adres yanlışlıkla kırıldıysa içeriği geri getir veya doğru URL'e düzelt.\n"
                + "3) Bu sayfaya işaret eden iç linkleri yeni adresle değiştir; yönlendirme kalıcı "
                + "çözüm değil, geçiş köprüsüdür.\n"
                + "4) İçerik gerçekten dönmeyecekse 410 dön ve adresi sitemap'ten çıkar.",
                "https://developers.google.com/search/docs/crawling-indexing/http-network-errors"),

            R("SERVER_ERROR_5XX", RuleCategory.Indexability, Severity.Critical, 10,
                "Sunucu hatası",
                "Sayfa 5xx dönüyor ya da hiç getirilemedi (bağlantı hatası, zaman aşımı). Bu sunucu "
                + "kaynaklı bir arızadır; arama motoru 5xx görünce önce tarama hızını düşürür, hata "
                + "sürerse sayfayı dizinden çıkarır. 4xx'ten daha acildir, çünkü sorun çoğu zaman "
                + "tek sayfada değil altyapının tamamındadır.",
                "Planlı bakım penceresinde alınmış bir ölçüm yanlış pozitif olabilir. Bu durumda "
                + "doğru davranış bulguyu yoksaymak değil, bakım sırasında 500 yerine 503 "
                + "döndürmektir.",
                "1) Sunucu ve uygulama loglarından kök nedeni bul: istisna, veritabanı bağlantısı, "
                + "bellek veya zaman aşımı.\n"
                + "2) Planlı bakımsa 500 yerine 503 dön ve Retry-After başlığı ekle; böylece "
                + "dizinden düşme riski azalır.\n"
                + "3) Yavaş yanıt veren sayfalarda zaman aşımı limitlerini ve sorgu maliyetini "
                + "gözden geçir.\n"
                + "4) Düzeltmeden sonra aynı adresi yeniden tara ve Search Console > Tarama "
                + "İstatistikleri'nde hata oranının düştüğünü doğrula.",
                "https://developers.google.com/search/docs/crawling-indexing/http-network-errors"),

            R("REDIRECT_CHAIN", RuleCategory.Indexability, Severity.Medium, 5,
                "Yönlendirme zinciri",
                "Sayfaya tek atlamada değil, birbirini izleyen birden fazla yönlendirmeyle "
                + "ulaşılıyor (örn. http > https > www > son adres). Her atlama ek gecikme demektir "
                + "ve zincir uzadıkça tarayıcıların takibi bırakma ihtimali artar; tarama bütçesi de "
                + "gereksiz harcanır. Sinyal kaybı tek başına büyük değildir, ama açılış süresi ve "
                + "keşif verimliliği ölçülebilir şekilde düşer.",
                "Alan adı taşıma ve protokol geçişi gibi dönemlerde zincir geçici olarak normaldir. "
                + "Geçiş tamamlandıktan sonra kalıcı hâle gelmişse yoksayma.",
                "1) Zinciri baştan sona izle ve 200 dönen son adresi belirle.\n"
                + "2) İç linkleri, menüyü, sitemap'i ve canonical'ları doğrudan son adrese "
                + "güncelle.\n"
                + "3) Sunucu/CDN kurallarını birleştir: protokol ve www tercihini tek kuralda çöz, "
                + "yol yönlendirmesini ondan sonra uygula.\n"
                + "4) Düzeltmeden sonra adresi tekrar iste ve geriye tek bir 301 kaldığını "
                + "doğrula.",
                "https://developers.google.com/search/docs/crawling-indexing/301-redirects"),

            R("REDIRECT_TARGET_INVALID", RuleCategory.Indexability, Severity.Critical, 9,
                "Yönlendirme hedefi geçersiz",
                "Yönlendirmenin Location başlığı http/https dışı ya da ayrıştırılamayan bir değere "
                + "işaret ediyor; hiçbir istemci hedefe ulaşamaz. Kullanıcı zincirin ortasında "
                + "kalır, arama motoru da adresi ölü kabul eder. Sebep genellikle şablon hatası, "
                + "eksik alan adı ya da javascript:/tel: gibi yanlış şemalı bir değerdir.",
                "Meşru bir istisnası yoktur; her tetiklenmesi gerçek bir hatadır. Yoksamak yerine "
                + "yönlendirmeyi üreten kuralı düzelt.",
                "1) Yönlendirmeyi üreten kuralı bul: sunucu konfigürasyonu, uygulama middleware'i "
                + "veya CMS eklentisi.\n"
                + "2) Location değerini geçerli bir http/https adresine çevir; göreli adres "
                + "kullanacaksan köke göre yaz (/yol).\n"
                + "3) Şablondan gelen boş değişkene karşı koruma ekle — hedef boş ise yönlendirme "
                + "hiç üretilmesin.\n"
                + "4) curl -I ile yanıt başlığını isteyerek hedefin doğru döndüğünü doğrula.",
                "https://developers.google.com/search/docs/crawling-indexing/301-redirects"),

            R("CANONICAL_MISSING", RuleCategory.Indexability, Severity.Low, 3,
                "Canonical yok",
                "Sayfada rel=canonical etiketi tanımlı değil. Canonical, aynı içeriğe birden fazla "
                + "adresten ulaşıldığında (izleme parametreleri, sıralama/filtre, http-https ve www "
                + "farkı) hangisinin asıl kabul edileceğini söyler. Etiket yoksa asıl adresi arama "
                + "motoru kendi seçer ve beklemediğin bir varyant dizine girebilir.",
                "Parametresiz, tek adresli küçük sitelerde etkisi sınırlıdır — bu yüzden önemi "
                + "düşük tutulur ve site genelinde yoksayılabilir. Filtre veya izleme parametresi "
                + "üretilen sitelerde yoksayma.",
                "1) Her sayfaya kendini gösteren bir canonical ekle: "
                + "<link rel=\"canonical\" href=\"https://site.com/yol\">\n"
                + "2) Mutlak URL kullan; protokol, www tercihi ve sondaki slash site genelinde tek "
                + "biçimde olsun.\n"
                + "3) Sayfalanmış listelerde her sayfa kendi adresini göstersin — hepsini ilk "
                + "sayfaya bağlama.\n"
                + "4) Etiketin <head> içinde ve tek adet olduğunu doğrula; ikinci bir canonical "
                + "ikisini birden geçersiz kılabilir.",
                "https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls"),

            R("CANONICAL_POINTS_ELSEWHERE", RuleCategory.Indexability, Severity.Medium, 5,
                "Canonical başkasını gösteriyor",
                "rel=canonical sayfanın kendi adresini değil başka bir adresi gösteriyor. Bu, "
                + "\"beni dizine ekleme, asıl olan şu\" demektir; niyet buysa doğru, değilse sayfa "
                + "sessizce aramadan silinir ve bunu hiçbir hata mesajı haber vermez. En sık sebep "
                + "şablonda sabitlenmiş canonical veya çoğaltılmış sayfa düzenidir.",
                "Yinelenen varyantlarda (utm parametreli, filtreli veya yazdırma adresleri) "
                + "beklenen davranıştır; oralarda yoksayabilirsin. Asıl sürüm olması gereken bir "
                + "sayfada yoksayma.",
                "1) Hedef adresi aç: gerçekten aynı içerik mi, 200 mü dönüyor kontrol et.\n"
                + "2) Bu sayfa asıl sürümse canonical'ı kendi adresine çevir.\n"
                + "3) Asıl sürüm değilse mevcut hâli bırak; sayfaya hiç ihtiyaç yoksa kaldırıp 301 "
                + "ile hedefe yönlendirmeyi değerlendir.\n"
                + "4) Şablon tüm sayfalara aynı canonical'ı basıyorsa değeri sayfa bazına çek.",
                "https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls"),

            R("SITEMAP_MISSING", RuleCategory.Indexability, Severity.Medium, 5,
                "Sitemap yok",
                "Sitede okunabilir bir XML sitemap bulunamadı: robots.txt'te Sitemap satırı yok ve "
                + "bilinen adreslerde geçerli bir dosya yanıt vermiyor. Sitemap zorunlu değildir "
                + "ama yeni ve derindeki sayfaların keşfini hızlandırır, son güncelleme tarihini "
                + "bildirir. Özellikle iç linki zayıf veya çok sayfalı sitelerde fark büyüktür.",
                "Menüden her sayfaya erişilen birkaç sayfalık sitelerde etkisi küçüktür; orada "
                + "yoksayılabilir. Yüzlerce sayfalı veya sık içerik eklenen sitelerde yoksayma.",
                "1) Yalnızca dizine girmesini istediğin, 200 dönen ve canonical'ı kendine bakan "
                + "adresleri içeren bir sitemap.xml üret.\n"
                + "2) 50.000 URL veya 50 MB sınırını aşıyorsan sitemap index dosyası kullan.\n"
                + "3) robots.txt'e mutlak adresle bildir: Sitemap: https://site.com/sitemap.xml\n"
                + "4) Search Console > Site Haritaları ekranından gönder ve okunduğunu doğrula.",
                "https://developers.google.com/search/docs/crawling-indexing/sitemaps/overview"),

            R("PAGE_NOT_IN_SITEMAP", RuleCategory.Indexability, Severity.Low, 3,
                "Sayfa sitemap dışında",
                "Dizinlenebilir bir sayfa sitemap'te listelenmiyor. Sitemap dışındaki sayfa yine "
                + "bulunabilir, ama keşfi tamamen iç linklere kalır; yeni yayınlanan içeriklerde bu "
                + "gecikme günlere yayılabilir. Ayrıca sitemap kapsamı ile gerçek sayfa listesi "
                + "arasındaki fark, Search Console raporlarını okumayı zorlaştırır.",
                "Dizine girmemesi gereken sayfalarda doğru çözüm sitemap'e eklemek değil noindex "
                + "vermektir; noindex verdiysen bu bulguyu yoksayabilirsin.",
                "1) Sayfa dizine girmeliyse sitemap'e ekle ve lastmod değerini gerçek güncelleme "
                + "tarihiyle doldur.\n"
                + "2) Sitemap otomatik üretiliyorsa hangi filtrenin bu adresi elediğini kontrol "
                + "et.\n"
                + "3) Sayfa dizine girmemeliyse noindex ver ve sitemap dışında bırak — iki sinyal "
                + "birbiriyle çelişmesin.\n"
                + "4) Güncellenen sitemap'i yeniden gönder.",
                "https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap"),

            // --- Meta ---
            R("META_TITLE_MISSING", RuleCategory.Meta, Severity.Critical, 9,
                "Title etiketi yok",
                "Sayfada <title> etiketi bulunmuyor. Title, arama sonucundaki başlığın ve tarayıcı "
                + "sekmesinin ana kaynağıdır; en güçlü sayfa içi sinyallerden biridir. Etiket yoksa "
                + "arama motoru başlığı sayfa içinden veya gelen link metinlerinden kendi üretir ve "
                + "sonuç çoğu zaman anlamsız çıkar.",
                "İstisnası yoktur: her HTML sayfasında bir title bulunmalıdır. Yoksaymak yerine "
                + "şablonda yedek bir başlık tanımla.",
                "1) <head> içine benzersiz bir <title> ekle; 30-60 karakter hedefle.\n"
                + "2) Ana anahtar kelimeyi başta kullan, marka adını sona koy "
                + "(örn. \"Kırmızı Kadın Bot Modelleri | Marka\").\n"
                + "3) Şablonda title boş bir değişkene bağlıysa yedek bir değer tanımla.\n"
                + "4) Başlığı yalnızca javascript ile yazıyorsan sunucu tarafında da bas; ilk HTML "
                + "yanıtında bulunması gerekir.",
                "https://developers.google.com/search/docs/appearance/title-link"),

            R("META_TITLE_TOO_SHORT", RuleCategory.Meta, Severity.Medium, 5,
                "Title çok kısa",
                "Title 30 karakterden kısa. Kısa başlıklar sayfanın ne sunduğunu anlatmaya yetmez, "
                + "arama sonucunda tıklama oranını düşürür ve kelime çeşitliliği olmadığı için daha "
                + "az sorguyla eşleşir. Teknik bir hata değil, kaçırılmış bir fırsattır.",
                "Marka ana sayfası gibi tek kelimenin yeterli olduğu yerlerde kabul edilebilir; "
                + "orada yoksayabilirsin. Kategori ve ürün sayfalarında yoksayma.",
                "1) Başlığa sayfayı ayırt eden nitelik ekle: kategori, model, şehir, yıl gibi.\n"
                + "2) 30-60 karakter aralığını hedefle; doldurma kelimesi değil gerçek bilgi "
                + "ekle.\n"
                + "3) Aynı kalıptan üretilen diğer sayfalarla birebir aynı olmadığından emin ol.\n"
                + "4) Anahtar kelime yığmadan, okunabilir tek bir cümle kur.",
                "https://developers.google.com/search/docs/appearance/title-link"),

            R("META_TITLE_TOO_LONG", RuleCategory.Meta, Severity.Medium, 5,
                "Title çok uzun",
                "Title 60 karakterden uzun. Arama sonucunda başlık karakter değil piksel genişliğine "
                + "göre kırpılır; sondaki kelimeler kullanıcıya hiç görünmez ve cümle yarıda kalınca "
                + "güven düşer. Uzunluk bir ceza sebebi değildir, ama görünürlük kaybı gerçekleşir.",
                "Uzun ürün adlarında kırpılma kaçınılmaz olabilir; ilk 60 karakter kendi başına "
                + "anlam taşıyorsa yoksayabilirsin.",
                "1) En önemli bilgiyi ilk 60 karaktere taşı.\n"
                + "2) Marka adını kısalt ya da çıkar; tekrar eden ekleri (\"en iyi\", \"ucuz\", "
                + "yıl bilgisi) temizle.\n"
                + "3) Kategori kalıplarındaki gereksiz sabit önekleri şablondan kaldır.\n"
                + "4) Yeni başlığı arama sonucu önizlemesinde kırpılmadan göründüğü noktaya kadar "
                + "kısalt.",
                "https://developers.google.com/search/docs/appearance/title-link"),

            R("META_TITLE_DUPLICATE", RuleCategory.Meta, Severity.Medium, 5,
                "Yinelenen title",
                "Aynı title birden fazla sayfada kullanılıyor. Arama motoru hangi sayfanın hangi "
                + "sorguya cevap olduğunu ayırt edemez; sayfalar birbirinin yerine geçerek "
                + "görünürlüğü böler ve hiçbiri tam güç kazanamaz. En sık sebep şablondan gelen "
                + "sabit başlık ile sayfalanmış veya filtreli listelerdir.",
                "Sayfalar gerçekten aynı içeriğin varyantıysa çözüm başlık değiştirmek değil "
                + "canonical vermektir; canonical verildiyse yoksayabilirsin.",
                "1) Çakışan sayfaları karşılaştır; gerçekten farklı içeriklerse her birine kendi "
                + "başlığını yaz.\n"
                + "2) Şablonda başlığı ayırt edici bir değişkenle üret: ürün adı, kategori, sayfa "
                + "numarası.\n"
                + "3) Sayfalanmış listelerde başlığa \"- Sayfa 2\" gibi bir ek koy.\n"
                + "4) Sayfalar aynı içeriğin varyantıysa canonical ile asıl sürümü işaret et.",
                "https://developers.google.com/search/docs/appearance/title-link"),

            R("META_DESC_MISSING", RuleCategory.Meta, Severity.High, 6,
                "Meta description yok",
                "Sayfada meta description yok. Bu etiket doğrudan bir sıralama faktörü değildir ama "
                + "arama sonucundaki özet metnini belirler; yoksa metin sayfa içinden seçilir ve "
                + "çoğu zaman menü, çerez uyarısı veya yasal metin parçası öne çıkar. İyi yazılmış "
                + "bir özet, sıralama değişmeden tıklama oranını artırır.",
                "Otomatik üretilmiş binlerce sayfada boş bırakmak, hepsine aynı kötü açıklamayı "
                + "yazmaktan iyidir; o durumda yoksayabilirsin. Önemli açılış sayfalarında "
                + "yoksayma.",
                "1) 120-155 karakter arası, sayfanın vaadini ve bir eylem çağrısını içeren özgün "
                + "bir açıklama yaz.\n"
                + "2) Anahtar kelimeyi doğal biçimde geçir; eşleşen kelimeler sonuçta kalın "
                + "gösterilir ve dikkat çeker.\n"
                + "3) Şablonla üretiyorsan içeriğin ilk cümlesini kopyalamak yerine ayrı bir özet "
                + "alanı kullan.\n"
                + "4) Açıklamayı title ile birebir aynı yapma; ikisi birbirini tamamlasın.",
                "https://developers.google.com/search/docs/appearance/snippet"),

            R("META_DESC_TOO_LONG", RuleCategory.Meta, Severity.Low, 3,
                "Meta description çok uzun",
                "Meta description 160 karakterden uzun. Fazlası arama sonucunda üç noktayla kesilir "
                + "ve sonda kalan eylem çağrısı kullanıcıya hiç görünmez. Uzun açıklama ceza almaz, "
                + "sadece etkisiz kalır.",
                "Görünen sınır dile ve cihaza göre değiştiği için sınırın biraz üzerindeki "
                + "açıklamalar yoksayılabilir. İlk 155 karakter tek başına anlam taşımıyorsa "
                + "yoksayma.",
                "1) En önemli cümleyi başa al ve toplamda 155 karakteri aşma.\n"
                + "2) Tekrar eden marka adı veya slogan kısmını çıkar.\n"
                + "3) Açıklamayı şablon üretiyorsa kırpma işlemini kelime sınırında yap; cümleyi "
                + "ortasından kesme.\n"
                + "4) Sonucu arama sonucu önizlemesinde doğrula.",
                "https://developers.google.com/search/docs/appearance/snippet"),

            R("META_DESC_DUPLICATE", RuleCategory.Meta, Severity.Low, 3,
                "Yinelenen meta description",
                "Aynı meta description birden fazla sayfada kullanılıyor. Aramada yan yana çıkan "
                + "sonuçlar birbirinin aynı görünür ve kullanıcı hangisine gireceğini seçemez; "
                + "açıklamanın tıklama artırıcı işlevi tamamen kaybolur. Genellikle şablondaki "
                + "sabit metinden kaynaklanır.",
                "Sayfalar aynı içeriğin varyantıysa asıl sorun açıklama değil eksik canonical'dır; "
                + "canonical verildiyse yoksayabilirsin.",
                "1) Açıklamayı sayfayı ayırt eden bilgiyle üret: ürün özelliği, kategori, konum.\n"
                + "2) Şablondaki sabit metni değişkenle değiştir.\n"
                + "3) Ayırt edici bilgi üretemiyorsan açıklamayı boş bırak; motorun sayfa içinden "
                + "seçmesi yinelemeden iyidir.\n"
                + "4) Sayfalar aynı içeriğin varyantıysa canonical ile birleştir.",
                "https://developers.google.com/search/docs/appearance/snippet"),

            // --- Content ---
            R("H1_MISSING", RuleCategory.Content, Severity.High, 6,
                "H1 yok",
                "Sayfada H1 başlığı bulunmuyor. H1, içerik için en üst seviye başlıktır; sayfanın "
                + "konusunu hem kullanıcıya hem ekran okuyucuya ilk o bildirir. Yoksa içerik "
                + "hiyerarşisi baştan kopuk olur ve sayfanın ana konusu zayıf sinyallenir.",
                "Tasarım gereği büyük bir başlık istemiyorsan doğru yol H1'i kaldırmak değil CSS "
                + "ile küçültmektir; bu yüzden yoksamak nadiren doğrudur.",
                "1) Sayfanın ana konusunu anlatan tek bir <h1> ekle.\n"
                + "2) H1'i title'ın birebir kopyası yapma; aynı konuyu farklı ifadeyle söyle.\n"
                + "3) Logo veya site adını H1 yapma — H1 sayfaya aittir, siteye değil.\n"
                + "4) Görünüm için CSS ile boyutlandır; display:none ile gizleme.",
                "https://developers.google.com/search/docs/fundamentals/seo-starter-guide"),

            R("H1_MULTIPLE", RuleCategory.Content, Severity.Medium, 4,
                "Birden fazla H1",
                "Sayfada birden çok H1 var. HTML5 bölüm yapısında teknik olarak geçerlidir, ancak "
                + "pratikte sayfanın ana konusu bulanıklaşır ve ekran okuyucuda gezinme zorlaşır. "
                + "En sık sebep, şablonun hem site adını hem sayfa başlığını H1 olarak basmasıdır.",
                "Tek sayfada birbirinden bağımsız birden fazla makale varsa (akış veya arşiv "
                + "düzeni) kabul edilebilir; orada yoksayabilirsin.",
                "1) Sayfanın ana konusunu anlatan tek H1'i seç.\n"
                + "2) Diğerlerini içerik hiyerarşisine göre H2 veya H3 yap.\n"
                + "3) Site adı ya da logo H1 içindeyse şablonda p veya div'e çevir.\n"
                + "4) Başlıkların görsel boyutunu etiket seçerek değil CSS ile ayarla.",
                "https://developers.google.com/search/docs/fundamentals/seo-starter-guide"),

            R("THIN_CONTENT", RuleCategory.Content, Severity.Medium, 5,
                "Zayıf içerik",
                "Sayfa metni 300 kelimenin altında. Kelime sayısı tek başına bir sıralama faktörü "
                + "değildir, ancak bu uzunluk çoğu sorguda kullanıcının sorusunu karşılamaz ve sayfa "
                + "düşük değerli olarak değerlendirilir. Çok sayıda ince sayfa, sitenin genel kalite "
                + "algısını da aşağı çeker.",
                "İletişim, giriş, teşekkür gibi işlevsel sayfalarda kısa metin normaldir; bunlarda "
                + "kuralı site genelinde yoksaymak doğrudur. Arama trafiği hedefleyen içeriklerde "
                + "yoksayma.",
                "1) Sayfanın hedeflediği soruyu belirle ve cevabı eksiksiz ver: kapsam, örnek, sık "
                + "sorulanlar.\n"
                + "2) Başka sayfalardan kopyalanmış metin yerine özgün içerik üret; uzunluk tek "
                + "başına yeterli değildir.\n"
                + "3) Aynı konuyu bölen çok sayıda ince sayfayı tek güçlü sayfada birleştir, "
                + "eskilerini 301 ile yönlendir.\n"
                + "4) Sayfa doğası gereği kısaysa kuralı site genelinde yoksay.",
                "https://developers.google.com/search/docs/essentials/creating-helpful-content"),

            R("DUPLICATE_CONTENT", RuleCategory.Content, Severity.High, 7,
                "Yinelenen içerik",
                "Aynı içerik parmak izi birden fazla adreste görülüyor. Arama motoru bunlardan "
                + "yalnızca birini seçer; seçim senin istediğin adres olmayabilir ve gelen linklerin "
                + "değeri varyantlar arasında bölünür. Çoğunlukla parametreli adresler, http-https "
                + "ve www varyantları, yazdırma sürümleri veya kopyalanmış kategori sayfaları sebep "
                + "olur.",
                "Şablonu aynı ama gövdesi gerçekten farklı sayfalarda yanlış pozitif olabilir; "
                + "gövdeyi karşılaştırıp öyleyse yoksay.",
                "1) Asıl sürümü belirle ve diğer adreslerden ona canonical ver.\n"
                + "2) Adres varyantlarını normalize et: parametre sıralaması, sondaki slash, "
                + "büyük-küçük harf tek biçime insin.\n"
                + "3) Gerçekten gereksiz kopyaları kaldır ve 301 ile asıl adrese yönlendir.\n"
                + "4) Sayfalar farklı amaca hizmet ediyorsa içeriği gerçekten farklılaştır; şablon "
                + "değil gövde metni değişsin.",
                "https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls"),

            R("HEADING_HIERARCHY_BROKEN", RuleCategory.Content, Severity.Low, 3,
                "Başlık hiyerarşisi bozuk",
                "Başlık seviyeleri sırayla ilerlemiyor; en az bir seviye atlanmış (örn. h2'den sonra "
                + "h4). Ekran okuyucu kullanıcıları içerikte başlık seviyelerine göre gezinir; "
                + "atlama olunca yapının bir parçası eksikmiş gibi algılanır. Neredeyse her zaman "
                + "başlık etiketinin anlam yerine yazı tipi boyutu için seçilmesinden kaynaklanır.",
                "Arama motorları açısından etkisi küçüktür, asıl maliyet erişilebilirliktedir. "
                + "Erişilebilirlik önceliğin değilse düşük öncelikle ele alabilir ya da "
                + "yoksayabilirsin.",
                "1) Başlıkları anlam sırasına göre düzenle: h1 > h2 > h3, seviye atlamadan.\n"
                + "2) Boyut için etiket değiştirme; CSS sınıfı kullan.\n"
                + "3) Editörlerin yanlış seviye seçmesini önlemek için içerik şablonunda "
                + "kullanılabilir seviyeleri sınırla.\n"
                + "4) Tarayıcının erişilebilirlik denetimiyle başlık ağacını kontrol et.",
                "https://www.w3.org/WAI/tutorials/page-structure/headings/"),

            // --- Links ---
            R("BROKEN_INTERNAL_LINK", RuleCategory.Links, Severity.High, 6,
                "Kırık iç link",
                "Bir iç link 4xx/5xx dönen sayfaya gidiyor. Kullanıcı akışın ortasında hata "
                + "sayfasına düşer; tarama açısından da boşa harcanan bir istek olur ve linkle "
                + "aktarılacak değer kaybolur. Kırık iç link çoğunlukla yeniden yapılandırma, "
                + "silinen ürün veya elle yazılmış yanlış adresten kaynaklanır.",
                "Kimlik doğrulama arkasındaki sayfalar tarayıcıya 401/403 döndüğü için yanlış "
                + "pozitif üretebilir; bunları yoksayabilirsin. Herkese açık hedeflerde yoksayma.",
                "1) Kaynak sayfadaki linki aç ve hedefin gerçek durum kodunu doğrula.\n"
                + "2) Hedef taşındıysa linki yeni adresle değiştir; yönlendirmeye güvenip bırakma.\n"
                + "3) Hedef kalıcı olarak yoksa linki kaldır ya da ilgili başka bir sayfaya "
                + "yönlendir.\n"
                + "4) Menü, altbilgi gibi site genelinde tekrar eden linklerde şablonu düzelt — tek "
                + "düzeltme tüm sayfaları toparlar.",
                "https://developers.google.com/search/docs/crawling-indexing/links-crawlable"),

            R("ORPHAN_PAGE", RuleCategory.Links, Severity.Medium, 4,
                "Öksüz sayfa",
                "Sayfaya hiçbir iç link işaret etmiyor. İç link olmadan sayfa yalnızca sitemap veya "
                + "dış linklerle bulunabilir; keşfi yavaşlar ve site içindeki önem sinyali sıfıra "
                + "yakın kalır. Kullanıcı da menüden ya da içerik içinden bu sayfaya ulaşamaz.",
                "Kampanya açılış sayfaları gibi bilinçli olarak menü dışında tutulan adreslerde "
                + "beklenen durumdur; o URL için yoksayabilirsin.",
                "1) Sayfayı konu olarak en yakın içeriklerden açıklayıcı anchor metniyle linkle.\n"
                + "2) Kalıcı değerdeyse menü, kategori listesi veya \"ilgili içerik\" bloğuna "
                + "ekle.\n"
                + "3) Bilinçli olarak gizli tutuluyorsa kuralı o URL için yoksay.\n"
                + "4) Sayfanın artık değeri yoksa kaldır ve 301 ile ilgili sayfaya yönlendir.",
                "https://developers.google.com/search/docs/crawling-indexing/links-crawlable"),

            R("TOO_DEEP", RuleCategory.Links, Severity.Low, 3,
                "Çok derin sayfa",
                "Sayfa kök sayfadan 4 tıklamadan uzakta. Derin sayfalar hem kullanıcı hem tarayıcı "
                + "tarafından daha az ziyaret edilir; büyük sitelerde tarama bütçesi üst seviyelerde "
                + "tükenir ve derindeki içerik geç güncellenir. Derinlik doğrudan bir ceza değildir, "
                + "ama önemli sayfaların derinde kalması görünürlük kaybıdır.",
                "Arşiv, eski sayfalama ve düşük öncelikli listelerde derinlik doğaldır; orada "
                + "yoksayabilirsin. Dönüşüm getiren sayfalarda yoksayma.",
                "1) Sayfanın gerçekten önemli olup olmadığına karar ver; önemliyse üst seviyeden "
                + "link ver.\n"
                + "2) Kategori/etiket sayfalarından veya \"ilgili içerik\" bloklarından kısayol "
                + "linkleri ekle.\n"
                + "3) Uzun sayfalama zincirlerini filtre veya kategori kırılımıyla kısalt.\n"
                + "4) Menüyü şişirmeden, konu kümelerini tek bir hub sayfasında topla.",
                "https://developers.google.com/search/docs/fundamentals/seo-starter-guide"),

            R("GENERIC_ANCHOR_TEXT", RuleCategory.Links, Severity.Low, 3,
                "Açıklayıcı olmayan anchor",
                "İç linklerde \"buraya tıklayın\", \"devamını oku\", \"detaylar\" gibi hedefi "
                + "anlatmayan anchor metinleri var. Anchor metni, hedef sayfanın konusunu bildiren "
                + "en güçlü iç sinyallerden biridir; genel ifadeler bu bilgiyi hiç taşımaz. Ekran "
                + "okuyucu kullanıcıları link listesinde yalnızca anchor metnini duyar, hedefsiz "
                + "metinler gezinmeyi zorlaştırır.",
                "Kart ve görsel düzenlerinde kısa metin gerekiyorsa aria-label ile telafi "
                + "edilebilir; aria-label eklediysen yoksayabilirsin.",
                "1) Anchor metnini hedefin konusuyla değiştir: \"devamını oku\" yerine \"iade "
                + "süreci nasıl işler\".\n"
                + "2) Aynı sayfaya giden linklerde birebir aynı metni tekrarlamak zorunda "
                + "değilsin; doğal çeşitlilik iyidir.\n"
                + "3) Tasarım kısa metin dayatıyorsa aria-label veya görsel olarak gizli ek metin "
                + "kullan.\n"
                + "4) Anahtar kelimeyi zorlama; cümle içinde doğal duran ifadeyi seç.",
                "https://developers.google.com/search/docs/crawling-indexing/links-crawlable"),

            // --- Images ---
            R("IMAGE_MISSING_ALT", RuleCategory.Images, Severity.Low, 3,
                "Alt metni eksik görseller",
                "Sayfada alt niteliği taşımayan <img> etiketleri var. Alt metni, görsel "
                + "yüklenmediğinde gösterilen ve ekran okuyucunun sesli okuduğu metindir; ayrıca "
                + "Görsel Arama'da sayfanın bulunmasını sağlar. Eksik alt, erişilebilirlik açısından "
                + "doğrudan bir engeldir.",
                "Dekoratif görsellerde doğru çözüm alt=\"\" yazmaktır — niteliği tamamen kaldırmak "
                + "değil. Boş alt kullandıysan yoksayabilirsin.",
                "1) Anlam taşıyan her görsele, görseli göremeyen birine ne anlatırsan onu yaz.\n"
                + "2) \"resim\", \"foto\" gibi dolgu kelimelerinden ve anahtar kelime yığmaktan "
                + "kaçın.\n"
                + "3) Dekoratif görsellerde alt=\"\" kullan; böylece ekran okuyucu atlar.\n"
                + "4) İçerik yönetim sisteminde görsel yüklerken alt alanını zorunlu hâle getir.",
                "https://developers.google.com/search/docs/appearance/google-images"),

            R("IMAGE_TOO_LARGE", RuleCategory.Images, Severity.Medium, 4,
                "Büyük görsel",
                "Sayfadaki bir veya daha fazla görsel 200 KB'ı aşıyor. Büyük görseller çoğu sayfada "
                + "en geç yüklenen parçadır ve LCP ölçümünü doğrudan belirler; mobil bağlantıda fark "
                + "saniyelerle ölçülür. Ayrıca kullanıcının veri kotasını gereksiz harcar.",
                "Yüksek çözünürlüğün ürünün kendisi olduğu galeri ve portfolyo sayfalarında büyük "
                + "dosyalar kabul edilebilir. Liste ve kapak görsellerinde yoksayma.",
                "1) Görselleri WebP veya AVIF olarak yeniden üret; JPEG'e göre genellikle %30-50 "
                + "küçük olur.\n"
                + "2) Gerçek gösterim boyutunda sun; 3000 piksel genişliğindeki dosyayı 400 "
                + "piksellik alana koyma.\n"
                + "3) srcset/sizes ile cihaza göre farklı boyut sun; ilk ekranın dışındakilere "
                + "loading=\"lazy\" ver.\n"
                + "4) İlk ekranda görünen görseli lazy yapma; tersine fetchpriority=\"high\" ile "
                + "önceliklendir.",
                "https://web.dev/articles/serve-responsive-images"),

            // --- Structured data & i18n ---
            R("SCHEMA_MISSING", RuleCategory.StructuredData, Severity.Low, 3,
                "Yapısal veri yok",
                "Sayfada schema.org işaretlemesi bulunamadı. Yapısal veri sıralamayı doğrudan "
                + "değiştirmez, ama arama sonucunda yıldız, fiyat, sık sorulanlar, tarif gibi zengin "
                + "gösterimleri mümkün kılar; bu da tıklama oranını artırır. Ayrıca sayfanın ne "
                + "hakkında olduğunu makineye net söyler.",
                "Hiçbir zengin sonuç tipine uymayan sade bilgi sayfalarında eksikliği sorun "
                + "değildir; orada yoksayabilirsin. Ürün, tarif, etkinlik ve SSS sayfalarında "
                + "yoksayma.",
                "1) Sayfanın türüne uygun tipi seç: Article, Product, FAQPage, LocalBusiness, "
                + "BreadcrumbList.\n"
                + "2) JSON-LD olarak <script type=\"application/ld+json\"> içinde ekle; mikro veri "
                + "yerine JSON-LD tercih et.\n"
                + "3) Yalnızca sayfada gerçekten görünen bilgiyi işaretle; görünmeyen veri politika "
                + "ihlalidir.\n"
                + "4) Zengin Sonuç Testi ile doğrula ve Search Console'daki gelişmeler raporunu "
                + "izle.",
                "https://developers.google.com/search/docs/appearance/structured-data/intro-structured-data"),

            R("INVALID_STRUCTURED_DATA", RuleCategory.StructuredData, Severity.Medium, 4,
                "Yapısal veri geçersiz",
                "Sayfada JSON-LD var ama biçimi bozuk: geçersiz JSON ya da tek script etiketi içinde "
                + "birden fazla kök nesne. Bu durumda işaretlemenin tamamı yok sayılır, yani emek "
                + "harcanmış ama hiçbir zengin sonuç kazancı oluşmaz. En sık sebepler kaçışı "
                + "yapılmamış tırnak, sondaki fazla virgül ve şablonun yan yana bastığı iki "
                + "nesnedir.",
                "Biçimsel bir hata olduğu için meşru istisnası yoktur. Yoksamak, çalışmayan "
                + "işaretlemeyi sayfada bırakmak demektir.",
                "1) Script içeriğini bir JSON doğrulayıcıdan geçir; sondaki virgül ve kaçışsız "
                + "tırnakları düzelt.\n"
                + "2) Her kök nesneyi kendi <script type=\"application/ld+json\"> etiketine koy, ya "
                + "da hepsini tek bir dizi ([ ... ]) içinde topla.\n"
                + "3) Şablondan gelen metin değerlerini JSON kaçışıyla yaz (tırnak, yeni satır, "
                + "ters bölü).\n"
                + "4) Zengin Sonuç Testi ile sayfayı tekrar tara ve hata kalmadığını doğrula.",
                "https://developers.google.com/search/docs/appearance/structured-data/intro-structured-data"),

            R("OG_TAGS_MISSING", RuleCategory.StructuredData, Severity.Low, 3,
                "Open Graph etiketleri eksik",
                "og:title, og:description veya og:image tanımlı değil. Open Graph etiketleri, link "
                + "sosyal ağlarda ve mesajlaşma uygulamalarında paylaşıldığında görünen kartın "
                + "içeriğini belirler. Eksikse başlık ve görsel rastgele seçilir ya da kart tamamen "
                + "boş görünür; bu da paylaşımdan gelen tıklamayı düşürür.",
                "Arama sıralamasına doğrudan etkisi yoktur; paylaşım beklenmeyen iç sayfalarda "
                + "yoksayabilirsin. Blog ve ürün sayfalarında yoksayma.",
                "1) <head> içine og:title, og:description, og:url ve og:image ekle.\n"
                + "2) og:image için en az 1200x630 piksel, mutlak URL'li bir görsel kullan.\n"
                + "3) og:type değerini içerik türüne göre ver: website, article, product.\n"
                + "4) Geniş kart için twitter:card=summary_large_image ekle ve paylaşım önizleme "
                + "aracıyla doğrula.",
                "https://ogp.me/"),

            R("LANG_ATTR_MISSING", RuleCategory.I18n, Severity.Medium, 4,
                "lang niteliği yok",
                "<html> etiketinde lang niteliği bulunmuyor. Bu nitelik ekran okuyuculara hangi "
                + "dilde okuyacağını, tarayıcılara çeviri önerip önermeyeceğini söyler. Eksikse "
                + "ekran okuyucu yanlış telaffuzla okur; erişilebilirlik açısından somut bir "
                + "sorundur.",
                "Arama motoru dili içerikten de çıkarabildiği için sıralama etkisi sınırlıdır, ama "
                + "düzeltmesi tek satırdır — yoksamak için iyi bir sebep nadiren bulunur.",
                "1) <html lang=\"tr\"> şeklinde sayfanın ana dilini bildir.\n"
                + "2) Çok dilli sitede her sürüm kendi dil kodunu taşısın; değeri şablonda "
                + "sabitleme.\n"
                + "3) Sayfa içinde başka dilde bir blok varsa o elemana kendi lang niteliğini "
                + "ver.\n"
                + "4) Dil sürümleri arasında hreflang bağlantılarını da ekle.",
                "https://www.w3.org/International/questions/qa-html-language-declarations"),

            // --- Performance (PSI) ---
            R("LCP_POOR", RuleCategory.Performance, Severity.High, 7,
                "Kötü LCP",
                "Largest Contentful Paint 4 saniyenin üzerinde. LCP, ekrandaki en büyük içerik "
                + "ögesinin (genellikle kapak görseli veya başlık bloğu) görünür olma süresidir ve "
                + "\"sayfa açıldı mı\" hissinin ana ölçüsüdür. 2,5 sn altı iyi, 4 sn üstü kötü kabul "
                + "edilir.",
                "Ölçüm PageSpeed Insights'ın tek seferlik laboratuvar koşusundan gelir; ağ "
                + "dalgalanması payı vardır. Tek bir ölçüme dayanıp yoksamak yerine ölçümü "
                + "tekrarla.",
                "1) PageSpeed raporunda LCP ögesini belirle — çoğunlukla ilk ekrandaki büyük "
                + "görseldir.\n"
                + "2) O görseli önceliklendir: fetchpriority=\"high\" ve preload kullan, lazy "
                + "yükleme uygulama.\n"
                + "3) Render engelleyen CSS/JS'i azalt: kritik CSS'i satır içi ver, kalanını "
                + "ertele.\n"
                + "4) Sunucu yanıt süresini (TTFB) düşür: önbellek, CDN ve sorgu optimizasyonu.\n"
                + "5) Görseli modern formatta ve gerçek gösterim boyutunda sun.",
                "https://web.dev/articles/lcp"),

            R("CLS_POOR", RuleCategory.Performance, Severity.Medium, 5,
                "Kötü CLS",
                "Cumulative Layout Shift 0,25'in üzerinde. CLS, sayfa yüklenirken içeriğin ne kadar "
                + "zıpladığını ölçer; kullanıcı tam tıklayacakken butonun kayması bu metriğe "
                + "yansır. 0,1 altı iyi, 0,25 üstü kötü kabul edilir. En sık sebepler boyutu "
                + "bildirilmemiş görseller, geç gelen reklam ve banner alanları ile sonradan "
                + "yüklenen yazı tipleridir.",
                "Ölçüm tek seferlik laboratuvar koşusudur; çerez bandı gibi tek seferlik ögeler "
                + "sonucu şişirebilir. Gerçek kullanıcı verisiyle karşılaştırmadan yoksayma.",
                "1) Tüm görsel ve video etiketlerine width/height ver ya da CSS aspect-ratio "
                + "kullan.\n"
                + "2) Reklam, gömülü içerik ve bildirim şeritleri için önceden yer tutucu alan "
                + "ayır.\n"
                + "3) Yazı tipi değişiminde kaymayı azaltmak için font-display değerini ayarla ve "
                + "yedek fontu metrik olarak yakın seç.\n"
                + "4) Mevcut içeriğin üstüne sonradan eleman ekleme; yeni içeriği kullanıcı "
                + "etkileşimi dışında araya sokma.",
                "https://web.dev/articles/cls"),

            R("INP_POOR", RuleCategory.Performance, Severity.Medium, 5,
                "Kötü INP",
                "Interaction to Next Paint 500 ms'nin üzerinde. INP, kullanıcının tıkladıktan veya "
                + "yazdıktan sonra ekranda görsel bir karşılık görmesi için geçen süreyi ölçer; "
                + "200 ms altı iyi, 500 ms üstü kötü kabul edilir. Yüksek INP genellikle ana iş "
                + "parçacığını uzun süre meşgul eden JavaScript'ten kaynaklanır.",
                "Neredeyse hiç etkileşim içermeyen tanıtım sayfalarında ölçümün güvenilirliği "
                + "düşüktür; orada yoksayabilirsin. Form ve filtre içeren sayfalarda yoksayma.",
                "1) 50 ms'yi aşan uzun görevleri parçalara böl; aralarda ana iş parçacığını serbest "
                + "bırak.\n"
                + "2) Etkileşim anında ağır hesaplama yapma; sonucu önceden hesapla veya web "
                + "worker'a taşı.\n"
                + "3) Kullanılmayan üçüncü taraf betiklerini kaldır, kalanları ertele ya da "
                + "asenkron yükle.\n"
                + "4) Tıklamadan hemen sonra görsel geri bildirim ver (durum değişimi), ağır işi "
                + "ondan sonra çalıştır.",
                "https://web.dev/articles/inp")
        );
    }

    private static Rule R(string code, RuleCategory category, Severity severity, int weight,
        string title, string description, string whenToIgnore, string howToFix,
        string? docUrl = null) => new()
    {
        Code = code,
        Category = category,
        Severity = severity,
        Weight = weight,
        TitleTr = title,
        DescriptionTr = description,
        WhenToIgnoreTr = whenToIgnore,
        HowToFixTr = howToFix,
        DocUrl = docUrl,
        IsActive = true
    };
}
