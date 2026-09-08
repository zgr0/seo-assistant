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
/// </remarks>
internal sealed class RuleSeed : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> b)
    {
        b.HasData(
            // --- Indexability ---
            R("ROBOTS_NOINDEX", RuleCategory.Indexability, Severity.Critical, 9,
                "noindex etiketi",
                "Sayfanin robots meta etiketinde ya da X-Robots-Tag yanit basliginda noindex var. "
                + "Bu, arama motorlarina \"bu sayfayi dizine ekleme\" demektir; sayfa taransa bile "
                + "sonuclarda hic gorunmez ve aldigi ic linklerin degeri bir yere aktarilmaz. "
                + "En sik sebep, test ortamindan canliya tasinan tema veya CMS ayaridir.",
                "Sepet, hesap, odeme ve tesekkur sayfalarinda noindex bilincli ve dogru bir "
                + "tercihtir. Trafik beklenen bir icerik sayfasinda ise yoksayma; bu, en agir "
                + "hatalardan biridir.",
                "1) Sayfa kaynagindaki <meta name=\"robots\"> etiketini ve HTTP yanitindaki "
                + "X-Robots-Tag basligini birlikte kontrol et; noindex ikisinden birinde olabilir.\n"
                + "2) Sayfa dizine girmeliyse degeri index,follow yap veya etiketi tamamen kaldir.\n"
                + "3) CMS/tema ayarlarinda \"arama motorlarini engelle\" secenegi aciksa kapat.\n"
                + "4) Duzeltmeden sonra Search Console > URL Denetimi ile sayfayi yeniden tara.",
                "https://developers.google.com/search/docs/crawling-indexing/block-indexing"),

            R("BLOCKED_BY_ROBOTS_TXT", RuleCategory.Indexability, Severity.High, 7,
                "robots.txt engeli",
                "robots.txt icindeki bir Disallow kurali, taranmasi gereken ic adresleri kapatiyor. "
                + "Engellenen adresi arama motoru indiremez; icerigi okunamadigi icin sayfa ya hic "
                + "dizine girmez ya da basliksiz-ozetsiz bos bir kayit olarak listelenir. Dikkat: "
                + "robots.txt engeli noindex ile ayni sey degildir — engellenen sayfa dis linkler "
                + "uzerinden yine de dizine dusebilir.",
                "Yonetim paneli, ic arama sonuclari ve tekrar eden filtre adresleri icin engel "
                + "dogrudur; bu adreslerde bulguyu yoksayabilirsin. Icerik sayfalarinda ise "
                + "dogrudan trafik kaybi demektir.",
                "1) robots.txt dosyasini ac ve engellenen yolu hangi Disallow satirinin "
                + "yakaladigini bul.\n"
                + "2) Kurali daralt: tum dizini kapatmak yerine yalnizca gizlenmesi gereken yolu "
                + "yaz (orn. Disallow: /admin/ yerine Disallow: /admin/logs/).\n"
                + "3) Amacin taramayi degil dizine girmeyi engellemekse robots.txt yerine noindex "
                + "kullan; ikisini ayni sayfada birlikte kullanma — engellenen sayfada noindex "
                + "hic okunamaz.\n"
                + "4) Search Console'un robots.txt raporuyla dogrula, sonra URL Denetimi'nden "
                + "yeniden tara.",
                "https://developers.google.com/search/docs/crawling-indexing/robots/intro"),

            R("BROKEN_PAGE_4XX", RuleCategory.Indexability, Severity.Critical, 10,
                "Sayfa 4xx donuyor",
                "Sayfa 4xx (cogunlukla 404 veya 410) donuyor; icerik sunucuda yok. Kullanici hata "
                + "sayfasina duser, arama motoru da adresi dizinden cikarir. Sayfaya ic link veya "
                + "dis backlink geliyorsa o baglantilarin biriktirdigi deger de bosa gider.",
                "Silinmis kampanya adresleri icin 410 bilincli bir tercih olabilir. Ancak adres "
                + "hala bir yerden linkleniyorsa yoksayma — once linki temizlemek gerekir.",
                "1) Adres kalici olarak kaldirildiysa en yakin ilgili sayfaya 301 yonlendirme koy; "
                + "her seyi ana sayfaya yonlendirme — alakasiz hedef yumusak 404 sayilir.\n"
                + "2) Adres yanlislikla kirildiysa icerigi geri getir veya dogru URL'e duzelt.\n"
                + "3) Bu sayfaya isaret eden ic linkleri yeni adresle degistir; yonlendirme kalici "
                + "cozum degil, gecis koprusudur.\n"
                + "4) Icerik gercekten donmeyecekse 410 don ve adresi sitemap'ten cikar.",
                "https://developers.google.com/search/docs/crawling-indexing/http-network-errors"),

            R("SERVER_ERROR_5XX", RuleCategory.Indexability, Severity.Critical, 10,
                "Sunucu hatasi",
                "Sayfa 5xx donuyor ya da hic getirilemedi (baglanti hatasi, zaman asimi). Bu sunucu "
                + "kaynakli bir arizadir; arama motoru 5xx gorunce once tarama hizini dusurur, hata "
                + "surerse sayfayi dizinden cikarir. 4xx'ten daha acildir, cunku sorun cogu zaman "
                + "tek sayfada degil altyapinin tamaminda olur.",
                "Planli bakim penceresinde alinmis bir olcum yanlis pozitif olabilir. Bu durumda "
                + "dogru davranis bulguyu yoksaymak degil, bakim sirasinda 500 yerine 503 "
                + "donmektir.",
                "1) Sunucu ve uygulama loglarindan kok nedeni bul: istisna, veritabani baglantisi, "
                + "bellek veya zaman asimi.\n"
                + "2) Planli bakimsa 500 yerine 503 don ve Retry-After basligi ekle; boylece "
                + "dizinden dusme riski azalir.\n"
                + "3) Yavas yanit veren sayfalarda zaman asimi limitlerini ve sorgu maliyetini "
                + "gozden gecir.\n"
                + "4) Duzeltmeden sonra ayni adresi yeniden tara ve Search Console > Tarama "
                + "Istatistikleri'nde hata oraninin dustugunu dogrula.",
                "https://developers.google.com/search/docs/crawling-indexing/http-network-errors"),

            R("REDIRECT_CHAIN", RuleCategory.Indexability, Severity.Medium, 5,
                "Yonlendirme zinciri",
                "Sayfaya tek atlamada degil, birbirini izleyen birden fazla yonlendirmeyle "
                + "ulasiliyor (orn. http > https > www > son adres). Her atlama ek gecikme demektir "
                + "ve zincir uzadikca tarayicilarin takibi birakma ihtimali artar; tarama butcesi de "
                + "gereksiz harcanir. Sinyal kaybi tek basina buyuk degildir, ama acilis suresi ve "
                + "kesif verimliligi olculebilir sekilde duser.",
                "Alan adi tasima ve protokol gecisi gibi donemlerde zincir gecici olarak normaldir. "
                + "Gecis tamamlandiktan sonra kalici hale gelmisse yoksayma.",
                "1) Zinciri bastan sona izle ve 200 donen son adresi belirle.\n"
                + "2) Ic linkleri, menuyu, sitemap'i ve canonical'lari dogrudan son adrese "
                + "guncelle.\n"
                + "3) Sunucu/CDN kurallarini birlestir: protokol ve www tercihini tek kuralda coz, "
                + "yol yonlendirmesini ondan sonra uygula.\n"
                + "4) Duzeltmeden sonra adresi tekrar iste ve geriye tek bir 301 kaldigini "
                + "dogrula.",
                "https://developers.google.com/search/docs/crawling-indexing/301-redirects"),

            R("REDIRECT_TARGET_INVALID", RuleCategory.Indexability, Severity.Critical, 9,
                "Yonlendirme hedefi gecersiz",
                "Yonlendirmenin Location basligi http/https disi ya da ayristirilamayan bir degere "
                + "isaret ediyor; hicbir istemci hedefe ulasamaz. Kullanici zincirin ortasinda "
                + "kalir, arama motoru da adresi olu kabul eder. Sebep genellikle sablon hatasi, "
                + "eksik alan adi ya da javascript:/tel: gibi yanlis semali bir degerdir.",
                "Mesru bir istisnasi yoktur; her tetiklenmesi gercek bir hatadir. Yoksamak yerine "
                + "yonlendirmeyi ureten kurali duzelt.",
                "1) Yonlendirmeyi ureten kurali bul: sunucu konfigurasyonu, uygulama middleware'i "
                + "veya CMS eklentisi.\n"
                + "2) Location degerini gecerli bir http/https adresine cevir; goreli adres "
                + "kullanacaksan koke gore yaz (/yol).\n"
                + "3) Sablondan gelen bos degiskene karsi koruma ekle — hedef bos ise yonlendirme "
                + "hic uretilmesin.\n"
                + "4) curl -I ile yanit basligini isteyerek hedefin dogru dondugunu dogrula.",
                "https://developers.google.com/search/docs/crawling-indexing/301-redirects"),

            R("CANONICAL_MISSING", RuleCategory.Indexability, Severity.Low, 3,
                "Canonical yok",
                "Sayfada rel=canonical etiketi tanimli degil. Canonical, ayni icerige birden fazla "
                + "adresten ulasildiginda (izleme parametreleri, siralama/filtre, http-https ve www "
                + "farki) hangisinin asil kabul edilecegini soyler. Etiket yoksa asil adresi arama "
                + "motoru kendi secer ve beklemedigin bir varyant dizine girebilir.",
                "Parametresiz, tek adresli kucuk sitelerde etkisi sinirlidir — bu yuzden onemi "
                + "dusuk tutulur ve site genelinde yoksayilabilir. Filtre veya izleme parametresi "
                + "uretilen sitelerde yoksayma.",
                "1) Her sayfaya kendini gosteren bir canonical ekle: "
                + "<link rel=\"canonical\" href=\"https://site.com/yol\">\n"
                + "2) Mutlak URL kullan; protokol, www tercihi ve sondaki slash site genelinde tek "
                + "bicimde olsun.\n"
                + "3) Sayfalanmis listelerde her sayfa kendi adresini gostersin — hepsini ilk "
                + "sayfaya baglama.\n"
                + "4) Etiketin <head> icinde ve tek adet oldugunu dogrula; ikinci bir canonical "
                + "ikisini birden gecersiz kilabilir.",
                "https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls"),

            R("CANONICAL_POINTS_ELSEWHERE", RuleCategory.Indexability, Severity.Medium, 5,
                "Canonical baskasini gosteriyor",
                "rel=canonical sayfanin kendi adresini degil baska bir adresi gosteriyor. Bu, "
                + "\"beni dizine ekleme, asil olan su\" demektir; niyet buysa dogru, degilse sayfa "
                + "sessizce aramadan silinir ve bunu hicbir hata mesaji haber vermez. En sik sebep "
                + "sablonda sabitlenmis canonical veya cogaltilmis sayfa duzenidir.",
                "Yinelenen varyantlarda (utm parametreli, filtreli veya yazdirma adresleri) "
                + "beklenen davranistir; oralarda yoksayabilirsin. Asil surum olmasi gereken bir "
                + "sayfada yoksayma.",
                "1) Hedef adresi ac: gercekten ayni icerik mi, 200 mu donuyor kontrol et.\n"
                + "2) Bu sayfa asil surumse canonical'i kendi adresine cevir.\n"
                + "3) Asil surum degilse mevcut hali birak; sayfaya hic ihtiyac yoksa kaldirip 301 "
                + "ile hedefe yonlendirmeyi degerlendir.\n"
                + "4) Sablon tum sayfalara ayni canonical'i basiyorsa degeri sayfa bazina cek.",
                "https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls"),

            R("SITEMAP_MISSING", RuleCategory.Indexability, Severity.Medium, 5,
                "Sitemap yok",
                "Sitede okunabilir bir XML sitemap bulunamadi: robots.txt'te Sitemap satiri yok ve "
                + "bilinen adreslerde gecerli bir dosya yanit vermiyor. Sitemap zorunlu degildir "
                + "ama yeni ve derindeki sayfalarin kesfini hizlandirir, son guncelleme tarihini "
                + "bildirir. Ozellikle ic linki zayif veya cok sayfali sitelerde fark buyuktur.",
                "Menuden her sayfaya erisilen birkac sayfalik sitelerde etkisi kucuktur; orada "
                + "yoksayilabilir. Yuzlerce sayfali veya sik icerik eklenen sitelerde yoksayma.",
                "1) Yalnizca dizine girmesini istedigin, 200 donen ve canonical'i kendine bakan "
                + "adresleri iceren bir sitemap.xml uret.\n"
                + "2) 50.000 URL veya 50 MB sinirini asiyorsan sitemap index dosyasi kullan.\n"
                + "3) robots.txt'e mutlak adresle bildir: Sitemap: https://site.com/sitemap.xml\n"
                + "4) Search Console > Site Haritalari ekranindan gonder ve okundugunu dogrula.",
                "https://developers.google.com/search/docs/crawling-indexing/sitemaps/overview"),

            R("PAGE_NOT_IN_SITEMAP", RuleCategory.Indexability, Severity.Low, 3,
                "Sayfa sitemap disinda",
                "Dizinlenebilir bir sayfa sitemap'te listelenmiyor. Sitemap disindaki sayfa yine "
                + "bulunabilir, ama kesfi tamamen ic linklere kalir; yeni yayinlanan iceriklerde bu "
                + "gecikme gunlere yayilabilir. Ayrica sitemap kapsami ile gercek sayfa listesi "
                + "arasindaki fark, Search Console raporlarini okumayi zorlastirir.",
                "Dizine girmemesi gereken sayfalarda dogru cozum sitemap'e eklemek degil noindex "
                + "vermektir; noindex verdiysen bu bulguyu yoksayabilirsin.",
                "1) Sayfa dizine girmeliyse sitemap'e ekle ve lastmod degerini gercek guncelleme "
                + "tarihiyle doldur.\n"
                + "2) Sitemap otomatik uretiliyorsa hangi filtrenin bu adresi eledigini kontrol "
                + "et.\n"
                + "3) Sayfa dizine girmemeliyse noindex ver ve sitemap disinda birak — iki sinyal "
                + "birbiriyle celismesin.\n"
                + "4) Guncellenen sitemap'i yeniden gonder.",
                "https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap"),

            // --- Meta ---
            R("META_TITLE_MISSING", RuleCategory.Meta, Severity.Critical, 9,
                "Title etiketi yok",
                "Sayfada <title> etiketi bulunmuyor. Title, arama sonucundaki basligin ve tarayici "
                + "sekmesinin ana kaynagidir; en guclu sayfa ici sinyallerden biridir. Etiket yoksa "
                + "arama motoru basligi sayfa icinden veya gelen link metinlerinden kendi uretir ve "
                + "sonuc cogu zaman anlamsiz cikar.",
                "Istisnasi yoktur: her HTML sayfasinda bir title bulunmalidir. Yoksaymak yerine "
                + "sablonda yedek bir baslik tanimla.",
                "1) <head> icine benzersiz bir <title> ekle; 30-60 karakter hedefle.\n"
                + "2) Ana anahtar kelimeyi basta kullan, marka adini sona koy "
                + "(orn. \"Kirmizi Kadin Bot Modelleri | Marka\").\n"
                + "3) Sablonda title bos bir degiskene bagliysa yedek bir deger tanimla.\n"
                + "4) Basligi yalnizca javascript ile yaziyorsan sunucu tarafinda da bas; ilk HTML "
                + "yanitinda bulunmasi gerekir.",
                "https://developers.google.com/search/docs/appearance/title-link"),

            R("META_TITLE_TOO_SHORT", RuleCategory.Meta, Severity.Medium, 5,
                "Title cok kisa",
                "Title 30 karakterden kisa. Kisa basliklar sayfanin ne sundugunu anlatmaya yetmez, "
                + "arama sonucunda tiklama oranini dusurur ve kelime cesitliligi olmadigi icin daha "
                + "az sorguyla eslesir. Teknik bir hata degil, kacirilmis bir firsattir.",
                "Marka ana sayfasi gibi tek kelimenin yeterli oldugu yerlerde kabul edilebilir; "
                + "orada yoksayabilirsin. Kategori ve urun sayfalarinda yoksayma.",
                "1) Basliga sayfayi ayirt eden nitelik ekle: kategori, model, sehir, yil gibi.\n"
                + "2) 30-60 karakter araligini hedefle; doldurma kelimesi degil gercek bilgi "
                + "ekle.\n"
                + "3) Ayni kaliptan uretilen diger sayfalarla birebir ayni olmadigindan emin ol.\n"
                + "4) Anahtar kelime yigmadan, okunabilir tek bir cumle kur.",
                "https://developers.google.com/search/docs/appearance/title-link"),

            R("META_TITLE_TOO_LONG", RuleCategory.Meta, Severity.Medium, 5,
                "Title cok uzun",
                "Title 60 karakterden uzun. Arama sonucunda baslik karakter degil piksel genisligine "
                + "gore kirpilir; sondaki kelimeler kullaniciya hic gorunmez ve cumle yarida kalinca "
                + "guven duser. Uzunluk bir ceza sebebi degildir, ama gorunurluk kaybi gerceklesir.",
                "Uzun urun adlarinda kirpilma kacinilmaz olabilir; ilk 60 karakter kendi basina "
                + "anlam tasiyorsa yoksayabilirsin.",
                "1) En onemli bilgiyi ilk 60 karaktere tasi.\n"
                + "2) Marka adini kisalt ya da cikar; tekrar eden ekleri (\"en iyi\", \"ucuz\", "
                + "yil bilgisi) temizle.\n"
                + "3) Kategori kaliplarindaki gereksiz sabit onekleri sablondan kaldir.\n"
                + "4) Yeni basligi arama sonucu onizlemesinde kirpilmadan gorundugu noktaya kadar "
                + "kisalt.",
                "https://developers.google.com/search/docs/appearance/title-link"),

            R("META_TITLE_DUPLICATE", RuleCategory.Meta, Severity.Medium, 5,
                "Yinelenen title",
                "Ayni title birden fazla sayfada kullaniliyor. Arama motoru hangi sayfanin hangi "
                + "sorguya cevap oldugunu ayirt edemez; sayfalar birbirinin yerine gecerek "
                + "gorunurlugu boler ve hicbiri tam guc kazanamaz. En sik sebep sablondan gelen "
                + "sabit baslik ile sayfalanmis veya filtreli listelerdir.",
                "Sayfalar gercekten ayni icerigin varyantiysa cozum baslik degistirmek degil "
                + "canonical vermektir; canonical verildiyse yoksayabilirsin.",
                "1) Cakisan sayfalari karsilastir; gercekten farkli iceriklerse her birine kendi "
                + "basligini yaz.\n"
                + "2) Sablonda basligi ayirt edici bir degiskenle uret: urun adi, kategori, sayfa "
                + "numarasi.\n"
                + "3) Sayfalanmis listelerde basliga \"- Sayfa 2\" gibi bir ek koy.\n"
                + "4) Sayfalar ayni icerigin varyantiysa canonical ile asil surumu isaret et.",
                "https://developers.google.com/search/docs/appearance/title-link"),

            R("META_DESC_MISSING", RuleCategory.Meta, Severity.High, 6,
                "Meta description yok",
                "Sayfada meta description yok. Bu etiket dogrudan bir siralama faktoru degildir ama "
                + "arama sonucundaki ozet metnini belirler; yoksa metin sayfa icinden secilir ve "
                + "cogu zaman menu, cerez uyarisi veya yasal metin parcasi one cikar. Iyi yazilmis "
                + "bir ozet, siralama degismeden tiklama oranini artirir.",
                "Otomatik uretilmis binlerce sayfada bos birakmak, hepsine ayni kotu aciklamayi "
                + "yazmaktan iyidir; o durumda yoksayabilirsin. Onemli acilis sayfalarinda "
                + "yoksayma.",
                "1) 120-155 karakter arasi, sayfanin vaadini ve bir eylem cagrisini iceren ozgun "
                + "bir aciklama yaz.\n"
                + "2) Anahtar kelimeyi dogal bicimde gecir; eslesen kelimeler sonucta kalin "
                + "gosterilir ve dikkat ceker.\n"
                + "3) Sablonla uretiyorsan icerigin ilk cumlesini kopyalamak yerine ayri bir ozet "
                + "alani kullan.\n"
                + "4) Aciklamayi title ile birebir ayni yapma; ikisi birbirini tamamlasin.",
                "https://developers.google.com/search/docs/appearance/snippet"),

            R("META_DESC_TOO_LONG", RuleCategory.Meta, Severity.Low, 3,
                "Meta description cok uzun",
                "Meta description 160 karakterden uzun. Fazlasi arama sonucunda uc noktayla kesilir "
                + "ve sonda kalan eylem cagrisi kullaniciya hic gorunmez. Uzun aciklama ceza almaz, "
                + "sadece etkisiz kalir.",
                "Gorunen sinir dile ve cihaza gore degistigi icin sinirin biraz uzerindeki "
                + "aciklamalar yoksayilabilir. Ilk 155 karakter tek basina anlam tasimiyorsa "
                + "yoksayma.",
                "1) En onemli cumleyi basa al ve toplamda 155 karakteri asma.\n"
                + "2) Tekrar eden marka adi veya slogan kismini cikar.\n"
                + "3) Aciklamayi sablon uretiyorsa kirpma islemini kelime sinirinda yap; cumleyi "
                + "ortasindan kesme.\n"
                + "4) Sonucu arama sonucu onizlemesinde dogrula.",
                "https://developers.google.com/search/docs/appearance/snippet"),

            R("META_DESC_DUPLICATE", RuleCategory.Meta, Severity.Low, 3,
                "Yinelenen meta description",
                "Ayni meta description birden fazla sayfada kullaniliyor. Aramada yan yana cikan "
                + "sonuclar birbirinin ayni gorunur ve kullanici hangisine girecegini secemez; "
                + "aciklamanin tiklama artirici islevi tamamen kaybolur. Genellikle sablondaki "
                + "sabit metinden kaynaklanir.",
                "Sayfalar ayni icerigin varyantiysa asil sorun aciklama degil eksik canonical'dir; "
                + "canonical verildiyse yoksayabilirsin.",
                "1) Aciklamayi sayfayi ayirt eden bilgiyle uret: urun ozelligi, kategori, konum.\n"
                + "2) Sablondaki sabit metni degiskenle degistir.\n"
                + "3) Ayirt edici bilgi uretemiyorsan aciklamayi bos birak; motorun sayfa icinden "
                + "secmesi yinelemeden iyidir.\n"
                + "4) Sayfalar ayni icerigin varyantiysa canonical ile birlestir.",
                "https://developers.google.com/search/docs/appearance/snippet"),

            // --- Content ---
            R("H1_MISSING", RuleCategory.Content, Severity.High, 6,
                "H1 yok",
                "Sayfada H1 basligi bulunmuyor. H1, icerik icin en ust seviye basliktir; sayfanin "
                + "konusunu hem kullaniciya hem ekran okuyucuya ilk o bildirir. Yoksa icerik "
                + "hiyerarsisi bastan kopuk olur ve sayfanin ana konusu zayif sinyallenir.",
                "Tasarim geregi buyuk bir baslik istemiyorsan dogru yol H1'i kaldirmak degil CSS "
                + "ile kucultmektir; bu yuzden yoksamak nadiren dogrudur.",
                "1) Sayfanin ana konusunu anlatan tek bir <h1> ekle.\n"
                + "2) H1'i title'in birebir kopyasi yapma; ayni konuyu farkli ifadeyle soyle.\n"
                + "3) Logo veya site adini H1 yapma — H1 sayfaya aittir, siteye degil.\n"
                + "4) Gorunum icin CSS ile boyutlandir; display:none ile gizleme.",
                "https://developers.google.com/search/docs/fundamentals/seo-starter-guide"),

            R("H1_MULTIPLE", RuleCategory.Content, Severity.Medium, 4,
                "Birden fazla H1",
                "Sayfada birden cok H1 var. HTML5 bolum yapisinda teknik olarak gecerlidir, ancak "
                + "pratikte sayfanin ana konusu bulaniklasir ve ekran okuyucuda gezinme zorlasir. "
                + "En sik sebep, sablonun hem site adini hem sayfa basligini H1 olarak basmasidir.",
                "Tek sayfada birbirinden bagimsiz birden fazla makale varsa (akis veya arsiv "
                + "duzeni) kabul edilebilir; orada yoksayabilirsin.",
                "1) Sayfanin ana konusunu anlatan tek H1'i sec.\n"
                + "2) Digerlerini icerik hiyerarsisine gore H2 veya H3 yap.\n"
                + "3) Site adi ya da logo H1 icindeyse sablonda p veya div'e cevir.\n"
                + "4) Basliklarin gorsel boyutunu etiket secerek degil CSS ile ayarla.",
                "https://developers.google.com/search/docs/fundamentals/seo-starter-guide"),

            R("THIN_CONTENT", RuleCategory.Content, Severity.Medium, 5,
                "Zayif icerik",
                "Sayfa metni 300 kelimenin altinda. Kelime sayisi tek basina bir siralama faktoru "
                + "degildir, ancak bu uzunluk cogu sorguda kullanicinin sorusunu karsilamaz ve sayfa "
                + "dusuk degerli olarak degerlendirilir. Cok sayida ince sayfa, sitenin genel kalite "
                + "algisini da asagi ceker.",
                "Iletisim, giris, tesekkur gibi islevsel sayfalarda kisa metin normaldir; bunlarda "
                + "kurali site genelinde yoksaymak dogrudur. Arama trafigi hedefleyen iceriklerde "
                + "yoksayma.",
                "1) Sayfanin hedefledigi soruyu belirle ve cevabi eksiksiz ver: kapsam, ornek, sik "
                + "sorulanlar.\n"
                + "2) Baska sayfalardan kopyalanmis metin yerine ozgun icerik uret; uzunluk tek "
                + "basina yeterli degildir.\n"
                + "3) Ayni konuyu bolen cok sayida ince sayfayi tek guclu sayfada birlestir, "
                + "eskilerini 301 ile yonlendir.\n"
                + "4) Sayfa dogasi geregi kisaysa kurali site genelinde yoksay.",
                "https://developers.google.com/search/docs/essentials/creating-helpful-content"),

            R("DUPLICATE_CONTENT", RuleCategory.Content, Severity.High, 7,
                "Yinelenen icerik",
                "Ayni icerik parmak izi birden fazla adreste goruluyor. Arama motoru bunlardan "
                + "yalnizca birini secer; secim senin istedigin adres olmayabilir ve gelen linklerin "
                + "degeri varyantlar arasinda bolunur. Cogunlukla parametreli adresler, http-https "
                + "ve www varyantlari, yazdirma surumleri veya kopyalanmis kategori sayfalari sebep "
                + "olur.",
                "Sablonu ayni ama govdesi gercekten farkli sayfalarda yanlis pozitif olabilir; "
                + "govdeyi karsilastirip oyleyse yoksay.",
                "1) Asil surumu belirle ve diger adreslerden ona canonical ver.\n"
                + "2) Adres varyantlarini normalize et: parametre siralamasi, sondaki slash, "
                + "buyuk-kucuk harf tek bicime insin.\n"
                + "3) Gercekten gereksiz kopyalari kaldir ve 301 ile asil adrese yonlendir.\n"
                + "4) Sayfalar farkli amaca hizmet ediyorsa icerigi gercekten farklilastir; sablon "
                + "degil govde metni degissin.",
                "https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls"),

            R("HEADING_HIERARCHY_BROKEN", RuleCategory.Content, Severity.Low, 3,
                "Baslik hiyerarsisi bozuk",
                "Baslik seviyeleri sirayla ilerlemiyor; en az bir seviye atlanmis (orn. h2'den sonra "
                + "h4). Ekran okuyucu kullanicilari icerikte baslik seviyelerine gore gezinir; "
                + "atlama olunca yapinin bir parcasi eksikmis gibi algilanir. Neredeyse her zaman "
                + "baslik etiketinin anlam yerine yazi tipi boyutu icin secilmesinden kaynaklanir.",
                "Arama motorlari acisindan etkisi kucuktur, asil maliyet erisilebilirliktedir. "
                + "Erisilebilirlik onceligin degilse dusuk oncelikle ele alabilir ya da "
                + "yoksayabilirsin.",
                "1) Basliklari anlam sirasina gore duzenle: h1 > h2 > h3, seviye atlamadan.\n"
                + "2) Boyut icin etiket degistirme; CSS sinifi kullan.\n"
                + "3) Editorlerin yanlis seviye secmesini onlemek icin icerik sablonunda "
                + "kullanilabilir seviyeleri sinirla.\n"
                + "4) Tarayicinin erisilebilirlik denetimiyle baslik agacini kontrol et.",
                "https://www.w3.org/WAI/tutorials/page-structure/headings/"),

            // --- Links ---
            R("BROKEN_INTERNAL_LINK", RuleCategory.Links, Severity.High, 6,
                "Kirik ic link",
                "Bir ic link 4xx/5xx donen sayfaya gidiyor. Kullanici akisin ortasinda hata "
                + "sayfasina duser; tarama acisindan da bosa harcanan bir istek olur ve linkle "
                + "aktarilacak deger kaybolur. Kirik ic link cogunlukla yeniden yapilandirma, "
                + "silinen urun veya elle yazilmis yanlis adresten kaynaklanir.",
                "Kimlik dogrulama arkasindaki sayfalar tarayiciya 401/403 dondugu icin yanlis "
                + "pozitif uretebilir; bunlari yoksayabilirsin. Herkese acik hedeflerde yoksayma.",
                "1) Kaynak sayfadaki linki ac ve hedefin gercek durum kodunu dogrula.\n"
                + "2) Hedef tasindiysa linki yeni adresle degistir; yonlendirmeye guvenip birakma.\n"
                + "3) Hedef kalici olarak yoksa linki kaldir ya da ilgili baska bir sayfaya "
                + "yonlendir.\n"
                + "4) Menu, altbilgi gibi site genelinde tekrar eden linklerde sablonu duzelt — tek "
                + "duzeltme tum sayfalari toparlar.",
                "https://developers.google.com/search/docs/crawling-indexing/links-crawlable"),

            R("ORPHAN_PAGE", RuleCategory.Links, Severity.Medium, 4,
                "Oksuz sayfa",
                "Sayfaya hicbir ic link isaret etmiyor. Ic link olmadan sayfa yalnizca sitemap veya "
                + "dis linklerle bulunabilir; kesfi yavaslar ve site icindeki onem sinyali sifira "
                + "yakin kalir. Kullanici da menuden ya da icerik icinden bu sayfaya ulasamaz.",
                "Kampanya acilis sayfalari gibi bilincli olarak menu disinda tutulan adreslerde "
                + "beklenen durumdur; o URL icin yoksayabilirsin.",
                "1) Sayfayi konu olarak en yakin iceriklerden aciklayici anchor metniyle linkle.\n"
                + "2) Kalici degerdeyse menu, kategori listesi veya \"ilgili icerik\" bloguna "
                + "ekle.\n"
                + "3) Bilincli olarak gizli tutuluyorsa kurali o URL icin yoksay.\n"
                + "4) Sayfanin artik degeri yoksa kaldir ve 301 ile ilgili sayfaya yonlendir.",
                "https://developers.google.com/search/docs/crawling-indexing/links-crawlable"),

            R("TOO_DEEP", RuleCategory.Links, Severity.Low, 3,
                "Cok derin sayfa",
                "Sayfa kok sayfadan 4 tiklamadan uzakta. Derin sayfalar hem kullanici hem tarayici "
                + "tarafindan daha az ziyaret edilir; buyuk sitelerde tarama butcesi ust seviyelerde "
                + "tukenir ve derindeki icerik gec guncellenir. Derinlik dogrudan bir ceza degildir, "
                + "ama onemli sayfalarin derinde kalmasi gorunurluk kaybidir.",
                "Arsiv, eski sayfalama ve dusuk oncelikli listelerde derinlik dogaldir; orada "
                + "yoksayabilirsin. Donusum getiren sayfalarda yoksayma.",
                "1) Sayfanin gercekten onemli olup olmadigina karar ver; onemliyse ust seviyeden "
                + "link ver.\n"
                + "2) Kategori/etiket sayfalarindan veya \"ilgili icerik\" bloklarindan kisayol "
                + "linkleri ekle.\n"
                + "3) Uzun sayfalama zincirlerini filtre veya kategori kirilimiyla kisalt.\n"
                + "4) Menuyu sisirmeden, konu kumelerini tek bir hub sayfasinda topla.",
                "https://developers.google.com/search/docs/fundamentals/seo-starter-guide"),

            R("GENERIC_ANCHOR_TEXT", RuleCategory.Links, Severity.Low, 3,
                "Aciklayici olmayan anchor",
                "Ic linklerde \"buraya tiklayin\", \"devamini oku\", \"detaylar\" gibi hedefi "
                + "anlatmayan anchor metinleri var. Anchor metni, hedef sayfanin konusunu bildiren "
                + "en guclu ic sinyallerden biridir; genel ifadeler bu bilgiyi hic tasimaz. Ekran "
                + "okuyucu kullanicilari link listesinde yalnizca anchor metnini duyar, hedefsiz "
                + "metinler gezinmeyi zorlastirir.",
                "Kart ve gorsel duzenlerinde kisa metin gerekiyorsa aria-label ile telafi "
                + "edilebilir; aria-label eklediysen yoksayabilirsin.",
                "1) Anchor metnini hedefin konusuyla degistir: \"devamini oku\" yerine \"iade "
                + "sureci nasil isler\".\n"
                + "2) Ayni sayfaya giden linklerde birebir ayni metni tekrarlamak zorunda "
                + "degilsin; dogal cesitlilik iyidir.\n"
                + "3) Tasarim kisa metin dayatiyorsa aria-label veya gorsel olarak gizli ek metin "
                + "kullan.\n"
                + "4) Anahtar kelimeyi zorlama; cumle icinde dogal duran ifadeyi sec.",
                "https://developers.google.com/search/docs/crawling-indexing/links-crawlable"),

            // --- Images ---
            R("IMAGE_MISSING_ALT", RuleCategory.Images, Severity.Low, 3,
                "Alt metni eksik gorseller",
                "Sayfada alt niteligi tasimayan <img> etiketleri var. Alt metni, gorsel "
                + "yuklenmediginde gosterilen ve ekran okuyucunun sesli okudugu metindir; ayrica "
                + "Gorsel Arama'da sayfanin bulunmasini saglar. Eksik alt, erisilebilirlik acisindan "
                + "dogrudan bir engeldir.",
                "Dekoratif gorsellerde dogru cozum alt=\"\" yazmaktir — niteligi tamamen kaldirmak "
                + "degil. Bos alt kullandiysan yoksayabilirsin.",
                "1) Anlam tasiyan her gorsele, gorseli goremeyen birine ne anlatirsan onu yaz.\n"
                + "2) \"resim\", \"foto\" gibi dolgu kelimelerinden ve anahtar kelime yigmaktan "
                + "kacin.\n"
                + "3) Dekoratif gorsellerde alt=\"\" kullan; boylece ekran okuyucu atlar.\n"
                + "4) Icerik yonetim sisteminde gorsel yuklerken alt alanini zorunlu hale getir.",
                "https://developers.google.com/search/docs/appearance/google-images"),

            R("IMAGE_TOO_LARGE", RuleCategory.Images, Severity.Medium, 4,
                "Buyuk gorsel",
                "Sayfadaki bir veya daha fazla gorsel 200 KB'i asiyor. Buyuk gorseller cogu sayfada "
                + "en gec yuklenen parcadir ve LCP olcumunu dogrudan belirler; mobil baglantida fark "
                + "saniyelerle olculur. Ayrica kullanicinin veri kotasini gereksiz harcar.",
                "Yuksek cozunurlugun urunun kendisi oldugu galeri ve portfolyo sayfalarinda buyuk "
                + "dosyalar kabul edilebilir. Liste ve kapak gorsellerinde yoksayma.",
                "1) Gorselleri WebP veya AVIF olarak yeniden uret; JPEG'e gore genellikle %30-50 "
                + "kucuk olur.\n"
                + "2) Gercek gosterim boyutunda sun; 3000 piksel genisligindeki dosyayi 400 "
                + "piksellik alana koyma.\n"
                + "3) srcset/sizes ile cihaza gore farkli boyut sun; ilk ekranin disindakilere "
                + "loading=\"lazy\" ver.\n"
                + "4) Ilk ekranda gorunen gorseli lazy yapma; tersine fetchpriority=\"high\" ile "
                + "onceliklendir.",
                "https://web.dev/articles/serve-responsive-images"),

            // --- Structured data & i18n ---
            R("SCHEMA_MISSING", RuleCategory.StructuredData, Severity.Low, 3,
                "Yapisal veri yok",
                "Sayfada schema.org isaretlemesi bulunamadi. Yapisal veri siralamayi dogrudan "
                + "degistirmez, ama arama sonucunda yildiz, fiyat, sik sorulanlar, tarif gibi zengin "
                + "gosterimleri mumkun kilar; bu da tiklama oranini artirir. Ayrica sayfanin ne "
                + "hakkinda oldugunu makineye net soyler.",
                "Hicbir zengin sonuc tipine uymayan sade bilgi sayfalarinda eksikligi sorun "
                + "degildir; orada yoksayabilirsin. Urun, tarif, etkinlik ve SSS sayfalarinda "
                + "yoksayma.",
                "1) Sayfanin turune uygun tipi sec: Article, Product, FAQPage, LocalBusiness, "
                + "BreadcrumbList.\n"
                + "2) JSON-LD olarak <script type=\"application/ld+json\"> icinde ekle; mikro veri "
                + "yerine JSON-LD tercih et.\n"
                + "3) Yalnizca sayfada gercekten gorunen bilgiyi isaretle; gorunmeyen veri politika "
                + "ihlalidir.\n"
                + "4) Zengin Sonuc Testi ile dogrula ve Search Console'daki gelismeler raporunu "
                + "izle.",
                "https://developers.google.com/search/docs/appearance/structured-data/intro-structured-data"),

            R("INVALID_STRUCTURED_DATA", RuleCategory.StructuredData, Severity.Medium, 4,
                "Yapisal veri gecersiz",
                "Sayfada JSON-LD var ama bicimi bozuk: gecersiz JSON ya da tek script etiketi icinde "
                + "birden fazla kok nesne. Bu durumda isaretlemenin tamami yok sayilir, yani emek "
                + "harcanmis ama hicbir zengin sonuc kazanci olusmaz. En sik sebepler kacisi "
                + "yapilmamis tirnak, sondaki fazla virgul ve sablonun yan yana bastigi iki "
                + "nesnedir.",
                "Bicimsel bir hata oldugu icin mesru istisnasi yoktur. Yoksamak, calismayan "
                + "isaretlemeyi sayfada birakmak demektir.",
                "1) Script icerigini bir JSON dogrulayicidan gecir; sondaki virgul ve kacissiz "
                + "tirnaklari duzelt.\n"
                + "2) Her kok nesneyi kendi <script type=\"application/ld+json\"> etiketine koy, ya "
                + "da hepsini tek bir dizi ([ ... ]) icinde topla.\n"
                + "3) Sablondan gelen metin degerlerini JSON kacisiyla yaz (tirnak, yeni satir, "
                + "ters bolu).\n"
                + "4) Zengin Sonuc Testi ile sayfayi tekrar tara ve hata kalmadigini dogrula.",
                "https://developers.google.com/search/docs/appearance/structured-data/intro-structured-data"),

            R("OG_TAGS_MISSING", RuleCategory.StructuredData, Severity.Low, 3,
                "Open Graph etiketleri eksik",
                "og:title, og:description veya og:image tanimli degil. Open Graph etiketleri, link "
                + "sosyal aglarda ve mesajlasma uygulamalarinda paylasildiginda gorunen kartin "
                + "icerigini belirler. Eksikse baslik ve gorsel rastgele secilir ya da kart tamamen "
                + "bos gorunur; bu da paylasimdan gelen tiklamayi dusurur.",
                "Arama siralamasina dogrudan etkisi yoktur; paylasim beklenmeyen ic sayfalarda "
                + "yoksayabilirsin. Blog ve urun sayfalarinda yoksayma.",
                "1) <head> icine og:title, og:description, og:url ve og:image ekle.\n"
                + "2) og:image icin en az 1200x630 piksel, mutlak URL'li bir gorsel kullan.\n"
                + "3) og:type degerini icerik turune gore ver: website, article, product.\n"
                + "4) Genis kart icin twitter:card=summary_large_image ekle ve paylasim onizleme "
                + "araciyla dogrula.",
                "https://ogp.me/"),

            R("LANG_ATTR_MISSING", RuleCategory.I18n, Severity.Medium, 4,
                "lang niteligi yok",
                "<html> etiketinde lang niteligi bulunmuyor. Bu nitelik ekran okuyuculara hangi "
                + "dilde okuyacagini, tarayicilara ceviri onerip onermeyecegini soyler. Eksikse "
                + "ekran okuyucu yanlis telaffuzla okur; erisilebilirlik acisindan somut bir "
                + "sorundur.",
                "Arama motoru dili icerikten de cikarabildigi icin siralama etkisi sinirlidir, ama "
                + "duzeltmesi tek satirdir — yoksamak icin iyi bir sebep nadiren bulunur.",
                "1) <html lang=\"tr\"> seklinde sayfanin ana dilini bildir.\n"
                + "2) Cok dilli sitede her surum kendi dil kodunu tasisin; degeri sablonda "
                + "sabitleme.\n"
                + "3) Sayfa icinde baska dilde bir blok varsa o elemana kendi lang niteligini "
                + "ver.\n"
                + "4) Dil surumleri arasinda hreflang baglantilarini da ekle.",
                "https://www.w3.org/International/questions/qa-html-language-declarations"),

            // --- Performance (PSI) ---
            R("LCP_POOR", RuleCategory.Performance, Severity.High, 7,
                "Kotu LCP",
                "Largest Contentful Paint 4 saniyenin uzerinde. LCP, ekrandaki en buyuk icerik "
                + "ogesinin (genellikle kapak gorseli veya baslik blogu) gorunur olma suresidir ve "
                + "\"sayfa acildi mi\" hissinin ana olcusudur. 2,5 sn alti iyi, 4 sn ustu kotu kabul "
                + "edilir.",
                "Olcum PageSpeed Insights'in tek seferlik laboratuvar kosusundan gelir; ag "
                + "dalgalanmasi payi vardir. Tek bir olcume dayanip yoksamak yerine olcumu "
                + "tekrarla.",
                "1) PageSpeed raporunda LCP ogesini belirle — cogunlukla ilk ekrandaki buyuk "
                + "gorseldir.\n"
                + "2) O gorseli onceliklendir: fetchpriority=\"high\" ve preload kullan, lazy "
                + "yukleme uygulama.\n"
                + "3) Render engelleyen CSS/JS'i azalt: kritik CSS'i satir ici ver, kalanini "
                + "ertele.\n"
                + "4) Sunucu yanit suresini (TTFB) dusur: onbellek, CDN ve sorgu optimizasyonu.\n"
                + "5) Gorseli modern formatta ve gercek gosterim boyutunda sun.",
                "https://web.dev/articles/lcp"),

            R("CLS_POOR", RuleCategory.Performance, Severity.Medium, 5,
                "Kotu CLS",
                "Cumulative Layout Shift 0,25'in uzerinde. CLS, sayfa yuklenirken icerigin ne kadar "
                + "zipladigini olcer; kullanici tam tiklayacakken butonun kaymasi bu metrige "
                + "yansir. 0,1 alti iyi, 0,25 ustu kotu kabul edilir. En sik sebepler boyutu "
                + "bildirilmemis gorseller, gec gelen reklam ve banner alanlari ile sonradan "
                + "yuklenen yazi tipleridir.",
                "Olcum tek seferlik laboratuvar kosusudur; cerez bandi gibi tek seferlik ogeler "
                + "sonucu sisirebilir. Gercek kullanici verisiyle karsilastirmadan yoksayma.",
                "1) Tum gorsel ve video etiketlerine width/height ver ya da CSS aspect-ratio "
                + "kullan.\n"
                + "2) Reklam, gomulu icerik ve bildirim seritleri icin onceden yer tutucu alan "
                + "ayir.\n"
                + "3) Yazi tipi degisiminde kaymayi azaltmak icin font-display degerini ayarla ve "
                + "yedek fontu metrik olarak yakin sec.\n"
                + "4) Mevcut icerigin ustune sonradan eleman ekleme; yeni icerigi kullanici "
                + "etkilesimi disinda araya sokma.",
                "https://web.dev/articles/cls"),

            R("INP_POOR", RuleCategory.Performance, Severity.Medium, 5,
                "Kotu INP",
                "Interaction to Next Paint 500 ms'nin uzerinde. INP, kullanicinin tikladiktan veya "
                + "yazdiktan sonra ekranda gorsel bir karsilik gormesi icin gecen sureyi olcer; "
                + "200 ms alti iyi, 500 ms ustu kotu kabul edilir. Yuksek INP genellikle ana is "
                + "parcacigini uzun sure mesgul eden JavaScript'ten kaynaklanir.",
                "Neredeyse hic etkilesim iceremeyen tanitim sayfalarinda olcumun guvenilirligi "
                + "dusuktur; orada yoksayabilirsin. Form ve filtre iceren sayfalarda yoksayma.",
                "1) 50 ms'yi asan uzun gorevleri parcalara bol; aralarda ana is parcacigini serbest "
                + "birak.\n"
                + "2) Etkilesim aninda agir hesaplama yapma; sonucu onceden hesapla veya web "
                + "worker'a tasi.\n"
                + "3) Kullanilmayan ucuncu taraf betiklerini kaldir, kalanlari ertele ya da "
                + "asenkron yukle.\n"
                + "4) Tiklamadan hemen sonra gorsel geri bildirim ver (durum degisimi), agir isi "
                + "ondan sonra calistir.",
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
