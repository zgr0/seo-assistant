using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TurkishDiacritics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "platform_profiles",
                keyColumn: "code",
                keyValue: "facebook",
                column: "guidance_tr",
                value: "Kısa metin + tek net CTA en iyi performansı verir. Link önizlemesi otomatik gelir, URL'i metinden çıkarabilirsin. Hashtag az kullanılır.");

            migrationBuilder.UpdateData(
                table: "platform_profiles",
                keyColumn: "code",
                keyValue: "instagram",
                column: "guidance_tr",
                value: "İlk satır kancadır; link biyoya alınır. Görsel odaklı, 3-5 anlamlı hashtag yeterli. Emoji dengeli kullanılır.");

            migrationBuilder.UpdateData(
                table: "platform_profiles",
                keyColumn: "code",
                keyValue: "linkedin",
                column: "guidance_tr",
                value: "Profesyonel ton, değerli içgörü ile başla. İlk 2 satır 'devamını gör' öncesi görünür. 3-5 sektörel hashtag. Emoji az.");

            migrationBuilder.UpdateData(
                table: "platform_profiles",
                keyColumn: "code",
                keyValue: "x",
                column: "guidance_tr",
                value: "Link içeren gönderilerde erişim düşer; linki ilk yanıta almayı öner. En fazla 1-2 hashtag. Net, iddialı tek cümle.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BLOCKED_BY_ROBOTS_TXT",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "robots.txt içindeki bir Disallow kuralı, taranması gereken iç adresleri kapatıyor. Engellenen adresi arama motoru indiremez; içeriği okunamadığı için sayfa ya hiç dizine girmez ya da başlıksız-özetsiz boş bir kayıt olarak listelenir. Dikkat: robots.txt engeli noindex ile aynı şey değildir — engellenen sayfa dış linkler üzerinden yine de dizine düşebilir.", "1) robots.txt dosyasını aç ve engellenen yolu hangi Disallow satırının yakaladığını bul.\n2) Kuralı daralt: tüm dizini kapatmak yerine yalnızca gizlenmesi gereken yolu yaz (örn. Disallow: /admin/ yerine Disallow: /admin/logs/).\n3) Amacın taramayı değil dizine girmeyi engellemekse robots.txt yerine noindex kullan; ikisini aynı sayfada birlikte kullanma — engellenen sayfada noindex hiç okunamaz.\n4) Search Console'un robots.txt raporuyla doğrula, sonra URL Denetimi'nden yeniden tara.", "Yönetim paneli, iç arama sonuçları ve tekrar eden filtre adresleri için engel doğrudur; bu adreslerde bulguyu yoksayabilirsin. İçerik sayfalarında ise doğrudan trafik kaybı demektir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_INTERNAL_LINK",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Bir iç link 4xx/5xx dönen sayfaya gidiyor. Kullanıcı akışın ortasında hata sayfasına düşer; tarama açısından da boşa harcanan bir istek olur ve linkle aktarılacak değer kaybolur. Kırık iç link çoğunlukla yeniden yapılandırma, silinen ürün veya elle yazılmış yanlış adresten kaynaklanır.", "1) Kaynak sayfadaki linki aç ve hedefin gerçek durum kodunu doğrula.\n2) Hedef taşındıysa linki yeni adresle değiştir; yönlendirmeye güvenip bırakma.\n3) Hedef kalıcı olarak yoksa linki kaldır ya da ilgili başka bir sayfaya yönlendir.\n4) Menü, altbilgi gibi site genelinde tekrar eden linklerde şablonu düzelt — tek düzeltme tüm sayfaları toparlar.", "Kırık iç link", "Kimlik doğrulama arkasındaki sayfalar tarayıcıya 401/403 döndüğü için yanlış pozitif üretebilir; bunları yoksayabilirsin. Herkese açık hedeflerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_PAGE_4XX",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa 4xx (çoğunlukla 404 veya 410) dönüyor; içerik sunucuda yok. Kullanıcı hata sayfasına düşer, arama motoru da adresi dizinden çıkarır. Sayfaya iç link veya dış backlink geliyorsa o bağlantıların biriktirdiği değer de boşa gider.", "1) Adres kalıcı olarak kaldırıldıysa en yakın ilgili sayfaya 301 yönlendirme koy; her şeyi ana sayfaya yönlendirme — alakasız hedef yumuşak 404 sayılır.\n2) Adres yanlışlıkla kırıldıysa içeriği geri getir veya doğru URL'e düzelt.\n3) Bu sayfaya işaret eden iç linkleri yeni adresle değiştir; yönlendirme kalıcı çözüm değil, geçiş köprüsüdür.\n4) İçerik gerçekten dönmeyecekse 410 dön ve adresi sitemap'ten çıkar.", "Sayfa 4xx dönüyor", "Silinmiş kampanya adresleri için 410 bilinçli bir tercih olabilir. Ancak adres hâlâ bir yerden linkleniyorsa yoksayma — önce linki temizlemek gerekir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada rel=canonical etiketi tanımlı değil. Canonical, aynı içeriğe birden fazla adresten ulaşıldığında (izleme parametreleri, sıralama/filtre, http-https ve www farkı) hangisinin asıl kabul edileceğini söyler. Etiket yoksa asıl adresi arama motoru kendi seçer ve beklemediğin bir varyant dizine girebilir.", "1) Her sayfaya kendini gösteren bir canonical ekle: <link rel=\"canonical\" href=\"https://site.com/yol\">\n2) Mutlak URL kullan; protokol, www tercihi ve sondaki slash site genelinde tek biçimde olsun.\n3) Sayfalanmış listelerde her sayfa kendi adresini göstersin — hepsini ilk sayfaya bağlama.\n4) Etiketin <head> içinde ve tek adet olduğunu doğrula; ikinci bir canonical ikisini birden geçersiz kılabilir.", "Parametresiz, tek adresli küçük sitelerde etkisi sınırlıdır — bu yüzden önemi düşük tutulur ve site genelinde yoksayılabilir. Filtre veya izleme parametresi üretilen sitelerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_POINTS_ELSEWHERE",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "rel=canonical sayfanın kendi adresini değil başka bir adresi gösteriyor. Bu, \"beni dizine ekleme, asıl olan şu\" demektir; niyet buysa doğru, değilse sayfa sessizce aramadan silinir ve bunu hiçbir hata mesajı haber vermez. En sık sebep şablonda sabitlenmiş canonical veya çoğaltılmış sayfa düzenidir.", "1) Hedef adresi aç: gerçekten aynı içerik mi, 200 mü dönüyor kontrol et.\n2) Bu sayfa asıl sürümse canonical'ı kendi adresine çevir.\n3) Asıl sürüm değilse mevcut hâli bırak; sayfaya hiç ihtiyaç yoksa kaldırıp 301 ile hedefe yönlendirmeyi değerlendir.\n4) Şablon tüm sayfalara aynı canonical'ı basıyorsa değeri sayfa bazına çek.", "Canonical başkasını gösteriyor", "Yinelenen varyantlarda (utm parametreli, filtreli veya yazdırma adresleri) beklenen davranıştır; oralarda yoksayabilirsin. Asıl sürüm olması gereken bir sayfada yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CLS_POOR",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Cumulative Layout Shift 0,25'in üzerinde. CLS, sayfa yüklenirken içeriğin ne kadar zıpladığını ölçer; kullanıcı tam tıklayacakken butonun kayması bu metriğe yansır. 0,1 altı iyi, 0,25 üstü kötü kabul edilir. En sık sebepler boyutu bildirilmemiş görseller, geç gelen reklam ve banner alanları ile sonradan yüklenen yazı tipleridir.", "1) Tüm görsel ve video etiketlerine width/height ver ya da CSS aspect-ratio kullan.\n2) Reklam, gömülü içerik ve bildirim şeritleri için önceden yer tutucu alan ayır.\n3) Yazı tipi değişiminde kaymayı azaltmak için font-display değerini ayarla ve yedek fontu metrik olarak yakın seç.\n4) Mevcut içeriğin üstüne sonradan eleman ekleme; yeni içeriği kullanıcı etkileşimi dışında araya sokma.", "Kötü CLS", "Ölçüm tek seferlik laboratuvar koşusudur; çerez bandı gibi tek seferlik ögeler sonucu şişirebilir. Gerçek kullanıcı verisiyle karşılaştırmadan yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "DUPLICATE_CONTENT",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Aynı içerik parmak izi birden fazla adreste görülüyor. Arama motoru bunlardan yalnızca birini seçer; seçim senin istediğin adres olmayabilir ve gelen linklerin değeri varyantlar arasında bölünür. Çoğunlukla parametreli adresler, http-https ve www varyantları, yazdırma sürümleri veya kopyalanmış kategori sayfaları sebep olur.", "1) Asıl sürümü belirle ve diğer adreslerden ona canonical ver.\n2) Adres varyantlarını normalize et: parametre sıralaması, sondaki slash, büyük-küçük harf tek biçime insin.\n3) Gerçekten gereksiz kopyaları kaldır ve 301 ile asıl adrese yönlendir.\n4) Sayfalar farklı amaca hizmet ediyorsa içeriği gerçekten farklılaştır; şablon değil gövde metni değişsin.", "Yinelenen içerik", "Şablonu aynı ama gövdesi gerçekten farklı sayfalarda yanlış pozitif olabilir; gövdeyi karşılaştırıp öyleyse yoksay." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "GENERIC_ANCHOR_TEXT",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "İç linklerde \"buraya tıklayın\", \"devamını oku\", \"detaylar\" gibi hedefi anlatmayan anchor metinleri var. Anchor metni, hedef sayfanın konusunu bildiren en güçlü iç sinyallerden biridir; genel ifadeler bu bilgiyi hiç taşımaz. Ekran okuyucu kullanıcıları link listesinde yalnızca anchor metnini duyar, hedefsiz metinler gezinmeyi zorlaştırır.", "1) Anchor metnini hedefin konusuyla değiştir: \"devamını oku\" yerine \"iade süreci nasıl işler\".\n2) Aynı sayfaya giden linklerde birebir aynı metni tekrarlamak zorunda değilsin; doğal çeşitlilik iyidir.\n3) Tasarım kısa metin dayatıyorsa aria-label veya görsel olarak gizli ek metin kullan.\n4) Anahtar kelimeyi zorlama; cümle içinde doğal duran ifadeyi seç.", "Açıklayıcı olmayan anchor", "Kart ve görsel düzenlerinde kısa metin gerekiyorsa aria-label ile telafi edilebilir; aria-label eklediysen yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada H1 başlığı bulunmuyor. H1, içerik için en üst seviye başlıktır; sayfanın konusunu hem kullanıcıya hem ekran okuyucuya ilk o bildirir. Yoksa içerik hiyerarşisi baştan kopuk olur ve sayfanın ana konusu zayıf sinyallenir.", "1) Sayfanın ana konusunu anlatan tek bir <h1> ekle.\n2) H1'i title'ın birebir kopyası yapma; aynı konuyu farklı ifadeyle söyle.\n3) Logo veya site adını H1 yapma — H1 sayfaya aittir, siteye değil.\n4) Görünüm için CSS ile boyutlandır; display:none ile gizleme.", "Tasarım gereği büyük bir başlık istemiyorsan doğru yol H1'i kaldırmak değil CSS ile küçültmektir; bu yüzden yoksamak nadiren doğrudur." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MULTIPLE",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada birden çok H1 var. HTML5 bölüm yapısında teknik olarak geçerlidir, ancak pratikte sayfanın ana konusu bulanıklaşır ve ekran okuyucuda gezinme zorlaşır. En sık sebep, şablonun hem site adını hem sayfa başlığını H1 olarak basmasıdır.", "1) Sayfanın ana konusunu anlatan tek H1'i seç.\n2) Diğerlerini içerik hiyerarşisine göre H2 veya H3 yap.\n3) Site adı ya da logo H1 içindeyse şablonda p veya div'e çevir.\n4) Başlıkların görsel boyutunu etiket seçerek değil CSS ile ayarla.", "Tek sayfada birbirinden bağımsız birden fazla makale varsa (akış veya arşiv düzeni) kabul edilebilir; orada yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "HEADING_HIERARCHY_BROKEN",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Başlık seviyeleri sırayla ilerlemiyor; en az bir seviye atlanmış (örn. h2'den sonra h4). Ekran okuyucu kullanıcıları içerikte başlık seviyelerine göre gezinir; atlama olunca yapının bir parçası eksikmiş gibi algılanır. Neredeyse her zaman başlık etiketinin anlam yerine yazı tipi boyutu için seçilmesinden kaynaklanır.", "1) Başlıkları anlam sırasına göre düzenle: h1 > h2 > h3, seviye atlamadan.\n2) Boyut için etiket değiştirme; CSS sınıfı kullan.\n3) Editörlerin yanlış seviye seçmesini önlemek için içerik şablonunda kullanılabilir seviyeleri sınırla.\n4) Tarayıcının erişilebilirlik denetimiyle başlık ağacını kontrol et.", "Başlık hiyerarşisi bozuk", "Arama motorları açısından etkisi küçüktür, asıl maliyet erişilebilirliktedir. Erişilebilirlik önceliğin değilse düşük öncelikle ele alabilir ya da yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_MISSING_ALT",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada alt niteliği taşımayan <img> etiketleri var. Alt metni, görsel yüklenmediğinde gösterilen ve ekran okuyucunun sesli okuduğu metindir; ayrıca Görsel Arama'da sayfanın bulunmasını sağlar. Eksik alt, erişilebilirlik açısından doğrudan bir engeldir.", "1) Anlam taşıyan her görsele, görseli göremeyen birine ne anlatırsan onu yaz.\n2) \"resim\", \"foto\" gibi dolgu kelimelerinden ve anahtar kelime yığmaktan kaçın.\n3) Dekoratif görsellerde alt=\"\" kullan; böylece ekran okuyucu atlar.\n4) İçerik yönetim sisteminde görsel yüklerken alt alanını zorunlu hâle getir.", "Alt metni eksik görseller", "Dekoratif görsellerde doğru çözüm alt=\"\" yazmaktır — niteliği tamamen kaldırmak değil. Boş alt kullandıysan yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_TOO_LARGE",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfadaki bir veya daha fazla görsel 200 KB'ı aşıyor. Büyük görseller çoğu sayfada en geç yüklenen parçadır ve LCP ölçümünü doğrudan belirler; mobil bağlantıda fark saniyelerle ölçülür. Ayrıca kullanıcının veri kotasını gereksiz harcar.", "1) Görselleri WebP veya AVIF olarak yeniden üret; JPEG'e göre genellikle %30-50 küçük olur.\n2) Gerçek gösterim boyutunda sun; 3000 piksel genişliğindeki dosyayı 400 piksellik alana koyma.\n3) srcset/sizes ile cihaza göre farklı boyut sun; ilk ekranın dışındakilere loading=\"lazy\" ver.\n4) İlk ekranda görünen görseli lazy yapma; tersine fetchpriority=\"high\" ile önceliklendir.", "Büyük görsel", "Yüksek çözünürlüğün ürünün kendisi olduğu galeri ve portfolyo sayfalarında büyük dosyalar kabul edilebilir. Liste ve kapak görsellerinde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INP_POOR",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Interaction to Next Paint 500 ms'nin üzerinde. INP, kullanıcının tıkladıktan veya yazdıktan sonra ekranda görsel bir karşılık görmesi için geçen süreyi ölçer; 200 ms altı iyi, 500 ms üstü kötü kabul edilir. Yüksek INP genellikle ana iş parçacığını uzun süre meşgul eden JavaScript'ten kaynaklanır.", "1) 50 ms'yi aşan uzun görevleri parçalara böl; aralarda ana iş parçacığını serbest bırak.\n2) Etkileşim anında ağır hesaplama yapma; sonucu önceden hesapla veya web worker'a taşı.\n3) Kullanılmayan üçüncü taraf betiklerini kaldır, kalanları ertele ya da asenkron yükle.\n4) Tıklamadan hemen sonra görsel geri bildirim ver (durum değişimi), ağır işi ondan sonra çalıştır.", "Kötü INP", "Neredeyse hiç etkileşim içermeyen tanıtım sayfalarında ölçümün güvenilirliği düşüktür; orada yoksayabilirsin. Form ve filtre içeren sayfalarda yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INVALID_STRUCTURED_DATA",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada JSON-LD var ama biçimi bozuk: geçersiz JSON ya da tek script etiketi içinde birden fazla kök nesne. Bu durumda işaretlemenin tamamı yok sayılır, yani emek harcanmış ama hiçbir zengin sonuç kazancı oluşmaz. En sık sebepler kaçışı yapılmamış tırnak, sondaki fazla virgül ve şablonun yan yana bastığı iki nesnedir.", "1) Script içeriğini bir JSON doğrulayıcıdan geçir; sondaki virgül ve kaçışsız tırnakları düzelt.\n2) Her kök nesneyi kendi <script type=\"application/ld+json\"> etiketine koy, ya da hepsini tek bir dizi ([ ... ]) içinde topla.\n3) Şablondan gelen metin değerlerini JSON kaçışıyla yaz (tırnak, yeni satır, ters bölü).\n4) Zengin Sonuç Testi ile sayfayı tekrar tara ve hata kalmadığını doğrula.", "Yapısal veri geçersiz", "Biçimsel bir hata olduğu için meşru istisnası yoktur. Yoksamak, çalışmayan işaretlemeyi sayfada bırakmak demektir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LANG_ATTR_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "<html> etiketinde lang niteliği bulunmuyor. Bu nitelik ekran okuyuculara hangi dilde okuyacağını, tarayıcılara çeviri önerip önermeyeceğini söyler. Eksikse ekran okuyucu yanlış telaffuzla okur; erişilebilirlik açısından somut bir sorundur.", "1) <html lang=\"tr\"> şeklinde sayfanın ana dilini bildir.\n2) Çok dilli sitede her sürüm kendi dil kodunu taşısın; değeri şablonda sabitleme.\n3) Sayfa içinde başka dilde bir blok varsa o elemana kendi lang niteliğini ver.\n4) Dil sürümleri arasında hreflang bağlantılarını da ekle.", "lang niteliği yok", "Arama motoru dili içerikten de çıkarabildiği için sıralama etkisi sınırlıdır, ama düzeltmesi tek satırdır — yoksamak için iyi bir sebep nadiren bulunur." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LCP_POOR",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Largest Contentful Paint 4 saniyenin üzerinde. LCP, ekrandaki en büyük içerik ögesinin (genellikle kapak görseli veya başlık bloğu) görünür olma süresidir ve \"sayfa açıldı mı\" hissinin ana ölçüsüdür. 2,5 sn altı iyi, 4 sn üstü kötü kabul edilir.", "1) PageSpeed raporunda LCP ögesini belirle — çoğunlukla ilk ekrandaki büyük görseldir.\n2) O görseli önceliklendir: fetchpriority=\"high\" ve preload kullan, lazy yükleme uygulama.\n3) Render engelleyen CSS/JS'i azalt: kritik CSS'i satır içi ver, kalanını ertele.\n4) Sunucu yanıt süresini (TTFB) düşür: önbellek, CDN ve sorgu optimizasyonu.\n5) Görseli modern formatta ve gerçek gösterim boyutunda sun.", "Kötü LCP", "Ölçüm PageSpeed Insights'ın tek seferlik laboratuvar koşusundan gelir; ağ dalgalanması payı vardır. Tek bir ölçüme dayanıp yoksamak yerine ölçümü tekrarla." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_DUPLICATE",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Aynı meta description birden fazla sayfada kullanılıyor. Aramada yan yana çıkan sonuçlar birbirinin aynı görünür ve kullanıcı hangisine gireceğini seçemez; açıklamanın tıklama artırıcı işlevi tamamen kaybolur. Genellikle şablondaki sabit metinden kaynaklanır.", "1) Açıklamayı sayfayı ayırt eden bilgiyle üret: ürün özelliği, kategori, konum.\n2) Şablondaki sabit metni değişkenle değiştir.\n3) Ayırt edici bilgi üretemiyorsan açıklamayı boş bırak; motorun sayfa içinden seçmesi yinelemeden iyidir.\n4) Sayfalar aynı içeriğin varyantıysa canonical ile birleştir.", "Sayfalar aynı içeriğin varyantıysa asıl sorun açıklama değil eksik canonical'dır; canonical verildiyse yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada meta description yok. Bu etiket doğrudan bir sıralama faktörü değildir ama arama sonucundaki özet metnini belirler; yoksa metin sayfa içinden seçilir ve çoğu zaman menü, çerez uyarısı veya yasal metin parçası öne çıkar. İyi yazılmış bir özet, sıralama değişmeden tıklama oranını artırır.", "1) 120-155 karakter arası, sayfanın vaadini ve bir eylem çağrısını içeren özgün bir açıklama yaz.\n2) Anahtar kelimeyi doğal biçimde geçir; eşleşen kelimeler sonuçta kalın gösterilir ve dikkat çeker.\n3) Şablonla üretiyorsan içeriğin ilk cümlesini kopyalamak yerine ayrı bir özet alanı kullan.\n4) Açıklamayı title ile birebir aynı yapma; ikisi birbirini tamamlasın.", "Otomatik üretilmiş binlerce sayfada boş bırakmak, hepsine aynı kötü açıklamayı yazmaktan iyidir; o durumda yoksayabilirsin. Önemli açılış sayfalarında yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_TOO_LONG",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Meta description 160 karakterden uzun. Fazlası arama sonucunda üç noktayla kesilir ve sonda kalan eylem çağrısı kullanıcıya hiç görünmez. Uzun açıklama ceza almaz, sadece etkisiz kalır.", "1) En önemli cümleyi başa al ve toplamda 155 karakteri aşma.\n2) Tekrar eden marka adı veya slogan kısmını çıkar.\n3) Açıklamayı şablon üretiyorsa kırpma işlemini kelime sınırında yap; cümleyi ortasından kesme.\n4) Sonucu arama sonucu önizlemesinde doğrula.", "Meta description çok uzun", "Görünen sınır dile ve cihaza göre değiştiği için sınırın biraz üzerindeki açıklamalar yoksayılabilir. İlk 155 karakter tek başına anlam taşımıyorsa yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_DUPLICATE",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Aynı title birden fazla sayfada kullanılıyor. Arama motoru hangi sayfanın hangi sorguya cevap olduğunu ayırt edemez; sayfalar birbirinin yerine geçerek görünürlüğü böler ve hiçbiri tam güç kazanamaz. En sık sebep şablondan gelen sabit başlık ile sayfalanmış veya filtreli listelerdir.", "1) Çakışan sayfaları karşılaştır; gerçekten farklı içeriklerse her birine kendi başlığını yaz.\n2) Şablonda başlığı ayırt edici bir değişkenle üret: ürün adı, kategori, sayfa numarası.\n3) Sayfalanmış listelerde başlığa \"- Sayfa 2\" gibi bir ek koy.\n4) Sayfalar aynı içeriğin varyantıysa canonical ile asıl sürümü işaret et.", "Sayfalar gerçekten aynı içeriğin varyantıysa çözüm başlık değiştirmek değil canonical vermektir; canonical verildiyse yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada <title> etiketi bulunmuyor. Title, arama sonucundaki başlığın ve tarayıcı sekmesinin ana kaynağıdır; en güçlü sayfa içi sinyallerden biridir. Etiket yoksa arama motoru başlığı sayfa içinden veya gelen link metinlerinden kendi üretir ve sonuç çoğu zaman anlamsız çıkar.", "1) <head> içine benzersiz bir <title> ekle; 30-60 karakter hedefle.\n2) Ana anahtar kelimeyi başta kullan, marka adını sona koy (örn. \"Kırmızı Kadın Bot Modelleri | Marka\").\n3) Şablonda title boş bir değişkene bağlıysa yedek bir değer tanımla.\n4) Başlığı yalnızca javascript ile yazıyorsan sunucu tarafında da bas; ilk HTML yanıtında bulunması gerekir.", "İstisnası yoktur: her HTML sayfasında bir title bulunmalıdır. Yoksaymak yerine şablonda yedek bir başlık tanımla." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_LONG",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Title 60 karakterden uzun. Arama sonucunda başlık karakter değil piksel genişliğine göre kırpılır; sondaki kelimeler kullanıcıya hiç görünmez ve cümle yarıda kalınca güven düşer. Uzunluk bir ceza sebebi değildir, ama görünürlük kaybı gerçekleşir.", "1) En önemli bilgiyi ilk 60 karaktere taşı.\n2) Marka adını kısalt ya da çıkar; tekrar eden ekleri (\"en iyi\", \"ucuz\", yıl bilgisi) temizle.\n3) Kategori kalıplarındaki gereksiz sabit önekleri şablondan kaldır.\n4) Yeni başlığı arama sonucu önizlemesinde kırpılmadan göründüğü noktaya kadar kısalt.", "Title çok uzun", "Uzun ürün adlarında kırpılma kaçınılmaz olabilir; ilk 60 karakter kendi başına anlam taşıyorsa yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_SHORT",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Title 30 karakterden kısa. Kısa başlıklar sayfanın ne sunduğunu anlatmaya yetmez, arama sonucunda tıklama oranını düşürür ve kelime çeşitliliği olmadığı için daha az sorguyla eşleşir. Teknik bir hata değil, kaçırılmış bir fırsattır.", "1) Başlığa sayfayı ayırt eden nitelik ekle: kategori, model, şehir, yıl gibi.\n2) 30-60 karakter aralığını hedefle; doldurma kelimesi değil gerçek bilgi ekle.\n3) Aynı kalıptan üretilen diğer sayfalarla birebir aynı olmadığından emin ol.\n4) Anahtar kelime yığmadan, okunabilir tek bir cümle kur.", "Title çok kısa", "Marka ana sayfası gibi tek kelimenin yeterli olduğu yerlerde kabul edilebilir; orada yoksayabilirsin. Kategori ve ürün sayfalarında yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "OG_TAGS_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "og:title, og:description veya og:image tanımlı değil. Open Graph etiketleri, link sosyal ağlarda ve mesajlaşma uygulamalarında paylaşıldığında görünen kartın içeriğini belirler. Eksikse başlık ve görsel rastgele seçilir ya da kart tamamen boş görünür; bu da paylaşımdan gelen tıklamayı düşürür.", "1) <head> içine og:title, og:description, og:url ve og:image ekle.\n2) og:image için en az 1200x630 piksel, mutlak URL'li bir görsel kullan.\n3) og:type değerini içerik türüne göre ver: website, article, product.\n4) Geniş kart için twitter:card=summary_large_image ekle ve paylaşım önizleme aracıyla doğrula.", "Arama sıralamasına doğrudan etkisi yoktur; paylaşım beklenmeyen iç sayfalarda yoksayabilirsin. Blog ve ürün sayfalarında yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ORPHAN_PAGE",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfaya hiçbir iç link işaret etmiyor. İç link olmadan sayfa yalnızca sitemap veya dış linklerle bulunabilir; keşfi yavaşlar ve site içindeki önem sinyali sıfıra yakın kalır. Kullanıcı da menüden ya da içerik içinden bu sayfaya ulaşamaz.", "1) Sayfayı konu olarak en yakın içeriklerden açıklayıcı anchor metniyle linkle.\n2) Kalıcı değerdeyse menü, kategori listesi veya \"ilgili içerik\" bloğuna ekle.\n3) Bilinçli olarak gizli tutuluyorsa kuralı o URL için yoksay.\n4) Sayfanın artık değeri yoksa kaldır ve 301 ile ilgili sayfaya yönlendir.", "Öksüz sayfa", "Kampanya açılış sayfaları gibi bilinçli olarak menü dışında tutulan adreslerde beklenen durumdur; o URL için yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "PAGE_NOT_IN_SITEMAP",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Dizinlenebilir bir sayfa sitemap'te listelenmiyor. Sitemap dışındaki sayfa yine bulunabilir, ama keşfi tamamen iç linklere kalır; yeni yayınlanan içeriklerde bu gecikme günlere yayılabilir. Ayrıca sitemap kapsamı ile gerçek sayfa listesi arasındaki fark, Search Console raporlarını okumayı zorlaştırır.", "1) Sayfa dizine girmeliyse sitemap'e ekle ve lastmod değerini gerçek güncelleme tarihiyle doldur.\n2) Sitemap otomatik üretiliyorsa hangi filtrenin bu adresi elediğini kontrol et.\n3) Sayfa dizine girmemeliyse noindex ver ve sitemap dışında bırak — iki sinyal birbiriyle çelişmesin.\n4) Güncellenen sitemap'i yeniden gönder.", "Sayfa sitemap dışında", "Dizine girmemesi gereken sayfalarda doğru çözüm sitemap'e eklemek değil noindex vermektir; noindex verdiysen bu bulguyu yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_CHAIN",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfaya tek atlamada değil, birbirini izleyen birden fazla yönlendirmeyle ulaşılıyor (örn. http > https > www > son adres). Her atlama ek gecikme demektir ve zincir uzadıkça tarayıcıların takibi bırakma ihtimali artar; tarama bütçesi de gereksiz harcanır. Sinyal kaybı tek başına büyük değildir, ama açılış süresi ve keşif verimliliği ölçülebilir şekilde düşer.", "1) Zinciri baştan sona izle ve 200 dönen son adresi belirle.\n2) İç linkleri, menüyü, sitemap'i ve canonical'ları doğrudan son adrese güncelle.\n3) Sunucu/CDN kurallarını birleştir: protokol ve www tercihini tek kuralda çöz, yol yönlendirmesini ondan sonra uygula.\n4) Düzeltmeden sonra adresi tekrar iste ve geriye tek bir 301 kaldığını doğrula.", "Yönlendirme zinciri", "Alan adı taşıma ve protokol geçişi gibi dönemlerde zincir geçici olarak normaldir. Geçiş tamamlandıktan sonra kalıcı hâle gelmişse yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_TARGET_INVALID",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Yönlendirmenin Location başlığı http/https dışı ya da ayrıştırılamayan bir değere işaret ediyor; hiçbir istemci hedefe ulaşamaz. Kullanıcı zincirin ortasında kalır, arama motoru da adresi ölü kabul eder. Sebep genellikle şablon hatası, eksik alan adı ya da javascript:/tel: gibi yanlış şemalı bir değerdir.", "1) Yönlendirmeyi üreten kuralı bul: sunucu konfigürasyonu, uygulama middleware'i veya CMS eklentisi.\n2) Location değerini geçerli bir http/https adresine çevir; göreli adres kullanacaksan köke göre yaz (/yol).\n3) Şablondan gelen boş değişkene karşı koruma ekle — hedef boş ise yönlendirme hiç üretilmesin.\n4) curl -I ile yanıt başlığını isteyerek hedefin doğru döndüğünü doğrula.", "Yönlendirme hedefi geçersiz", "Meşru bir istisnası yoktur; her tetiklenmesi gerçek bir hatadır. Yoksamak yerine yönlendirmeyi üreten kuralı düzelt." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ROBOTS_NOINDEX",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfanın robots meta etiketinde ya da X-Robots-Tag yanıt başlığında noindex var. Bu, arama motorlarına \"bu sayfayı dizine ekleme\" demektir; sayfa taransa bile sonuçlarda hiç görünmez ve aldığı iç linklerin değeri bir yere aktarılmaz. En sık sebep, test ortamından canlıya taşınan tema veya CMS ayarıdır.", "1) Sayfa kaynağındaki <meta name=\"robots\"> etiketini ve HTTP yanıtındaki X-Robots-Tag başlığını birlikte kontrol et; noindex ikisinden birinde olabilir.\n2) Sayfa dizine girmeliyse değeri index,follow yap veya etiketi tamamen kaldır.\n3) CMS/tema ayarlarında \"arama motorlarını engelle\" seçeneği açıksa kapat.\n4) Düzeltmeden sonra Search Console > URL Denetimi ile sayfayı yeniden tara.", "Sepet, hesap, ödeme ve teşekkür sayfalarında noindex bilinçli ve doğru bir tercihtir. Trafik beklenen bir içerik sayfasında ise yoksayma; bu, en ağır hatalardan biridir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SCHEMA_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada schema.org işaretlemesi bulunamadı. Yapısal veri sıralamayı doğrudan değiştirmez, ama arama sonucunda yıldız, fiyat, sık sorulanlar, tarif gibi zengin gösterimleri mümkün kılar; bu da tıklama oranını artırır. Ayrıca sayfanın ne hakkında olduğunu makineye net söyler.", "1) Sayfanın türüne uygun tipi seç: Article, Product, FAQPage, LocalBusiness, BreadcrumbList.\n2) JSON-LD olarak <script type=\"application/ld+json\"> içinde ekle; mikro veri yerine JSON-LD tercih et.\n3) Yalnızca sayfada gerçekten görünen bilgiyi işaretle; görünmeyen veri politika ihlalidir.\n4) Zengin Sonuç Testi ile doğrula ve Search Console'daki gelişmeler raporunu izle.", "Yapısal veri yok", "Hiçbir zengin sonuç tipine uymayan sade bilgi sayfalarında eksikliği sorun değildir; orada yoksayabilirsin. Ürün, tarif, etkinlik ve SSS sayfalarında yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SERVER_ERROR_5XX",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa 5xx dönüyor ya da hiç getirilemedi (bağlantı hatası, zaman aşımı). Bu sunucu kaynaklı bir arızadır; arama motoru 5xx görünce önce tarama hızını düşürür, hata sürerse sayfayı dizinden çıkarır. 4xx'ten daha acildir, çünkü sorun çoğu zaman tek sayfada değil altyapının tamamındadır.", "1) Sunucu ve uygulama loglarından kök nedeni bul: istisna, veritabanı bağlantısı, bellek veya zaman aşımı.\n2) Planlı bakımsa 500 yerine 503 dön ve Retry-After başlığı ekle; böylece dizinden düşme riski azalır.\n3) Yavaş yanıt veren sayfalarda zaman aşımı limitlerini ve sorgu maliyetini gözden geçir.\n4) Düzeltmeden sonra aynı adresi yeniden tara ve Search Console > Tarama İstatistikleri'nde hata oranının düştüğünü doğrula.", "Sunucu hatası", "Planlı bakım penceresinde alınmış bir ölçüm yanlış pozitif olabilir. Bu durumda doğru davranış bulguyu yoksaymak değil, bakım sırasında 500 yerine 503 döndürmektir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SITEMAP_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sitede okunabilir bir XML sitemap bulunamadı: robots.txt'te Sitemap satırı yok ve bilinen adreslerde geçerli bir dosya yanıt vermiyor. Sitemap zorunlu değildir ama yeni ve derindeki sayfaların keşfini hızlandırır, son güncelleme tarihini bildirir. Özellikle iç linki zayıf veya çok sayfalı sitelerde fark büyüktür.", "1) Yalnızca dizine girmesini istediğin, 200 dönen ve canonical'ı kendine bakan adresleri içeren bir sitemap.xml üret.\n2) 50.000 URL veya 50 MB sınırını aşıyorsan sitemap index dosyası kullan.\n3) robots.txt'e mutlak adresle bildir: Sitemap: https://site.com/sitemap.xml\n4) Search Console > Site Haritaları ekranından gönder ve okunduğunu doğrula.", "Menüden her sayfaya erişilen birkaç sayfalık sitelerde etkisi küçüktür; orada yoksayılabilir. Yüzlerce sayfalı veya sık içerik eklenen sitelerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "THIN_CONTENT",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa metni 300 kelimenin altında. Kelime sayısı tek başına bir sıralama faktörü değildir, ancak bu uzunluk çoğu sorguda kullanıcının sorusunu karşılamaz ve sayfa düşük değerli olarak değerlendirilir. Çok sayıda ince sayfa, sitenin genel kalite algısını da aşağı çeker.", "1) Sayfanın hedeflediği soruyu belirle ve cevabı eksiksiz ver: kapsam, örnek, sık sorulanlar.\n2) Başka sayfalardan kopyalanmış metin yerine özgün içerik üret; uzunluk tek başına yeterli değildir.\n3) Aynı konuyu bölen çok sayıda ince sayfayı tek güçlü sayfada birleştir, eskilerini 301 ile yönlendir.\n4) Sayfa doğası gereği kısaysa kuralı site genelinde yoksay.", "Zayıf içerik", "İletişim, giriş, teşekkür gibi işlevsel sayfalarda kısa metin normaldir; bunlarda kuralı site genelinde yoksaymak doğrudur. Arama trafiği hedefleyen içeriklerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "TOO_DEEP",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa kök sayfadan 4 tıklamadan uzakta. Derin sayfalar hem kullanıcı hem tarayıcı tarafından daha az ziyaret edilir; büyük sitelerde tarama bütçesi üst seviyelerde tükenir ve derindeki içerik geç güncellenir. Derinlik doğrudan bir ceza değildir, ama önemli sayfaların derinde kalması görünürlük kaybıdır.", "1) Sayfanın gerçekten önemli olup olmadığına karar ver; önemliyse üst seviyeden link ver.\n2) Kategori/etiket sayfalarından veya \"ilgili içerik\" bloklarından kısayol linkleri ekle.\n3) Uzun sayfalama zincirlerini filtre veya kategori kırılımıyla kısalt.\n4) Menüyü şişirmeden, konu kümelerini tek bir hub sayfasında topla.", "Çok derin sayfa", "Arşiv, eski sayfalama ve düşük öncelikli listelerde derinlik doğaldır; orada yoksayabilirsin. Dönüşüm getiren sayfalarda yoksayma." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "platform_profiles",
                keyColumn: "code",
                keyValue: "facebook",
                column: "guidance_tr",
                value: "Kisa metin + tek net CTA en iyi performansi verir. Link onizlemesi otomatik gelir, URL'i metinden cikarabilirsin. Hashtag az kullanilir.");

            migrationBuilder.UpdateData(
                table: "platform_profiles",
                keyColumn: "code",
                keyValue: "instagram",
                column: "guidance_tr",
                value: "Ilk satir kancadir; link biyoya alinir. Gorsel odakli, 3-5 anlamli hashtag yeterli. Emoji dengeli kullanilir.");

            migrationBuilder.UpdateData(
                table: "platform_profiles",
                keyColumn: "code",
                keyValue: "linkedin",
                column: "guidance_tr",
                value: "Profesyonel ton, degerli icgoru ile basla. Ilk 2 satir 'devamini gor' oncesi gorunur. 3-5 sektorel hashtag. Emoji az.");

            migrationBuilder.UpdateData(
                table: "platform_profiles",
                keyColumn: "code",
                keyValue: "x",
                column: "guidance_tr",
                value: "Link iceren gonderilerde erisim duser; linki ilk yanita almayi oner. En fazla 1-2 hashtag. Net, iddiali tek cumle.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BLOCKED_BY_ROBOTS_TXT",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "robots.txt icindeki bir Disallow kurali, taranmasi gereken ic adresleri kapatiyor. Engellenen adresi arama motoru indiremez; icerigi okunamadigi icin sayfa ya hic dizine girmez ya da basliksiz-ozetsiz bos bir kayit olarak listelenir. Dikkat: robots.txt engeli noindex ile ayni sey degildir — engellenen sayfa dis linkler uzerinden yine de dizine dusebilir.", "1) robots.txt dosyasini ac ve engellenen yolu hangi Disallow satirinin yakaladigini bul.\n2) Kurali daralt: tum dizini kapatmak yerine yalnizca gizlenmesi gereken yolu yaz (orn. Disallow: /admin/ yerine Disallow: /admin/logs/).\n3) Amacin taramayi degil dizine girmeyi engellemekse robots.txt yerine noindex kullan; ikisini ayni sayfada birlikte kullanma — engellenen sayfada noindex hic okunamaz.\n4) Search Console'un robots.txt raporuyla dogrula, sonra URL Denetimi'nden yeniden tara.", "Yonetim paneli, ic arama sonuclari ve tekrar eden filtre adresleri icin engel dogrudur; bu adreslerde bulguyu yoksayabilirsin. Icerik sayfalarinda ise dogrudan trafik kaybi demektir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_INTERNAL_LINK",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Bir ic link 4xx/5xx donen sayfaya gidiyor. Kullanici akisin ortasinda hata sayfasina duser; tarama acisindan da bosa harcanan bir istek olur ve linkle aktarilacak deger kaybolur. Kirik ic link cogunlukla yeniden yapilandirma, silinen urun veya elle yazilmis yanlis adresten kaynaklanir.", "1) Kaynak sayfadaki linki ac ve hedefin gercek durum kodunu dogrula.\n2) Hedef tasindiysa linki yeni adresle degistir; yonlendirmeye guvenip birakma.\n3) Hedef kalici olarak yoksa linki kaldir ya da ilgili baska bir sayfaya yonlendir.\n4) Menu, altbilgi gibi site genelinde tekrar eden linklerde sablonu duzelt — tek duzeltme tum sayfalari toparlar.", "Kirik ic link", "Kimlik dogrulama arkasindaki sayfalar tarayiciya 401/403 dondugu icin yanlis pozitif uretebilir; bunlari yoksayabilirsin. Herkese acik hedeflerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_PAGE_4XX",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa 4xx (cogunlukla 404 veya 410) donuyor; icerik sunucuda yok. Kullanici hata sayfasina duser, arama motoru da adresi dizinden cikarir. Sayfaya ic link veya dis backlink geliyorsa o baglantilarin biriktirdigi deger de bosa gider.", "1) Adres kalici olarak kaldirildiysa en yakin ilgili sayfaya 301 yonlendirme koy; her seyi ana sayfaya yonlendirme — alakasiz hedef yumusak 404 sayilir.\n2) Adres yanlislikla kirildiysa icerigi geri getir veya dogru URL'e duzelt.\n3) Bu sayfaya isaret eden ic linkleri yeni adresle degistir; yonlendirme kalici cozum degil, gecis koprusudur.\n4) Icerik gercekten donmeyecekse 410 don ve adresi sitemap'ten cikar.", "Sayfa 4xx donuyor", "Silinmis kampanya adresleri icin 410 bilincli bir tercih olabilir. Ancak adres hala bir yerden linkleniyorsa yoksayma — once linki temizlemek gerekir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada rel=canonical etiketi tanimli degil. Canonical, ayni icerige birden fazla adresten ulasildiginda (izleme parametreleri, siralama/filtre, http-https ve www farki) hangisinin asil kabul edilecegini soyler. Etiket yoksa asil adresi arama motoru kendi secer ve beklemedigin bir varyant dizine girebilir.", "1) Her sayfaya kendini gosteren bir canonical ekle: <link rel=\"canonical\" href=\"https://site.com/yol\">\n2) Mutlak URL kullan; protokol, www tercihi ve sondaki slash site genelinde tek bicimde olsun.\n3) Sayfalanmis listelerde her sayfa kendi adresini gostersin — hepsini ilk sayfaya baglama.\n4) Etiketin <head> icinde ve tek adet oldugunu dogrula; ikinci bir canonical ikisini birden gecersiz kilabilir.", "Parametresiz, tek adresli kucuk sitelerde etkisi sinirlidir — bu yuzden onemi dusuk tutulur ve site genelinde yoksayilabilir. Filtre veya izleme parametresi uretilen sitelerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_POINTS_ELSEWHERE",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "rel=canonical sayfanin kendi adresini degil baska bir adresi gosteriyor. Bu, \"beni dizine ekleme, asil olan su\" demektir; niyet buysa dogru, degilse sayfa sessizce aramadan silinir ve bunu hicbir hata mesaji haber vermez. En sik sebep sablonda sabitlenmis canonical veya cogaltilmis sayfa duzenidir.", "1) Hedef adresi ac: gercekten ayni icerik mi, 200 mu donuyor kontrol et.\n2) Bu sayfa asil surumse canonical'i kendi adresine cevir.\n3) Asil surum degilse mevcut hali birak; sayfaya hic ihtiyac yoksa kaldirip 301 ile hedefe yonlendirmeyi degerlendir.\n4) Sablon tum sayfalara ayni canonical'i basiyorsa degeri sayfa bazina cek.", "Canonical baskasini gosteriyor", "Yinelenen varyantlarda (utm parametreli, filtreli veya yazdirma adresleri) beklenen davranistir; oralarda yoksayabilirsin. Asil surum olmasi gereken bir sayfada yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CLS_POOR",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Cumulative Layout Shift 0,25'in uzerinde. CLS, sayfa yuklenirken icerigin ne kadar zipladigini olcer; kullanici tam tiklayacakken butonun kaymasi bu metrige yansir. 0,1 alti iyi, 0,25 ustu kotu kabul edilir. En sik sebepler boyutu bildirilmemis gorseller, gec gelen reklam ve banner alanlari ile sonradan yuklenen yazi tipleridir.", "1) Tum gorsel ve video etiketlerine width/height ver ya da CSS aspect-ratio kullan.\n2) Reklam, gomulu icerik ve bildirim seritleri icin onceden yer tutucu alan ayir.\n3) Yazi tipi degisiminde kaymayi azaltmak icin font-display degerini ayarla ve yedek fontu metrik olarak yakin sec.\n4) Mevcut icerigin ustune sonradan eleman ekleme; yeni icerigi kullanici etkilesimi disinda araya sokma.", "Kotu CLS", "Olcum tek seferlik laboratuvar kosusudur; cerez bandi gibi tek seferlik ogeler sonucu sisirebilir. Gercek kullanici verisiyle karsilastirmadan yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "DUPLICATE_CONTENT",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Ayni icerik parmak izi birden fazla adreste goruluyor. Arama motoru bunlardan yalnizca birini secer; secim senin istedigin adres olmayabilir ve gelen linklerin degeri varyantlar arasinda bolunur. Cogunlukla parametreli adresler, http-https ve www varyantlari, yazdirma surumleri veya kopyalanmis kategori sayfalari sebep olur.", "1) Asil surumu belirle ve diger adreslerden ona canonical ver.\n2) Adres varyantlarini normalize et: parametre siralamasi, sondaki slash, buyuk-kucuk harf tek bicime insin.\n3) Gercekten gereksiz kopyalari kaldir ve 301 ile asil adrese yonlendir.\n4) Sayfalar farkli amaca hizmet ediyorsa icerigi gercekten farklilastir; sablon degil govde metni degissin.", "Yinelenen icerik", "Sablonu ayni ama govdesi gercekten farkli sayfalarda yanlis pozitif olabilir; govdeyi karsilastirip oyleyse yoksay." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "GENERIC_ANCHOR_TEXT",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Ic linklerde \"buraya tiklayin\", \"devamini oku\", \"detaylar\" gibi hedefi anlatmayan anchor metinleri var. Anchor metni, hedef sayfanin konusunu bildiren en guclu ic sinyallerden biridir; genel ifadeler bu bilgiyi hic tasimaz. Ekran okuyucu kullanicilari link listesinde yalnizca anchor metnini duyar, hedefsiz metinler gezinmeyi zorlastirir.", "1) Anchor metnini hedefin konusuyla degistir: \"devamini oku\" yerine \"iade sureci nasil isler\".\n2) Ayni sayfaya giden linklerde birebir ayni metni tekrarlamak zorunda degilsin; dogal cesitlilik iyidir.\n3) Tasarim kisa metin dayatiyorsa aria-label veya gorsel olarak gizli ek metin kullan.\n4) Anahtar kelimeyi zorlama; cumle icinde dogal duran ifadeyi sec.", "Aciklayici olmayan anchor", "Kart ve gorsel duzenlerinde kisa metin gerekiyorsa aria-label ile telafi edilebilir; aria-label eklediysen yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada H1 basligi bulunmuyor. H1, icerik icin en ust seviye basliktir; sayfanin konusunu hem kullaniciya hem ekran okuyucuya ilk o bildirir. Yoksa icerik hiyerarsisi bastan kopuk olur ve sayfanin ana konusu zayif sinyallenir.", "1) Sayfanin ana konusunu anlatan tek bir <h1> ekle.\n2) H1'i title'in birebir kopyasi yapma; ayni konuyu farkli ifadeyle soyle.\n3) Logo veya site adini H1 yapma — H1 sayfaya aittir, siteye degil.\n4) Gorunum icin CSS ile boyutlandir; display:none ile gizleme.", "Tasarim geregi buyuk bir baslik istemiyorsan dogru yol H1'i kaldirmak degil CSS ile kucultmektir; bu yuzden yoksamak nadiren dogrudur." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MULTIPLE",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada birden cok H1 var. HTML5 bolum yapisinda teknik olarak gecerlidir, ancak pratikte sayfanin ana konusu bulaniklasir ve ekran okuyucuda gezinme zorlasir. En sik sebep, sablonun hem site adini hem sayfa basligini H1 olarak basmasidir.", "1) Sayfanin ana konusunu anlatan tek H1'i sec.\n2) Digerlerini icerik hiyerarsisine gore H2 veya H3 yap.\n3) Site adi ya da logo H1 icindeyse sablonda p veya div'e cevir.\n4) Basliklarin gorsel boyutunu etiket secerek degil CSS ile ayarla.", "Tek sayfada birbirinden bagimsiz birden fazla makale varsa (akis veya arsiv duzeni) kabul edilebilir; orada yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "HEADING_HIERARCHY_BROKEN",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Baslik seviyeleri sirayla ilerlemiyor; en az bir seviye atlanmis (orn. h2'den sonra h4). Ekran okuyucu kullanicilari icerikte baslik seviyelerine gore gezinir; atlama olunca yapinin bir parcasi eksikmis gibi algilanir. Neredeyse her zaman baslik etiketinin anlam yerine yazi tipi boyutu icin secilmesinden kaynaklanir.", "1) Basliklari anlam sirasina gore duzenle: h1 > h2 > h3, seviye atlamadan.\n2) Boyut icin etiket degistirme; CSS sinifi kullan.\n3) Editorlerin yanlis seviye secmesini onlemek icin icerik sablonunda kullanilabilir seviyeleri sinirla.\n4) Tarayicinin erisilebilirlik denetimiyle baslik agacini kontrol et.", "Baslik hiyerarsisi bozuk", "Arama motorlari acisindan etkisi kucuktur, asil maliyet erisilebilirliktedir. Erisilebilirlik onceligin degilse dusuk oncelikle ele alabilir ya da yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_MISSING_ALT",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada alt niteligi tasimayan <img> etiketleri var. Alt metni, gorsel yuklenmediginde gosterilen ve ekran okuyucunun sesli okudugu metindir; ayrica Gorsel Arama'da sayfanin bulunmasini saglar. Eksik alt, erisilebilirlik acisindan dogrudan bir engeldir.", "1) Anlam tasiyan her gorsele, gorseli goremeyen birine ne anlatirsan onu yaz.\n2) \"resim\", \"foto\" gibi dolgu kelimelerinden ve anahtar kelime yigmaktan kacin.\n3) Dekoratif gorsellerde alt=\"\" kullan; boylece ekran okuyucu atlar.\n4) Icerik yonetim sisteminde gorsel yuklerken alt alanini zorunlu hale getir.", "Alt metni eksik gorseller", "Dekoratif gorsellerde dogru cozum alt=\"\" yazmaktir — niteligi tamamen kaldirmak degil. Bos alt kullandiysan yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_TOO_LARGE",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfadaki bir veya daha fazla gorsel 200 KB'i asiyor. Buyuk gorseller cogu sayfada en gec yuklenen parcadir ve LCP olcumunu dogrudan belirler; mobil baglantida fark saniyelerle olculur. Ayrica kullanicinin veri kotasini gereksiz harcar.", "1) Gorselleri WebP veya AVIF olarak yeniden uret; JPEG'e gore genellikle %30-50 kucuk olur.\n2) Gercek gosterim boyutunda sun; 3000 piksel genisligindeki dosyayi 400 piksellik alana koyma.\n3) srcset/sizes ile cihaza gore farkli boyut sun; ilk ekranin disindakilere loading=\"lazy\" ver.\n4) Ilk ekranda gorunen gorseli lazy yapma; tersine fetchpriority=\"high\" ile onceliklendir.", "Buyuk gorsel", "Yuksek cozunurlugun urunun kendisi oldugu galeri ve portfolyo sayfalarinda buyuk dosyalar kabul edilebilir. Liste ve kapak gorsellerinde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INP_POOR",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Interaction to Next Paint 500 ms'nin uzerinde. INP, kullanicinin tikladiktan veya yazdiktan sonra ekranda gorsel bir karsilik gormesi icin gecen sureyi olcer; 200 ms alti iyi, 500 ms ustu kotu kabul edilir. Yuksek INP genellikle ana is parcacigini uzun sure mesgul eden JavaScript'ten kaynaklanir.", "1) 50 ms'yi asan uzun gorevleri parcalara bol; aralarda ana is parcacigini serbest birak.\n2) Etkilesim aninda agir hesaplama yapma; sonucu onceden hesapla veya web worker'a tasi.\n3) Kullanilmayan ucuncu taraf betiklerini kaldir, kalanlari ertele ya da asenkron yukle.\n4) Tiklamadan hemen sonra gorsel geri bildirim ver (durum degisimi), agir isi ondan sonra calistir.", "Kotu INP", "Neredeyse hic etkilesim iceremeyen tanitim sayfalarinda olcumun guvenilirligi dusuktur; orada yoksayabilirsin. Form ve filtre iceren sayfalarda yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INVALID_STRUCTURED_DATA",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada JSON-LD var ama bicimi bozuk: gecersiz JSON ya da tek script etiketi icinde birden fazla kok nesne. Bu durumda isaretlemenin tamami yok sayilir, yani emek harcanmis ama hicbir zengin sonuc kazanci olusmaz. En sik sebepler kacisi yapilmamis tirnak, sondaki fazla virgul ve sablonun yan yana bastigi iki nesnedir.", "1) Script icerigini bir JSON dogrulayicidan gecir; sondaki virgul ve kacissiz tirnaklari duzelt.\n2) Her kok nesneyi kendi <script type=\"application/ld+json\"> etiketine koy, ya da hepsini tek bir dizi ([ ... ]) icinde topla.\n3) Sablondan gelen metin degerlerini JSON kacisiyla yaz (tirnak, yeni satir, ters bolu).\n4) Zengin Sonuc Testi ile sayfayi tekrar tara ve hata kalmadigini dogrula.", "Yapisal veri gecersiz", "Bicimsel bir hata oldugu icin mesru istisnasi yoktur. Yoksamak, calismayan isaretlemeyi sayfada birakmak demektir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LANG_ATTR_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "<html> etiketinde lang niteligi bulunmuyor. Bu nitelik ekran okuyuculara hangi dilde okuyacagini, tarayicilara ceviri onerip onermeyecegini soyler. Eksikse ekran okuyucu yanlis telaffuzla okur; erisilebilirlik acisindan somut bir sorundur.", "1) <html lang=\"tr\"> seklinde sayfanin ana dilini bildir.\n2) Cok dilli sitede her surum kendi dil kodunu tasisin; degeri sablonda sabitleme.\n3) Sayfa icinde baska dilde bir blok varsa o elemana kendi lang niteligini ver.\n4) Dil surumleri arasinda hreflang baglantilarini da ekle.", "lang niteligi yok", "Arama motoru dili icerikten de cikarabildigi icin siralama etkisi sinirlidir, ama duzeltmesi tek satirdir — yoksamak icin iyi bir sebep nadiren bulunur." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LCP_POOR",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Largest Contentful Paint 4 saniyenin uzerinde. LCP, ekrandaki en buyuk icerik ogesinin (genellikle kapak gorseli veya baslik blogu) gorunur olma suresidir ve \"sayfa acildi mi\" hissinin ana olcusudur. 2,5 sn alti iyi, 4 sn ustu kotu kabul edilir.", "1) PageSpeed raporunda LCP ogesini belirle — cogunlukla ilk ekrandaki buyuk gorseldir.\n2) O gorseli onceliklendir: fetchpriority=\"high\" ve preload kullan, lazy yukleme uygulama.\n3) Render engelleyen CSS/JS'i azalt: kritik CSS'i satir ici ver, kalanini ertele.\n4) Sunucu yanit suresini (TTFB) dusur: onbellek, CDN ve sorgu optimizasyonu.\n5) Gorseli modern formatta ve gercek gosterim boyutunda sun.", "Kotu LCP", "Olcum PageSpeed Insights'in tek seferlik laboratuvar kosusundan gelir; ag dalgalanmasi payi vardir. Tek bir olcume dayanip yoksamak yerine olcumu tekrarla." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_DUPLICATE",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Ayni meta description birden fazla sayfada kullaniliyor. Aramada yan yana cikan sonuclar birbirinin ayni gorunur ve kullanici hangisine girecegini secemez; aciklamanin tiklama artirici islevi tamamen kaybolur. Genellikle sablondaki sabit metinden kaynaklanir.", "1) Aciklamayi sayfayi ayirt eden bilgiyle uret: urun ozelligi, kategori, konum.\n2) Sablondaki sabit metni degiskenle degistir.\n3) Ayirt edici bilgi uretemiyorsan aciklamayi bos birak; motorun sayfa icinden secmesi yinelemeden iyidir.\n4) Sayfalar ayni icerigin varyantiysa canonical ile birlestir.", "Sayfalar ayni icerigin varyantiysa asil sorun aciklama degil eksik canonical'dir; canonical verildiyse yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada meta description yok. Bu etiket dogrudan bir siralama faktoru degildir ama arama sonucundaki ozet metnini belirler; yoksa metin sayfa icinden secilir ve cogu zaman menu, cerez uyarisi veya yasal metin parcasi one cikar. Iyi yazilmis bir ozet, siralama degismeden tiklama oranini artirir.", "1) 120-155 karakter arasi, sayfanin vaadini ve bir eylem cagrisini iceren ozgun bir aciklama yaz.\n2) Anahtar kelimeyi dogal bicimde gecir; eslesen kelimeler sonucta kalin gosterilir ve dikkat ceker.\n3) Sablonla uretiyorsan icerigin ilk cumlesini kopyalamak yerine ayri bir ozet alani kullan.\n4) Aciklamayi title ile birebir ayni yapma; ikisi birbirini tamamlasin.", "Otomatik uretilmis binlerce sayfada bos birakmak, hepsine ayni kotu aciklamayi yazmaktan iyidir; o durumda yoksayabilirsin. Onemli acilis sayfalarinda yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_TOO_LONG",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Meta description 160 karakterden uzun. Fazlasi arama sonucunda uc noktayla kesilir ve sonda kalan eylem cagrisi kullaniciya hic gorunmez. Uzun aciklama ceza almaz, sadece etkisiz kalir.", "1) En onemli cumleyi basa al ve toplamda 155 karakteri asma.\n2) Tekrar eden marka adi veya slogan kismini cikar.\n3) Aciklamayi sablon uretiyorsa kirpma islemini kelime sinirinda yap; cumleyi ortasindan kesme.\n4) Sonucu arama sonucu onizlemesinde dogrula.", "Meta description cok uzun", "Gorunen sinir dile ve cihaza gore degistigi icin sinirin biraz uzerindeki aciklamalar yoksayilabilir. Ilk 155 karakter tek basina anlam tasimiyorsa yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_DUPLICATE",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Ayni title birden fazla sayfada kullaniliyor. Arama motoru hangi sayfanin hangi sorguya cevap oldugunu ayirt edemez; sayfalar birbirinin yerine gecerek gorunurlugu boler ve hicbiri tam guc kazanamaz. En sik sebep sablondan gelen sabit baslik ile sayfalanmis veya filtreli listelerdir.", "1) Cakisan sayfalari karsilastir; gercekten farkli iceriklerse her birine kendi basligini yaz.\n2) Sablonda basligi ayirt edici bir degiskenle uret: urun adi, kategori, sayfa numarasi.\n3) Sayfalanmis listelerde basliga \"- Sayfa 2\" gibi bir ek koy.\n4) Sayfalar ayni icerigin varyantiysa canonical ile asil surumu isaret et.", "Sayfalar gercekten ayni icerigin varyantiysa cozum baslik degistirmek degil canonical vermektir; canonical verildiyse yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada <title> etiketi bulunmuyor. Title, arama sonucundaki basligin ve tarayici sekmesinin ana kaynagidir; en guclu sayfa ici sinyallerden biridir. Etiket yoksa arama motoru basligi sayfa icinden veya gelen link metinlerinden kendi uretir ve sonuc cogu zaman anlamsiz cikar.", "1) <head> icine benzersiz bir <title> ekle; 30-60 karakter hedefle.\n2) Ana anahtar kelimeyi basta kullan, marka adini sona koy (orn. \"Kirmizi Kadin Bot Modelleri | Marka\").\n3) Sablonda title bos bir degiskene bagliysa yedek bir deger tanimla.\n4) Basligi yalnizca javascript ile yaziyorsan sunucu tarafinda da bas; ilk HTML yanitinda bulunmasi gerekir.", "Istisnasi yoktur: her HTML sayfasinda bir title bulunmalidir. Yoksaymak yerine sablonda yedek bir baslik tanimla." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_LONG",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Title 60 karakterden uzun. Arama sonucunda baslik karakter degil piksel genisligine gore kirpilir; sondaki kelimeler kullaniciya hic gorunmez ve cumle yarida kalinca guven duser. Uzunluk bir ceza sebebi degildir, ama gorunurluk kaybi gerceklesir.", "1) En onemli bilgiyi ilk 60 karaktere tasi.\n2) Marka adini kisalt ya da cikar; tekrar eden ekleri (\"en iyi\", \"ucuz\", yil bilgisi) temizle.\n3) Kategori kaliplarindaki gereksiz sabit onekleri sablondan kaldir.\n4) Yeni basligi arama sonucu onizlemesinde kirpilmadan gorundugu noktaya kadar kisalt.", "Title cok uzun", "Uzun urun adlarinda kirpilma kacinilmaz olabilir; ilk 60 karakter kendi basina anlam tasiyorsa yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_SHORT",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Title 30 karakterden kisa. Kisa basliklar sayfanin ne sundugunu anlatmaya yetmez, arama sonucunda tiklama oranini dusurur ve kelime cesitliligi olmadigi icin daha az sorguyla eslesir. Teknik bir hata degil, kacirilmis bir firsattir.", "1) Basliga sayfayi ayirt eden nitelik ekle: kategori, model, sehir, yil gibi.\n2) 30-60 karakter araligini hedefle; doldurma kelimesi degil gercek bilgi ekle.\n3) Ayni kaliptan uretilen diger sayfalarla birebir ayni olmadigindan emin ol.\n4) Anahtar kelime yigmadan, okunabilir tek bir cumle kur.", "Title cok kisa", "Marka ana sayfasi gibi tek kelimenin yeterli oldugu yerlerde kabul edilebilir; orada yoksayabilirsin. Kategori ve urun sayfalarinda yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "OG_TAGS_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "og:title, og:description veya og:image tanimli degil. Open Graph etiketleri, link sosyal aglarda ve mesajlasma uygulamalarinda paylasildiginda gorunen kartin icerigini belirler. Eksikse baslik ve gorsel rastgele secilir ya da kart tamamen bos gorunur; bu da paylasimdan gelen tiklamayi dusurur.", "1) <head> icine og:title, og:description, og:url ve og:image ekle.\n2) og:image icin en az 1200x630 piksel, mutlak URL'li bir gorsel kullan.\n3) og:type degerini icerik turune gore ver: website, article, product.\n4) Genis kart icin twitter:card=summary_large_image ekle ve paylasim onizleme araciyla dogrula.", "Arama siralamasina dogrudan etkisi yoktur; paylasim beklenmeyen ic sayfalarda yoksayabilirsin. Blog ve urun sayfalarinda yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ORPHAN_PAGE",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfaya hicbir ic link isaret etmiyor. Ic link olmadan sayfa yalnizca sitemap veya dis linklerle bulunabilir; kesfi yavaslar ve site icindeki onem sinyali sifira yakin kalir. Kullanici da menuden ya da icerik icinden bu sayfaya ulasamaz.", "1) Sayfayi konu olarak en yakin iceriklerden aciklayici anchor metniyle linkle.\n2) Kalici degerdeyse menu, kategori listesi veya \"ilgili icerik\" bloguna ekle.\n3) Bilincli olarak gizli tutuluyorsa kurali o URL icin yoksay.\n4) Sayfanin artik degeri yoksa kaldir ve 301 ile ilgili sayfaya yonlendir.", "Oksuz sayfa", "Kampanya acilis sayfalari gibi bilincli olarak menu disinda tutulan adreslerde beklenen durumdur; o URL icin yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "PAGE_NOT_IN_SITEMAP",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Dizinlenebilir bir sayfa sitemap'te listelenmiyor. Sitemap disindaki sayfa yine bulunabilir, ama kesfi tamamen ic linklere kalir; yeni yayinlanan iceriklerde bu gecikme gunlere yayilabilir. Ayrica sitemap kapsami ile gercek sayfa listesi arasindaki fark, Search Console raporlarini okumayi zorlastirir.", "1) Sayfa dizine girmeliyse sitemap'e ekle ve lastmod degerini gercek guncelleme tarihiyle doldur.\n2) Sitemap otomatik uretiliyorsa hangi filtrenin bu adresi eledigini kontrol et.\n3) Sayfa dizine girmemeliyse noindex ver ve sitemap disinda birak — iki sinyal birbiriyle celismesin.\n4) Guncellenen sitemap'i yeniden gonder.", "Sayfa sitemap disinda", "Dizine girmemesi gereken sayfalarda dogru cozum sitemap'e eklemek degil noindex vermektir; noindex verdiysen bu bulguyu yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_CHAIN",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfaya tek atlamada degil, birbirini izleyen birden fazla yonlendirmeyle ulasiliyor (orn. http > https > www > son adres). Her atlama ek gecikme demektir ve zincir uzadikca tarayicilarin takibi birakma ihtimali artar; tarama butcesi de gereksiz harcanir. Sinyal kaybi tek basina buyuk degildir, ama acilis suresi ve kesif verimliligi olculebilir sekilde duser.", "1) Zinciri bastan sona izle ve 200 donen son adresi belirle.\n2) Ic linkleri, menuyu, sitemap'i ve canonical'lari dogrudan son adrese guncelle.\n3) Sunucu/CDN kurallarini birlestir: protokol ve www tercihini tek kuralda coz, yol yonlendirmesini ondan sonra uygula.\n4) Duzeltmeden sonra adresi tekrar iste ve geriye tek bir 301 kaldigini dogrula.", "Yonlendirme zinciri", "Alan adi tasima ve protokol gecisi gibi donemlerde zincir gecici olarak normaldir. Gecis tamamlandiktan sonra kalici hale gelmisse yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_TARGET_INVALID",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Yonlendirmenin Location basligi http/https disi ya da ayristirilamayan bir degere isaret ediyor; hicbir istemci hedefe ulasamaz. Kullanici zincirin ortasinda kalir, arama motoru da adresi olu kabul eder. Sebep genellikle sablon hatasi, eksik alan adi ya da javascript:/tel: gibi yanlis semali bir degerdir.", "1) Yonlendirmeyi ureten kurali bul: sunucu konfigurasyonu, uygulama middleware'i veya CMS eklentisi.\n2) Location degerini gecerli bir http/https adresine cevir; goreli adres kullanacaksan koke gore yaz (/yol).\n3) Sablondan gelen bos degiskene karsi koruma ekle — hedef bos ise yonlendirme hic uretilmesin.\n4) curl -I ile yanit basligini isteyerek hedefin dogru dondugunu dogrula.", "Yonlendirme hedefi gecersiz", "Mesru bir istisnasi yoktur; her tetiklenmesi gercek bir hatadir. Yoksamak yerine yonlendirmeyi ureten kurali duzelt." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ROBOTS_NOINDEX",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfanin robots meta etiketinde ya da X-Robots-Tag yanit basliginda noindex var. Bu, arama motorlarina \"bu sayfayi dizine ekleme\" demektir; sayfa taransa bile sonuclarda hic gorunmez ve aldigi ic linklerin degeri bir yere aktarilmaz. En sik sebep, test ortamindan canliya tasinan tema veya CMS ayaridir.", "1) Sayfa kaynagindaki <meta name=\"robots\"> etiketini ve HTTP yanitindaki X-Robots-Tag basligini birlikte kontrol et; noindex ikisinden birinde olabilir.\n2) Sayfa dizine girmeliyse degeri index,follow yap veya etiketi tamamen kaldir.\n3) CMS/tema ayarlarinda \"arama motorlarini engelle\" secenegi aciksa kapat.\n4) Duzeltmeden sonra Search Console > URL Denetimi ile sayfayi yeniden tara.", "Sepet, hesap, odeme ve tesekkur sayfalarinda noindex bilincli ve dogru bir tercihtir. Trafik beklenen bir icerik sayfasinda ise yoksayma; bu, en agir hatalardan biridir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SCHEMA_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada schema.org isaretlemesi bulunamadi. Yapisal veri siralamayi dogrudan degistirmez, ama arama sonucunda yildiz, fiyat, sik sorulanlar, tarif gibi zengin gosterimleri mumkun kilar; bu da tiklama oranini artirir. Ayrica sayfanin ne hakkinda oldugunu makineye net soyler.", "1) Sayfanin turune uygun tipi sec: Article, Product, FAQPage, LocalBusiness, BreadcrumbList.\n2) JSON-LD olarak <script type=\"application/ld+json\"> icinde ekle; mikro veri yerine JSON-LD tercih et.\n3) Yalnizca sayfada gercekten gorunen bilgiyi isaretle; gorunmeyen veri politika ihlalidir.\n4) Zengin Sonuc Testi ile dogrula ve Search Console'daki gelismeler raporunu izle.", "Yapisal veri yok", "Hicbir zengin sonuc tipine uymayan sade bilgi sayfalarinda eksikligi sorun degildir; orada yoksayabilirsin. Urun, tarif, etkinlik ve SSS sayfalarinda yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SERVER_ERROR_5XX",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa 5xx donuyor ya da hic getirilemedi (baglanti hatasi, zaman asimi). Bu sunucu kaynakli bir arizadir; arama motoru 5xx gorunce once tarama hizini dusurur, hata surerse sayfayi dizinden cikarir. 4xx'ten daha acildir, cunku sorun cogu zaman tek sayfada degil altyapinin tamaminda olur.", "1) Sunucu ve uygulama loglarindan kok nedeni bul: istisna, veritabani baglantisi, bellek veya zaman asimi.\n2) Planli bakimsa 500 yerine 503 don ve Retry-After basligi ekle; boylece dizinden dusme riski azalir.\n3) Yavas yanit veren sayfalarda zaman asimi limitlerini ve sorgu maliyetini gozden gecir.\n4) Duzeltmeden sonra ayni adresi yeniden tara ve Search Console > Tarama Istatistikleri'nde hata oraninin dustugunu dogrula.", "Sunucu hatasi", "Planli bakim penceresinde alinmis bir olcum yanlis pozitif olabilir. Bu durumda dogru davranis bulguyu yoksaymak degil, bakim sirasinda 500 yerine 503 donmektir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SITEMAP_MISSING",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sitede okunabilir bir XML sitemap bulunamadi: robots.txt'te Sitemap satiri yok ve bilinen adreslerde gecerli bir dosya yanit vermiyor. Sitemap zorunlu degildir ama yeni ve derindeki sayfalarin kesfini hizlandirir, son guncelleme tarihini bildirir. Ozellikle ic linki zayif veya cok sayfali sitelerde fark buyuktur.", "1) Yalnizca dizine girmesini istedigin, 200 donen ve canonical'i kendine bakan adresleri iceren bir sitemap.xml uret.\n2) 50.000 URL veya 50 MB sinirini asiyorsan sitemap index dosyasi kullan.\n3) robots.txt'e mutlak adresle bildir: Sitemap: https://site.com/sitemap.xml\n4) Search Console > Site Haritalari ekranindan gonder ve okundugunu dogrula.", "Menuden her sayfaya erisilen birkac sayfalik sitelerde etkisi kucuktur; orada yoksayilabilir. Yuzlerce sayfali veya sik icerik eklenen sitelerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "THIN_CONTENT",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa metni 300 kelimenin altinda. Kelime sayisi tek basina bir siralama faktoru degildir, ancak bu uzunluk cogu sorguda kullanicinin sorusunu karsilamaz ve sayfa dusuk degerli olarak degerlendirilir. Cok sayida ince sayfa, sitenin genel kalite algisini da asagi ceker.", "1) Sayfanin hedefledigi soruyu belirle ve cevabi eksiksiz ver: kapsam, ornek, sik sorulanlar.\n2) Baska sayfalardan kopyalanmis metin yerine ozgun icerik uret; uzunluk tek basina yeterli degildir.\n3) Ayni konuyu bolen cok sayida ince sayfayi tek guclu sayfada birlestir, eskilerini 301 ile yonlendir.\n4) Sayfa dogasi geregi kisaysa kurali site genelinde yoksay.", "Zayif icerik", "Iletisim, giris, tesekkur gibi islevsel sayfalarda kisa metin normaldir; bunlarda kurali site genelinde yoksaymak dogrudur. Arama trafigi hedefleyen iceriklerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "TOO_DEEP",
                columns: new[] { "description_tr", "how_to_fix_tr", "title_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa kok sayfadan 4 tiklamadan uzakta. Derin sayfalar hem kullanici hem tarayici tarafindan daha az ziyaret edilir; buyuk sitelerde tarama butcesi ust seviyelerde tukenir ve derindeki icerik gec guncellenir. Derinlik dogrudan bir ceza degildir, ama onemli sayfalarin derinde kalmasi gorunurluk kaybidir.", "1) Sayfanin gercekten onemli olup olmadigina karar ver; onemliyse ust seviyeden link ver.\n2) Kategori/etiket sayfalarindan veya \"ilgili icerik\" bloklarindan kisayol linkleri ekle.\n3) Uzun sayfalama zincirlerini filtre veya kategori kirilimiyla kisalt.\n4) Menuyu sisirmeden, konu kumelerini tek bir hub sayfasinda topla.", "Cok derin sayfa", "Arsiv, eski sayfalama ve dusuk oncelikli listelerde derinlik dogaldir; orada yoksayabilirsin. Donusum getiren sayfalarda yoksayma." });
        }
    }
}
