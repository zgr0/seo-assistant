using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnrichRuleTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BLOCKED_BY_ROBOTS_TXT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "robots.txt icindeki bir Disallow kurali, taranmasi gereken ic adresleri kapatiyor. Engellenen adresi arama motoru indiremez; icerigi okunamadigi icin sayfa ya hic dizine girmez ya da basliksiz-ozetsiz bos bir kayit olarak listelenir. Yonetim paneli, ic arama sonuclari ve tekrar eden filtre adresleri icin engel dogrudur. Dikkat: robots.txt engeli noindex ile ayni sey degildir — engellenen sayfa dis linkler uzerinden yine de dizine dusebilir.", "https://developers.google.com/search/docs/crawling-indexing/robots/intro", "1) robots.txt dosyasini ac ve engellenen yolu hangi Disallow satirinin yakaladigini bul.\n2) Kurali daralt: tum dizini kapatmak yerine yalnizca gizlenmesi gereken yolu yaz (orn. Disallow: /admin/ yerine Disallow: /admin/logs/).\n3) Amacin taramayi degil dizine girmeyi engellemekse robots.txt yerine noindex kullan; ikisini ayni sayfada birlikte kullanma — engellenen sayfada noindex hic okunamaz.\n4) Search Console'un robots.txt raporuyla dogrula, sonra URL Denetimi'nden yeniden tara." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_INTERNAL_LINK",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Bir ic link 4xx/5xx donen sayfaya gidiyor. Kullanici akisin ortasinda hata sayfasina duser; tarama acisindan da bosa harcanan bir istek olur ve linkle aktarilacak deger kaybolur. Kirik ic link cogunlukla yeniden yapilandirma, silinen urun veya elle yazilmis yanlis adresten kaynaklanir. Bilincli olarak 404 birakilan adresler varsa cozum onlara link vermemektir.", "https://developers.google.com/search/docs/crawling-indexing/links-crawlable", "1) Kaynak sayfadaki linki ac ve hedefin gercek durum kodunu dogrula.\n2) Hedef tasindiysa linki yeni adresle degistir; yonlendirmeye guvenip birakma.\n3) Hedef kalici olarak yoksa linki kaldir ya da ilgili baska bir sayfaya yonlendir.\n4) Menu, altbilgi gibi site genelinde tekrar eden linklerde sablonu duzelt — tek duzeltme tum sayfalari toparlar." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_PAGE_4XX",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfa 4xx (cogunlukla 404 veya 410) donuyor; icerik sunucuda yok. Kullanici hata sayfasina duser, arama motoru da adresi dizinden cikarir. Sayfaya ic link veya dis backlink geliyorsa o baglantilarin biriktirdigi deger de bosa gider. Silinmis kampanya adresleri icin 410 bilincli bir tercih olabilir, ancak adres hala bir yerden linkleniyorsa mutlaka ele alinmalidir.", "https://developers.google.com/search/docs/crawling-indexing/http-network-errors", "1) Adres kalici olarak kaldirildiysa en yakin ilgili sayfaya 301 yonlendirme koy; her seyi ana sayfaya yonlendirme — alakasiz hedef yumusak 404 sayilir.\n2) Adres yanlislikla kirildiysa icerigi geri getir veya dogru URL'e duzelt.\n3) Bu sayfaya isaret eden ic linkleri yeni adresle degistir; yonlendirme kalici cozum degil, gecis koprusudur.\n4) Icerik gercekten donmeyecekse 410 don ve adresi sitemap'ten cikar." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada rel=canonical etiketi tanimli degil. Canonical, ayni icerige birden fazla adresten ulasildiginda (izleme parametreleri, siralama/filtre, http-https ve www farki) hangisinin asil kabul edilecegini soyler. Etiket yoksa asil adresi arama motoru kendi secer ve beklemedigin bir varyant dizine girebilir. Parametresiz, tek adresli kucuk sitelerde etkisi sinirlidir — bu yuzden onemi dusuk tutulur.", "https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls", "1) Her sayfaya kendini gosteren bir canonical ekle: <link rel=\"canonical\" href=\"https://site.com/yol\">\n2) Mutlak URL kullan; protokol, www tercihi ve sondaki slash site genelinde tek bicimde olsun.\n3) Sayfalanmis listelerde her sayfa kendi adresini gostersin — hepsini ilk sayfaya baglama.\n4) Etiketin <head> icinde ve tek adet oldugunu dogrula; ikinci bir canonical ikisini birden gecersiz kilabilir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_POINTS_ELSEWHERE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "rel=canonical sayfanin kendi adresini degil baska bir adresi gosteriyor. Bu, \"beni dizine ekleme, asil olan su\" demektir; niyet buysa dogru, degilse sayfa sessizce aramadan silinir ve bunu hicbir hata mesaji haber vermez. En sik sebep sablonda sabitlenmis canonical veya cogaltilmis sayfa duzenidir. Yinelenen varyantlarda (utm parametreli, filtreli adresler) beklenen davranistir.", "https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls", "1) Hedef adresi ac: gercekten ayni icerik mi, 200 mu donuyor kontrol et.\n2) Bu sayfa asil surumse canonical'i kendi adresine cevir.\n3) Asil surum degilse mevcut hali birak; sayfaya hic ihtiyac yoksa kaldirip 301 ile hedefe yonlendirmeyi degerlendir.\n4) Sablon tum sayfalara ayni canonical'i basiyorsa degeri sayfa bazina cek." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CLS_POOR",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Cumulative Layout Shift 0,25'in uzerinde. CLS, sayfa yuklenirken icerigin ne kadar zipladigini olcer; kullanici tam tiklayacakken butonun kaymasi bu metrige yansir. 0,1 alti iyi, 0,25 ustu kotu kabul edilir. En sik sebepler boyutu bildirilmemis gorseller, gec gelen reklam ve banner alanlari ile sonradan yuklenen yazi tipleridir.", "https://web.dev/articles/cls", "1) Tum gorsel ve video etiketlerine width/height ver ya da CSS aspect-ratio kullan.\n2) Reklam, gomulu icerik ve bildirim seritleri icin onceden yer tutucu alan ayir.\n3) Yazi tipi degisiminde kaymayi azaltmak icin font-display degerini ayarla ve yedek fontu metrik olarak yakin sec.\n4) Mevcut icerigin ustune sonradan eleman ekleme; yeni icerigi kullanici etkilesimi disinda araya sokma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "DUPLICATE_CONTENT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Ayni icerik parmak izi birden fazla adreste goruluyor. Arama motoru bunlardan yalnizca birini secer; secim senin istedigin adres olmayabilir ve gelen linklerin degeri varyantlar arasinda bolunur. Cogunlukla parametreli adresler, http-https ve www varyantlari, yazdirma surumleri veya kopyalanmis kategori sayfalari sebep olur. Sablonu ayni, govdesi farkli sayfalarda yanlis pozitif olabilir.", "https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls", "1) Asil surumu belirle ve diger adreslerden ona canonical ver.\n2) Adres varyantlarini normalize et: parametre siralamasi, sondaki slash, buyuk-kucuk harf tek bicime insin.\n3) Gercekten gereksiz kopyalari kaldir ve 301 ile asil adrese yonlendir.\n4) Sayfalar farkli amaca hizmet ediyorsa icerigi gercekten farklilastir; sablon degil govde metni degissin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "GENERIC_ANCHOR_TEXT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Ic linklerde \"buraya tiklayin\", \"devamini oku\", \"detaylar\" gibi hedefi anlatmayan anchor metinleri var. Anchor metni, hedef sayfanin konusunu bildiren en guclu ic sinyallerden biridir; genel ifadeler bu bilgiyi hic tasimaz. Ekran okuyucu kullanicilari link listesinde yalnizca anchor metnini duyar, hedefsiz metinler gezinmeyi zorlastirir. Kart ve gorsel duzenlerinde kisa metin gerekiyorsa aria-label ile telafi edilebilir.", "https://developers.google.com/search/docs/crawling-indexing/links-crawlable", "1) Anchor metnini hedefin konusuyla degistir: \"devamini oku\" yerine \"iade sureci nasil isler\".\n2) Ayni sayfaya giden linklerde birebir ayni metni tekrarlamak zorunda degilsin; dogal cesitlilik iyidir.\n3) Tasarim kisa metin dayatiyorsa aria-label veya gorsel olarak gizli ek metin kullan.\n4) Anahtar kelimeyi zorlama; cumle icinde dogal duran ifadeyi sec." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada H1 basligi bulunmuyor. H1, icerik icin en ust seviye basliktir; sayfanin konusunu hem kullaniciya hem ekran okuyucuya ilk o bildirir. Yoksa icerik hiyerarsisi bastan kopuk olur ve sayfanin ana konusu zayif sinyallenir. Tasarim geregi buyuk bir baslik istemiyorsan dogru yol H1'i kaldirmak degil, CSS ile kucultmektir.", "https://developers.google.com/search/docs/fundamentals/seo-starter-guide", "1) Sayfanin ana konusunu anlatan tek bir <h1> ekle.\n2) H1'i title'in birebir kopyasi yapma; ayni konuyu farkli ifadeyle soyle.\n3) Logo veya site adini H1 yapma — H1 sayfaya aittir, siteye degil.\n4) Gorunum icin CSS ile boyutlandir; display:none ile gizleme." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MULTIPLE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada birden cok H1 var. HTML5 bolum yapisinda teknik olarak gecerlidir, ancak pratikte sayfanin ana konusu bulaniklasir ve ekran okuyucuda gezinme zorlasir. En sik sebep, sablonun hem site adini hem sayfa basligini H1 olarak basmasidir. Tek sayfada birbirinden bagimsiz birden fazla makale varsa kabul edilebilir.", "https://developers.google.com/search/docs/fundamentals/seo-starter-guide", "1) Sayfanin ana konusunu anlatan tek H1'i sec.\n2) Digerlerini icerik hiyerarsisine gore H2 veya H3 yap.\n3) Site adi ya da logo H1 icindeyse sablonda p veya div'e cevir.\n4) Basliklarin gorsel boyutunu etiket secerek degil CSS ile ayarla." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "HEADING_HIERARCHY_BROKEN",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Baslik seviyeleri sirayla ilerlemiyor; en az bir seviye atlanmis (orn. h2'den sonra h4). Ekran okuyucu kullanicilari icerikte baslik seviyelerine gore gezinir; atlama olunca yapinin bir parcasi eksikmis gibi algilanir. Arama motorlari acisindan etkisi kucuktur, asil maliyet erisilebilirliktedir. Neredeyse her zaman baslik etiketinin anlam yerine yazi tipi boyutu icin secilmesinden kaynaklanir.", "https://www.w3.org/WAI/tutorials/page-structure/headings/", "1) Basliklari anlam sirasina gore duzenle: h1 > h2 > h3, seviye atlamadan.\n2) Boyut icin etiket degistirme; CSS sinifi kullan.\n3) Editorlerin yanlis seviye secmesini onlemek icin icerik sablonunda kullanilabilir seviyeleri sinirla.\n4) Tarayicinin erisilebilirlik denetimiyle baslik agacini kontrol et." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_MISSING_ALT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada alt niteligi tasimayan <img> etiketleri var. Alt metni, gorsel yuklenmediginde gosterilen ve ekran okuyucunun sesli okudugu metindir; ayrica Gorsel Arama'da sayfanin bulunmasini saglar. Eksik alt, erisilebilirlik acisindan dogrudan bir engeldir. Dekoratif gorsellerde alt bos birakilmalidir (alt=\"\") — nitelik tamamen kaldirilmamalidir.", "https://developers.google.com/search/docs/appearance/google-images", "1) Anlam tasiyan her gorsele, gorseli goremeyen birine ne anlatirsan onu yaz.\n2) \"resim\", \"foto\" gibi dolgu kelimelerinden ve anahtar kelime yigmaktan kacin.\n3) Dekoratif gorsellerde alt=\"\" kullan; boylece ekran okuyucu atlar.\n4) Icerik yonetim sisteminde gorsel yuklerken alt alanini zorunlu hale getir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_TOO_LARGE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfadaki bir veya daha fazla gorsel 200 KB'i asiyor. Buyuk gorseller cogu sayfada en gec yuklenen parcadir ve LCP olcumunu dogrudan belirler; mobil baglantida fark saniyelerle olculur. Ayrica kullanicinin veri kotasini gereksiz harcar. Yuksek cozunurluk gerektiren galerilerde tek tek buyuk dosyalar kabul edilebilir, ama liste ve kapak gorsellerinde olmamalidir.", "https://web.dev/articles/serve-responsive-images", "1) Gorselleri WebP veya AVIF olarak yeniden uret; JPEG'e gore genellikle %30-50 kucuk olur.\n2) Gercek gosterim boyutunda sun; 3000 piksel genisligindeki dosyayi 400 piksellik alana koyma.\n3) srcset/sizes ile cihaza gore farkli boyut sun; ilk ekranin disindakilere loading=\"lazy\" ver.\n4) Ilk ekranda gorunen gorseli lazy yapma; tersine fetchpriority=\"high\" ile onceliklendir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INP_POOR",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Interaction to Next Paint 500 ms'nin uzerinde. INP, kullanicinin tikladiktan veya yazdiktan sonra ekranda gorsel bir karsilik gormesi icin gecen sureyi olcer; 200 ms alti iyi, 500 ms ustu kotu kabul edilir. Yuksek INP genellikle ana is parcacigini uzun sure mesgul eden JavaScript'ten kaynaklanir. Az etkilesimli tanitim sayfalarinda olcumun guvenilirligi dusuk olabilir.", "https://web.dev/articles/inp", "1) 50 ms'yi asan uzun gorevleri parcalara bol; aralarda ana is parcacigini serbest birak.\n2) Etkilesim aninda agir hesaplama yapma; sonucu onceden hesapla veya web worker'a tasi.\n3) Kullanilmayan ucuncu taraf betiklerini kaldir, kalanlari ertele ya da asenkron yukle.\n4) Tiklamadan hemen sonra gorsel geri bildirim ver (durum degisimi), agir isi ondan sonra calistir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INVALID_STRUCTURED_DATA",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada JSON-LD var ama bicimi bozuk: gecersiz JSON ya da tek script etiketi icinde birden fazla kok nesne. Bu durumda isaretlemenin tamami yok sayilir, yani emek harcanmis ama hicbir zengin sonuc kazanci olusmaz. En sik sebepler kacisi yapilmamis tirnak, sondaki fazla virgul ve sablonun yan yana bastigi iki nesnedir. Bicimsel bir hata oldugu icin istisnasi yoktur.", "https://developers.google.com/search/docs/appearance/structured-data/intro-structured-data", "1) Script icerigini bir JSON dogrulayicidan gecir; sondaki virgul ve kacissiz tirnaklari duzelt.\n2) Her kok nesneyi kendi <script type=\"application/ld+json\"> etiketine koy, ya da hepsini tek bir dizi ([ ... ]) icinde topla.\n3) Sablondan gelen metin degerlerini JSON kacisiyla yaz (tirnak, yeni satir, ters bolu).\n4) Zengin Sonuc Testi ile sayfayi tekrar tara ve hata kalmadigini dogrula." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LANG_ATTR_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "<html> etiketinde lang niteligi bulunmuyor. Bu nitelik ekran okuyuculara hangi dilde okuyacagini, tarayicilara ceviri onerip onermeyecegini soyler. Eksikse ekran okuyucu yanlis telaffuzla okur; erisilebilirlik acisindan somut bir sorundur. Arama motoru dili icerikten de cikarabildigi icin siralama etkisi sinirlidir.", "https://www.w3.org/International/questions/qa-html-language-declarations", "1) <html lang=\"tr\"> seklinde sayfanin ana dilini bildir.\n2) Cok dilli sitede her surum kendi dil kodunu tasisin; degeri sablonda sabitleme.\n3) Sayfa icinde baska dilde bir blok varsa o elemana kendi lang niteligini ver.\n4) Dil surumleri arasinda hreflang baglantilarini da ekle." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LCP_POOR",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Largest Contentful Paint 4 saniyenin uzerinde. LCP, ekrandaki en buyuk icerik ogesinin (genellikle kapak gorseli veya baslik blogu) gorunur olma suresidir ve \"sayfa acildi mi\" hissinin ana olcusudur. 2,5 sn alti iyi, 4 sn ustu kotu kabul edilir. Olcum PageSpeed Insights uzerinden alinir; tek seferlik bir olcumde ag dalgalanmasi payi olabilir.", "https://web.dev/articles/lcp", "1) PageSpeed raporunda LCP ogesini belirle — cogunlukla ilk ekrandaki buyuk gorseldir.\n2) O gorseli onceliklendir: fetchpriority=\"high\" ve preload kullan, lazy yukleme uygulama.\n3) Render engelleyen CSS/JS'i azalt: kritik CSS'i satir ici ver, kalanini ertele.\n4) Sunucu yanit suresini (TTFB) dusur: onbellek, CDN ve sorgu optimizasyonu.\n5) Gorseli modern formatta ve gercek gosterim boyutunda sun." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_DUPLICATE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Ayni meta description birden fazla sayfada kullaniliyor. Aramada yan yana cikan sonuclar birbirinin ayni gorunur ve kullanici hangisine girecegini secemez; aciklamanin tiklama artirici islevi tamamen kaybolur. Genellikle sablondaki sabit metinden kaynaklanir. Sayfalar ayni icerigin varyantiysa asil sorun aciklama degil, eksik canonical'dir.", "https://developers.google.com/search/docs/appearance/snippet", "1) Aciklamayi sayfayi ayirt eden bilgiyle uret: urun ozelligi, kategori, konum.\n2) Sablondaki sabit metni degiskenle degistir.\n3) Ayirt edici bilgi uretemiyorsan aciklamayi bos birak; motorun sayfa icinden secmesi yinelemeden iyidir.\n4) Sayfalar ayni icerigin varyantiysa canonical ile birlestir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada meta description yok. Bu etiket dogrudan bir siralama faktoru degildir ama arama sonucundaki ozet metnini belirler; yoksa metin sayfa icinden secilir ve cogu zaman menu, cerez uyarisi veya yasal metin parcasi one cikar. Iyi yazilmis bir ozet, siralama degismeden tiklama oranini artirir. Otomatik uretilmis binlerce sayfada bos birakmak, hepsine ayni kotu aciklamayi yazmaktan daha iyidir.", "https://developers.google.com/search/docs/appearance/snippet", "1) 120-155 karakter arasi, sayfanin vaadini ve bir eylem cagrisini iceren ozgun bir aciklama yaz.\n2) Anahtar kelimeyi dogal bicimde gecir; eslesen kelimeler sonucta kalin gosterilir ve dikkat ceker.\n3) Sablonla uretiyorsan icerigin ilk cumlesini kopyalamak yerine ayri bir ozet alani kullan.\n4) Aciklamayi title ile birebir ayni yapma; ikisi birbirini tamamlasin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_TOO_LONG",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Meta description 160 karakterden uzun. Fazlasi arama sonucunda uc noktayla kesilir ve sonda kalan eylem cagrisi kullaniciya hic gorunmez. Uzun aciklama ceza almaz, sadece etkisiz kalir. Gorunen sinir dile ve cihaza gore degistigi icin hedef karakter sayisini biraz altta tutmak guvenlidir.", "https://developers.google.com/search/docs/appearance/snippet", "1) En onemli cumleyi basa al ve toplamda 155 karakteri asma.\n2) Tekrar eden marka adi veya slogan kismini cikar.\n3) Aciklamayi sablon uretiyorsa kirpma islemini kelime sinirinda yap; cumleyi ortasindan kesme.\n4) Sonucu arama sonucu onizlemesinde dogrula." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_DUPLICATE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Ayni title birden fazla sayfada kullaniliyor. Arama motoru hangi sayfanin hangi sorguya cevap oldugunu ayirt edemez; sayfalar birbirinin yerine gecerek gorunurlugu boler ve hicbiri tam guc kazanamaz. En sik sebep sablondan gelen sabit baslik ile sayfalanmis veya filtreli listelerdir. Sayfalar gercekten ayni icerigin varyantiysa cozum baslik degistirmek degil canonical vermektir.", "https://developers.google.com/search/docs/appearance/title-link", "1) Cakisan sayfalari karsilastir; gercekten farkli iceriklerse her birine kendi basligini yaz.\n2) Sablonda basligi ayirt edici bir degiskenle uret: urun adi, kategori, sayfa numarasi.\n3) Sayfalanmis listelerde basliga \"- Sayfa 2\" gibi bir ek koy.\n4) Sayfalar ayni icerigin varyantiysa canonical ile asil surumu isaret et." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada <title> etiketi bulunmuyor. Title, arama sonucundaki basligin ve tarayici sekmesinin ana kaynagidir; en guclu sayfa ici sinyallerden biridir. Etiket yoksa arama motoru basligi sayfa icinden veya gelen link metinlerinden kendi uretir ve sonuc cogu zaman anlamsiz cikar. Bu kuralin istisnasi yoktur: her HTML sayfasinda bir title bulunmalidir.", "https://developers.google.com/search/docs/appearance/title-link", "1) <head> icine benzersiz bir <title> ekle; 30-60 karakter hedefle.\n2) Ana anahtar kelimeyi basta kullan, marka adini sona koy (orn. \"Kirmizi Kadin Bot Modelleri | Marka\").\n3) Sablonda title bos bir degiskene bagliysa yedek bir deger tanimla.\n4) Basligi yalnizca javascript ile yaziyorsan sunucu tarafinda da bas; ilk HTML yanitinda bulunmasi gerekir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_LONG",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Title 60 karakterden uzun. Arama sonucunda baslik karakter degil piksel genisligine gore kirpilir; sondaki kelimeler kullaniciya hic gorunmez ve cumle yarida kalinca guven duser. Uzunluk bir ceza sebebi degildir, ama gorunurluk kaybi gerceklesir. Uzun urun adlarinda kirpilma kacinilmaz olabilir; onemli olan ilk 60 karakterin kendi basina anlam tasimasidir.", "https://developers.google.com/search/docs/appearance/title-link", "1) En onemli bilgiyi ilk 60 karaktere tasi.\n2) Marka adini kisalt ya da cikar; tekrar eden ekleri (\"en iyi\", \"ucuz\", yil bilgisi) temizle.\n3) Kategori kaliplarindaki gereksiz sabit onekleri sablondan kaldir.\n4) Yeni basligi arama sonucu onizlemesinde kirpilmadan gorundugu noktaya kadar kisalt." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_SHORT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Title 30 karakterden kisa. Kisa basliklar sayfanin ne sundugunu anlatmaya yetmez, arama sonucunda tiklama oranini dusurur ve kelime cesitliligi olmadigi icin daha az sorguyla eslesir. Teknik bir hata degil, kacirilmis bir firsattir. Yalnizca marka ana sayfasi gibi tek kelimenin yeterli oldugu yerlerde kabul edilebilir.", "https://developers.google.com/search/docs/appearance/title-link", "1) Basliga sayfayi ayirt eden nitelik ekle: kategori, model, sehir, yil gibi.\n2) 30-60 karakter araligini hedefle; doldurma kelimesi degil gercek bilgi ekle.\n3) Ayni kaliptan uretilen diger sayfalarla birebir ayni olmadigindan emin ol.\n4) Anahtar kelime yigmadan, okunabilir tek bir cumle kur." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "OG_TAGS_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "og:title, og:description veya og:image tanimli degil. Open Graph etiketleri, link sosyal aglarda ve mesajlasma uygulamalarinda paylasildiginda gorunen kartin icerigini belirler. Eksikse baslik ve gorsel rastgele secilir ya da kart tamamen bos gorunur; bu da paylasimdan gelen tiklamayi dusurur. Arama siralamasina dogrudan etkisi yoktur.", "https://ogp.me/", "1) <head> icine og:title, og:description, og:url ve og:image ekle.\n2) og:image icin en az 1200x630 piksel, mutlak URL'li bir gorsel kullan.\n3) og:type degerini icerik turune gore ver: website, article, product.\n4) Genis kart icin twitter:card=summary_large_image ekle ve paylasim onizleme araciyla dogrula." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ORPHAN_PAGE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfaya hicbir ic link isaret etmiyor. Ic link olmadan sayfa yalnizca sitemap veya dis linklerle bulunabilir; kesfi yavaslar ve site icindeki onem sinyali sifira yakin kalir. Kullanici da menuden ya da icerik icinden bu sayfaya ulasamaz. Kampanya acilis sayfalari gibi bilincli olarak menu disinda tutulan adreslerde beklenen durumdur.", "https://developers.google.com/search/docs/crawling-indexing/links-crawlable", "1) Sayfayi konu olarak en yakin iceriklerden aciklayici anchor metniyle linkle.\n2) Kalici degerdeyse menu, kategori listesi veya \"ilgili icerik\" bloguna ekle.\n3) Bilincli olarak gizli tutuluyorsa kurali o URL icin yoksay.\n4) Sayfanin artik degeri yoksa kaldir ve 301 ile ilgili sayfaya yonlendir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "PAGE_NOT_IN_SITEMAP",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Dizinlenebilir bir sayfa sitemap'te listelenmiyor. Sitemap disindaki sayfa yine bulunabilir, ama kesfi tamamen ic linklere kalir; yeni yayinlanan iceriklerde bu gecikme gunlere yayilabilir. Ayrica sitemap kapsami ile gercek sayfa listesi arasindaki fark, Search Console raporlarini okumayi zorlastirir. Dizine girmemesi gereken sayfalar icin dogru cozum sitemap'e eklemek degil noindex vermektir.", "https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap", "1) Sayfa dizine girmeliyse sitemap'e ekle ve lastmod degerini gercek guncelleme tarihiyle doldur.\n2) Sitemap otomatik uretiliyorsa hangi filtrenin bu adresi eledigini kontrol et.\n3) Sayfa dizine girmemeliyse noindex ver ve sitemap disinda birak — iki sinyal birbiriyle celismesin.\n4) Guncellenen sitemap'i yeniden gonder." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_CHAIN",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfaya tek atlamada degil, birbirini izleyen birden fazla yonlendirmeyle ulasiliyor (orn. http > https > www > son adres). Her atlama ek gecikme demektir ve zincir uzadikca tarayicilarin takibi birakma ihtimali artar; tarama butcesi de gereksiz harcanir. Sinyal kaybi tek basina buyuk degildir, ama acilis suresi ve kesif verimliligi olculebilir sekilde duser. Alan adi tasima gibi gecislerde gecici olarak normaldir, kalicilasmamalidir.", "https://developers.google.com/search/docs/crawling-indexing/301-redirects", "1) Zinciri bastan sona izle ve 200 donen son adresi belirle.\n2) Ic linkleri, menuyu, sitemap'i ve canonical'lari dogrudan son adrese guncelle.\n3) Sunucu/CDN kurallarini birlestir: protokol ve www tercihini tek kuralda coz, yol yonlendirmesini ondan sonra uygula.\n4) Duzeltmeden sonra adresi tekrar iste ve geriye tek bir 301 kaldigini dogrula." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_TARGET_INVALID",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Yonlendirmenin Location basligi http/https disi ya da ayristirilamayan bir degere isaret ediyor; hicbir istemci hedefe ulasamaz. Kullanici zincirin ortasinda kalir, arama motoru da adresi olu kabul eder. Sebep genellikle sablon hatasi, eksik alan adi ya da javascript:/tel: gibi yanlis semali bir degerdir. Bu kuralin bilincli bir kullanimi yoktur; her tetiklenmesi gercek hatadir.", "https://developers.google.com/search/docs/crawling-indexing/301-redirects", "1) Yonlendirmeyi ureten kurali bul: sunucu konfigurasyonu, uygulama middleware'i veya CMS eklentisi.\n2) Location degerini gecerli bir http/https adresine cevir; goreli adres kullanacaksan koke gore yaz (/yol).\n3) Sablondan gelen bos degiskene karsi koruma ekle — hedef bos ise yonlendirme hic uretilmesin.\n4) curl -I ile yanit basligini isteyerek hedefin dogru dondugunu dogrula." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ROBOTS_NOINDEX",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfanin robots meta etiketinde ya da X-Robots-Tag yanit basliginda noindex var. Bu, arama motorlarina \"bu sayfayi dizine ekleme\" demektir; sayfa taransa bile sonuclarda hic gorunmez ve aldigi ic linklerin degeri bir yere aktarilmaz. Sepet, hesap, filtre veya tesekkur sayfalarinda bilincli bir tercih olabilir; trafik beklenen bir icerik sayfasinda ise en agir hatalardan biridir.", "https://developers.google.com/search/docs/crawling-indexing/block-indexing", "1) Sayfa kaynagindaki <meta name=\"robots\"> etiketini ve HTTP yanitindaki X-Robots-Tag basligini birlikte kontrol et; noindex ikisinden birinde olabilir.\n2) Sayfa dizine girmeliyse degeri index,follow yap veya etiketi tamamen kaldir.\n3) CMS/tema ayarlarinda \"arama motorlarini engelle\" secenegi aciksa kapat; test ortamindan kopyalanan ayar en sik sebeptir.\n4) Duzeltmeden sonra Search Console > URL Denetimi ile sayfayi yeniden tara." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SCHEMA_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada schema.org isaretlemesi bulunamadi. Yapisal veri siralamayi dogrudan degistirmez, ama arama sonucunda yildiz, fiyat, sik sorulanlar, tarif gibi zengin gosterimleri mumkun kilar; bu da tiklama oranini artirir. Ayrica sayfanin ne hakkinda oldugunu makineye net soyler. Hicbir zengin sonuc tipine uymayan sade sayfalarda eksikligi sorun degildir.", "https://developers.google.com/search/docs/appearance/structured-data/intro-structured-data", "1) Sayfanin turune uygun tipi sec: Article, Product, FAQPage, LocalBusiness, BreadcrumbList.\n2) JSON-LD olarak <script type=\"application/ld+json\"> icinde ekle; mikro veri yerine JSON-LD tercih et.\n3) Yalnizca sayfada gercekten gorunen bilgiyi isaretle; gorunmeyen veri politika ihlalidir.\n4) Zengin Sonuc Testi ile dogrula ve Search Console'daki gelismeler raporunu izle." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SERVER_ERROR_5XX",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfa 5xx donuyor ya da hic getirilemedi (baglanti hatasi, zaman asimi). Bu sunucu kaynakli bir arizadir; arama motoru 5xx gorunce once tarama hizini dusurur, hata surerse sayfayi dizinden cikarir. 4xx'ten daha acildir, cunku sorun cogu zaman tek sayfada degil altyapinin tamaminda olur. Planli bakim aninda alinan olcum ise yanlis pozitif olabilir — dogru davranis o sirada 503 donmektir.", "https://developers.google.com/search/docs/crawling-indexing/http-network-errors", "1) Sunucu ve uygulama loglarindan kok nedeni bul: istisna, veritabani baglantisi, bellek veya zaman asimi.\n2) Planli bakimsa 500 yerine 503 don ve Retry-After basligi ekle; boylece dizinden dusme riski azalir.\n3) Yavas yanit veren sayfalarda zaman asimi limitlerini ve sorgu maliyetini gozden gecir.\n4) Duzeltmeden sonra ayni adresi yeniden tara ve Search Console > Tarama Istatistikleri'nde hata oraninin dustugunu dogrula." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SITEMAP_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sitede okunabilir bir XML sitemap bulunamadi: robots.txt'te Sitemap satiri yok ve bilinen adreslerde gecerli bir dosya yanit vermiyor. Sitemap zorunlu degildir ama yeni ve derindeki sayfalarin kesfini hizlandirir, son guncelleme tarihini bildirir. Ozellikle ic linki zayif veya cok sayfali sitelerde fark buyuktur. Menuden her sayfaya erisilen birkac sayfalik sitelerde etkisi kucuktur.", "https://developers.google.com/search/docs/crawling-indexing/sitemaps/overview", "1) Yalnizca dizine girmesini istedigin, 200 donen ve canonical'i kendine bakan adresleri iceren bir sitemap.xml uret.\n2) 50.000 URL veya 50 MB sinirini asiyorsan sitemap index dosyasi kullan.\n3) robots.txt'e mutlak adresle bildir: Sitemap: https://site.com/sitemap.xml\n4) Search Console > Site Haritalari ekranindan gonder ve okundugunu dogrula." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "THIN_CONTENT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfa metni 300 kelimenin altinda. Kelime sayisi tek basina bir siralama faktoru degildir, ancak bu uzunluk cogu sorguda kullanicinin sorusunu karsilamaz ve sayfa dusuk degerli olarak degerlendirilir. Cok sayida ince sayfa, sitenin genel kalite algisini da asagi ceker. Iletisim, giris, tesekkur gibi islevsel sayfalarda kisa metin normaldir — bunlarda kurali yoksaymak dogrudur.", "https://developers.google.com/search/docs/essentials/creating-helpful-content", "1) Sayfanin hedefledigi soruyu belirle ve cevabi eksiksiz ver: kapsam, ornek, sik sorulanlar.\n2) Baska sayfalardan kopyalanmis metin yerine ozgun icerik uret; uzunluk tek basina yeterli degildir.\n3) Ayni konuyu bolen cok sayida ince sayfayi tek guclu sayfada birlestir, eskilerini 301 ile yonlendir.\n4) Sayfa dogasi geregi kisaysa (islevsel sayfa) kurali site genelinde yoksay." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "TOO_DEEP",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfa kok sayfadan 4 tiklamadan uzakta. Derin sayfalar hem kullanici hem tarayici tarafindan daha az ziyaret edilir; buyuk sitelerde tarama butcesi ust seviyelerde tukenir ve derindeki icerik gec guncellenir. Derinlik dogrudan bir ceza degildir, ama onemli sayfalarin derinde kalmasi gorunurluk kaybidir. Arsiv ve eski sayfa listelerinde derinlik dogaldir.", "https://developers.google.com/search/docs/fundamentals/seo-starter-guide", "1) Sayfanin gercekten onemli olup olmadigina karar ver; onemliyse ust seviyeden link ver.\n2) Kategori/etiket sayfalarindan veya \"ilgili icerik\" bloklarindan kisayol linkleri ekle.\n3) Uzun sayfalama zincirlerini filtre veya kategori kirilimiyla kisalt.\n4) Menuyu sisirmeden, konu kumelerini tek bir hub sayfasinda topla." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BLOCKED_BY_ROBOTS_TXT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "robots.txt taranmasi gereken ic adresleri kapatiyor.", null, "Disallow kurallarini daralt; yalnizca gercekten gizlenmesi gereken yollari kapat." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_INTERNAL_LINK",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Ic link 4xx/5xx donen bir sayfaya gidiyor.", null, "Hedefi guncelle veya linki kaldir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_PAGE_4XX",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfa 4xx durum kodu donuyor ve dizine eklenemez.", null, "Icerigi geri getir veya kalici olarak dogru adrese 301 yonlendir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "rel=canonical etiketi tanimli degil.", null, "Kendine referans veren bir canonical URL ekle." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_POINTS_ELSEWHERE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "rel=canonical sayfanin kendi adresini gostermiyor.", null, "Asil sayfa buysa canonical'i kendine cevir; degilse yinelenen icerigi kaldir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CLS_POOR",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Cumulative Layout Shift 0.25'in uzerinde.", null, "Gorsel/reklam alanlarina sabit boyut ver, gec yuklenen icerigi yer tutucuyla yerlestir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "DUPLICATE_CONTENT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Ayni content_hash birden fazla sayfada goruluyor.", null, "Icerigi farklilastir veya canonical ile asil sayfayi isaret et." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "GENERIC_ANCHOR_TEXT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Ic linklerde \"buraya tiklayin\" gibi genel anchor metinleri var.", null, "Anchor metnini hedef sayfanin konusunu anlatacak sekilde yaz." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada H1 basligi bulunmuyor.", null, "Sayfa basina tek ve aciklayici bir H1 ekle." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MULTIPLE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada birden cok H1 var.", null, "Tek H1 birak, digerlerini H2/H3 yap." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "HEADING_HIERARCHY_BROKEN",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Baslik seviyeleri atlanmis (orn. h2'den sonra h4).", null, "Basliklari sirayla kullan; seviye atlamadan h1 → h2 → h3 ilerle." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_MISSING_ALT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Bir veya daha fazla <img> alt niteligi tasimiyor.", null, "Anlamli gorsellere aciklayici alt metni ekle." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_TOO_LARGE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfadaki bir veya daha fazla gorsel 200 KB'i asiyor.", null, "Gorselleri sikistir, WebP/AVIF kullan ve boyutu ekrana gore olceklendir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INP_POOR",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Interaction to Next Paint 500 ms'nin uzerinde.", null, "Uzun JS gorevlerini bol, ana is parcacigini serbest birak." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INVALID_STRUCTURED_DATA",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada JSON-LD var ama bicimi bozuk; arama motorlari okuyamaz.", null, "Her kok nesneyi ayri bir <script type=\"application/ld+json\"> icine koy, JSON'u dogrula." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LANG_ATTR_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "<html> etiketinde lang niteligi bulunmuyor.", null, "Sayfanin dilini <html lang=\"tr\"> seklinde bildir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LCP_POOR",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Largest Contentful Paint 4 sn'nin uzerinde.", null, "Kritik gorselleri onceliklendir, render-blocking kaynaklari azalt." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_DUPLICATE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Ayni meta description birden fazla sayfada kullaniliyor.", null, "Her sayfa icin ayri bir aciklama yaz." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada meta description yok; SERP snippet'i kontrolsuz.", null, "Tiklama tesvik eden, 160 karakteri asmayan bir meta description yaz." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_TOO_LONG",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Meta description 160 karakterden uzun.", null, "Uzunlugu 160 karakterin altina cek." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_DUPLICATE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Ayni title birden fazla sayfada kullaniliyor.", null, "Her sayfaya icerigini anlatan benzersiz bir title yaz." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada <title> etiketi bulunmuyor.", null, "Her sayfaya 30-60 karakter, anahtar kelime iceren benzersiz bir title ekle." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_LONG",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Title 60 karakterden uzun; SERP'te kirpilir.", null, "Title'i 60 karakterin altina indir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_SHORT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Title 30 karakterden kisa.", null, "Title'i 30-60 karakter araligina cikar." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "OG_TAGS_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "og:title, og:description veya og:image tanimli degil.", null, "Paylasim kartlari icin temel Open Graph etiketlerini ekle." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ORPHAN_PAGE",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfaya hicbir ic link isaret etmiyor.", null, "Ilgili sayfalardan ic link ver; menu veya icerik icinden erisilebilir yap." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "PAGE_NOT_IN_SITEMAP",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Dizinlenebilir sayfa sitemap'te listelenmiyor.", null, "Sayfayi sitemap'e ekle veya dizine girmemesi gerekiyorsa noindex ver." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_CHAIN",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfaya birden fazla yonlendirme atlayarak ulasiliyor.", null, "Linkleri son adrese guncelle, zinciri tek atlamaya indir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_TARGET_INVALID",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Yonlendirme http/https disi bir adrese gidiyor; hicbir istemci hedefe ulasamaz.", null, "Location basligini gercek bir URL'e cevir ya da yonlendirmeyi kaldir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ROBOTS_NOINDEX",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "robots meta veya X-Robots-Tag noindex iceriyor.", null, "Dizine girmesi gereken sayfalarda noindex'i kaldir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SCHEMA_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfada schema.org isaretlemesi bulunamadi.", null, "Uygun schema tipini (Article, Product, FAQ...) JSON-LD olarak ekle." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SERVER_ERROR_5XX",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfa 5xx donuyor ya da hic getirilemedi.", null, "Sunucu hatasini gider; getirilemeyen sayfalar dizinden dusuyor." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SITEMAP_MISSING",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sitede okunabilir bir sitemap bulunamadi.", null, "sitemap.xml yayinla ve robots.txt icinde Sitemap satiriyla bildir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "THIN_CONTENT",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfa metni 300 kelimenin altinda.", null, "Icerigi ozgun ve kullaniciya deger katacak sekilde genislet." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "TOO_DEEP",
                columns: new[] { "description_tr", "doc_url", "how_to_fix_tr" },
                values: new object[] { "Sayfa kok sayfadan 4 tiklamadan uzakta.", null, "Site yapisini duzlestir; onemli sayfalari ust seviyelere yaklastir." });
        }
    }
}
