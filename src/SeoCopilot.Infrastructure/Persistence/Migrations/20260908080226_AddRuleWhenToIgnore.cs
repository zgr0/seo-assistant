using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleWhenToIgnore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "when_to_ignore_tr",
                table: "rules",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BLOCKED_BY_ROBOTS_TXT",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "robots.txt icindeki bir Disallow kurali, taranmasi gereken ic adresleri kapatiyor. Engellenen adresi arama motoru indiremez; icerigi okunamadigi icin sayfa ya hic dizine girmez ya da basliksiz-ozetsiz bos bir kayit olarak listelenir. Dikkat: robots.txt engeli noindex ile ayni sey degildir — engellenen sayfa dis linkler uzerinden yine de dizine dusebilir.", "Yonetim paneli, ic arama sonuclari ve tekrar eden filtre adresleri icin engel dogrudur; bu adreslerde bulguyu yoksayabilirsin. Icerik sayfalarinda ise dogrudan trafik kaybi demektir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_INTERNAL_LINK",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Bir ic link 4xx/5xx donen sayfaya gidiyor. Kullanici akisin ortasinda hata sayfasina duser; tarama acisindan da bosa harcanan bir istek olur ve linkle aktarilacak deger kaybolur. Kirik ic link cogunlukla yeniden yapilandirma, silinen urun veya elle yazilmis yanlis adresten kaynaklanir.", "Kimlik dogrulama arkasindaki sayfalar tarayiciya 401/403 dondugu icin yanlis pozitif uretebilir; bunlari yoksayabilirsin. Herkese acik hedeflerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_PAGE_4XX",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa 4xx (cogunlukla 404 veya 410) donuyor; icerik sunucuda yok. Kullanici hata sayfasina duser, arama motoru da adresi dizinden cikarir. Sayfaya ic link veya dis backlink geliyorsa o baglantilarin biriktirdigi deger de bosa gider.", "Silinmis kampanya adresleri icin 410 bilincli bir tercih olabilir. Ancak adres hala bir yerden linkleniyorsa yoksayma — once linki temizlemek gerekir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_MISSING",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada rel=canonical etiketi tanimli degil. Canonical, ayni icerige birden fazla adresten ulasildiginda (izleme parametreleri, siralama/filtre, http-https ve www farki) hangisinin asil kabul edilecegini soyler. Etiket yoksa asil adresi arama motoru kendi secer ve beklemedigin bir varyant dizine girebilir.", "Parametresiz, tek adresli kucuk sitelerde etkisi sinirlidir — bu yuzden onemi dusuk tutulur ve site genelinde yoksayilabilir. Filtre veya izleme parametresi uretilen sitelerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_POINTS_ELSEWHERE",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "rel=canonical sayfanin kendi adresini degil baska bir adresi gosteriyor. Bu, \"beni dizine ekleme, asil olan su\" demektir; niyet buysa dogru, degilse sayfa sessizce aramadan silinir ve bunu hicbir hata mesaji haber vermez. En sik sebep sablonda sabitlenmis canonical veya cogaltilmis sayfa duzenidir.", "Yinelenen varyantlarda (utm parametreli, filtreli veya yazdirma adresleri) beklenen davranistir; oralarda yoksayabilirsin. Asil surum olmasi gereken bir sayfada yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CLS_POOR",
                column: "when_to_ignore_tr",
                value: "Olcum tek seferlik laboratuvar kosusudur; cerez bandi gibi tek seferlik ogeler sonucu sisirebilir. Gercek kullanici verisiyle karsilastirmadan yoksayma.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "DUPLICATE_CONTENT",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Ayni icerik parmak izi birden fazla adreste goruluyor. Arama motoru bunlardan yalnizca birini secer; secim senin istedigin adres olmayabilir ve gelen linklerin degeri varyantlar arasinda bolunur. Cogunlukla parametreli adresler, http-https ve www varyantlari, yazdirma surumleri veya kopyalanmis kategori sayfalari sebep olur.", "Sablonu ayni ama govdesi gercekten farkli sayfalarda yanlis pozitif olabilir; govdeyi karsilastirip oyleyse yoksay." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "GENERIC_ANCHOR_TEXT",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Ic linklerde \"buraya tiklayin\", \"devamini oku\", \"detaylar\" gibi hedefi anlatmayan anchor metinleri var. Anchor metni, hedef sayfanin konusunu bildiren en guclu ic sinyallerden biridir; genel ifadeler bu bilgiyi hic tasimaz. Ekran okuyucu kullanicilari link listesinde yalnizca anchor metnini duyar, hedefsiz metinler gezinmeyi zorlastirir.", "Kart ve gorsel duzenlerinde kisa metin gerekiyorsa aria-label ile telafi edilebilir; aria-label eklediysen yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MISSING",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada H1 basligi bulunmuyor. H1, icerik icin en ust seviye basliktir; sayfanin konusunu hem kullaniciya hem ekran okuyucuya ilk o bildirir. Yoksa icerik hiyerarsisi bastan kopuk olur ve sayfanin ana konusu zayif sinyallenir.", "Tasarim geregi buyuk bir baslik istemiyorsan dogru yol H1'i kaldirmak degil CSS ile kucultmektir; bu yuzden yoksamak nadiren dogrudur." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MULTIPLE",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada birden cok H1 var. HTML5 bolum yapisinda teknik olarak gecerlidir, ancak pratikte sayfanin ana konusu bulaniklasir ve ekran okuyucuda gezinme zorlasir. En sik sebep, sablonun hem site adini hem sayfa basligini H1 olarak basmasidir.", "Tek sayfada birbirinden bagimsiz birden fazla makale varsa (akis veya arsiv duzeni) kabul edilebilir; orada yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "HEADING_HIERARCHY_BROKEN",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Baslik seviyeleri sirayla ilerlemiyor; en az bir seviye atlanmis (orn. h2'den sonra h4). Ekran okuyucu kullanicilari icerikte baslik seviyelerine gore gezinir; atlama olunca yapinin bir parcasi eksikmis gibi algilanir. Neredeyse her zaman baslik etiketinin anlam yerine yazi tipi boyutu icin secilmesinden kaynaklanir.", "Arama motorlari acisindan etkisi kucuktur, asil maliyet erisilebilirliktedir. Erisilebilirlik onceligin degilse dusuk oncelikle ele alabilir ya da yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_MISSING_ALT",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada alt niteligi tasimayan <img> etiketleri var. Alt metni, gorsel yuklenmediginde gosterilen ve ekran okuyucunun sesli okudugu metindir; ayrica Gorsel Arama'da sayfanin bulunmasini saglar. Eksik alt, erisilebilirlik acisindan dogrudan bir engeldir.", "Dekoratif gorsellerde dogru cozum alt=\"\" yazmaktir — niteligi tamamen kaldirmak degil. Bos alt kullandiysan yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_TOO_LARGE",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfadaki bir veya daha fazla gorsel 200 KB'i asiyor. Buyuk gorseller cogu sayfada en gec yuklenen parcadir ve LCP olcumunu dogrudan belirler; mobil baglantida fark saniyelerle olculur. Ayrica kullanicinin veri kotasini gereksiz harcar.", "Yuksek cozunurlugun urunun kendisi oldugu galeri ve portfolyo sayfalarinda buyuk dosyalar kabul edilebilir. Liste ve kapak gorsellerinde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INP_POOR",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Interaction to Next Paint 500 ms'nin uzerinde. INP, kullanicinin tikladiktan veya yazdiktan sonra ekranda gorsel bir karsilik gormesi icin gecen sureyi olcer; 200 ms alti iyi, 500 ms ustu kotu kabul edilir. Yuksek INP genellikle ana is parcacigini uzun sure mesgul eden JavaScript'ten kaynaklanir.", "Neredeyse hic etkilesim iceremeyen tanitim sayfalarinda olcumun guvenilirligi dusuktur; orada yoksayabilirsin. Form ve filtre iceren sayfalarda yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INVALID_STRUCTURED_DATA",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada JSON-LD var ama bicimi bozuk: gecersiz JSON ya da tek script etiketi icinde birden fazla kok nesne. Bu durumda isaretlemenin tamami yok sayilir, yani emek harcanmis ama hicbir zengin sonuc kazanci olusmaz. En sik sebepler kacisi yapilmamis tirnak, sondaki fazla virgul ve sablonun yan yana bastigi iki nesnedir.", "Bicimsel bir hata oldugu icin mesru istisnasi yoktur. Yoksamak, calismayan isaretlemeyi sayfada birakmak demektir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LANG_ATTR_MISSING",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "<html> etiketinde lang niteligi bulunmuyor. Bu nitelik ekran okuyuculara hangi dilde okuyacagini, tarayicilara ceviri onerip onermeyecegini soyler. Eksikse ekran okuyucu yanlis telaffuzla okur; erisilebilirlik acisindan somut bir sorundur.", "Arama motoru dili icerikten de cikarabildigi icin siralama etkisi sinirlidir, ama duzeltmesi tek satirdir — yoksamak icin iyi bir sebep nadiren bulunur." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LCP_POOR",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Largest Contentful Paint 4 saniyenin uzerinde. LCP, ekrandaki en buyuk icerik ogesinin (genellikle kapak gorseli veya baslik blogu) gorunur olma suresidir ve \"sayfa acildi mi\" hissinin ana olcusudur. 2,5 sn alti iyi, 4 sn ustu kotu kabul edilir.", "Olcum PageSpeed Insights'in tek seferlik laboratuvar kosusundan gelir; ag dalgalanmasi payi vardir. Tek bir olcume dayanip yoksamak yerine olcumu tekrarla." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_DUPLICATE",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Ayni meta description birden fazla sayfada kullaniliyor. Aramada yan yana cikan sonuclar birbirinin ayni gorunur ve kullanici hangisine girecegini secemez; aciklamanin tiklama artirici islevi tamamen kaybolur. Genellikle sablondaki sabit metinden kaynaklanir.", "Sayfalar ayni icerigin varyantiysa asil sorun aciklama degil eksik canonical'dir; canonical verildiyse yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_MISSING",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada meta description yok. Bu etiket dogrudan bir siralama faktoru degildir ama arama sonucundaki ozet metnini belirler; yoksa metin sayfa icinden secilir ve cogu zaman menu, cerez uyarisi veya yasal metin parcasi one cikar. Iyi yazilmis bir ozet, siralama degismeden tiklama oranini artirir.", "Otomatik uretilmis binlerce sayfada bos birakmak, hepsine ayni kotu aciklamayi yazmaktan iyidir; o durumda yoksayabilirsin. Onemli acilis sayfalarinda yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_TOO_LONG",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Meta description 160 karakterden uzun. Fazlasi arama sonucunda uc noktayla kesilir ve sonda kalan eylem cagrisi kullaniciya hic gorunmez. Uzun aciklama ceza almaz, sadece etkisiz kalir.", "Gorunen sinir dile ve cihaza gore degistigi icin sinirin biraz uzerindeki aciklamalar yoksayilabilir. Ilk 155 karakter tek basina anlam tasimiyorsa yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_DUPLICATE",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Ayni title birden fazla sayfada kullaniliyor. Arama motoru hangi sayfanin hangi sorguya cevap oldugunu ayirt edemez; sayfalar birbirinin yerine gecerek gorunurlugu boler ve hicbiri tam guc kazanamaz. En sik sebep sablondan gelen sabit baslik ile sayfalanmis veya filtreli listelerdir.", "Sayfalar gercekten ayni icerigin varyantiysa cozum baslik degistirmek degil canonical vermektir; canonical verildiyse yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_MISSING",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada <title> etiketi bulunmuyor. Title, arama sonucundaki basligin ve tarayici sekmesinin ana kaynagidir; en guclu sayfa ici sinyallerden biridir. Etiket yoksa arama motoru basligi sayfa icinden veya gelen link metinlerinden kendi uretir ve sonuc cogu zaman anlamsiz cikar.", "Istisnasi yoktur: her HTML sayfasinda bir title bulunmalidir. Yoksaymak yerine sablonda yedek bir baslik tanimla." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_LONG",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Title 60 karakterden uzun. Arama sonucunda baslik karakter degil piksel genisligine gore kirpilir; sondaki kelimeler kullaniciya hic gorunmez ve cumle yarida kalinca guven duser. Uzunluk bir ceza sebebi degildir, ama gorunurluk kaybi gerceklesir.", "Uzun urun adlarinda kirpilma kacinilmaz olabilir; ilk 60 karakter kendi basina anlam tasiyorsa yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_SHORT",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Title 30 karakterden kisa. Kisa basliklar sayfanin ne sundugunu anlatmaya yetmez, arama sonucunda tiklama oranini dusurur ve kelime cesitliligi olmadigi icin daha az sorguyla eslesir. Teknik bir hata degil, kacirilmis bir firsattir.", "Marka ana sayfasi gibi tek kelimenin yeterli oldugu yerlerde kabul edilebilir; orada yoksayabilirsin. Kategori ve urun sayfalarinda yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "OG_TAGS_MISSING",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "og:title, og:description veya og:image tanimli degil. Open Graph etiketleri, link sosyal aglarda ve mesajlasma uygulamalarinda paylasildiginda gorunen kartin icerigini belirler. Eksikse baslik ve gorsel rastgele secilir ya da kart tamamen bos gorunur; bu da paylasimdan gelen tiklamayi dusurur.", "Arama siralamasina dogrudan etkisi yoktur; paylasim beklenmeyen ic sayfalarda yoksayabilirsin. Blog ve urun sayfalarinda yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ORPHAN_PAGE",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfaya hicbir ic link isaret etmiyor. Ic link olmadan sayfa yalnizca sitemap veya dis linklerle bulunabilir; kesfi yavaslar ve site icindeki onem sinyali sifira yakin kalir. Kullanici da menuden ya da icerik icinden bu sayfaya ulasamaz.", "Kampanya acilis sayfalari gibi bilincli olarak menu disinda tutulan adreslerde beklenen durumdur; o URL icin yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "PAGE_NOT_IN_SITEMAP",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Dizinlenebilir bir sayfa sitemap'te listelenmiyor. Sitemap disindaki sayfa yine bulunabilir, ama kesfi tamamen ic linklere kalir; yeni yayinlanan iceriklerde bu gecikme gunlere yayilabilir. Ayrica sitemap kapsami ile gercek sayfa listesi arasindaki fark, Search Console raporlarini okumayi zorlastirir.", "Dizine girmemesi gereken sayfalarda dogru cozum sitemap'e eklemek degil noindex vermektir; noindex verdiysen bu bulguyu yoksayabilirsin." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_CHAIN",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfaya tek atlamada degil, birbirini izleyen birden fazla yonlendirmeyle ulasiliyor (orn. http > https > www > son adres). Her atlama ek gecikme demektir ve zincir uzadikca tarayicilarin takibi birakma ihtimali artar; tarama butcesi de gereksiz harcanir. Sinyal kaybi tek basina buyuk degildir, ama acilis suresi ve kesif verimliligi olculebilir sekilde duser.", "Alan adi tasima ve protokol gecisi gibi donemlerde zincir gecici olarak normaldir. Gecis tamamlandiktan sonra kalici hale gelmisse yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_TARGET_INVALID",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Yonlendirmenin Location basligi http/https disi ya da ayristirilamayan bir degere isaret ediyor; hicbir istemci hedefe ulasamaz. Kullanici zincirin ortasinda kalir, arama motoru da adresi olu kabul eder. Sebep genellikle sablon hatasi, eksik alan adi ya da javascript:/tel: gibi yanlis semali bir degerdir.", "Mesru bir istisnasi yoktur; her tetiklenmesi gercek bir hatadir. Yoksamak yerine yonlendirmeyi ureten kurali duzelt." });

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
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfada schema.org isaretlemesi bulunamadi. Yapisal veri siralamayi dogrudan degistirmez, ama arama sonucunda yildiz, fiyat, sik sorulanlar, tarif gibi zengin gosterimleri mumkun kilar; bu da tiklama oranini artirir. Ayrica sayfanin ne hakkinda oldugunu makineye net soyler.", "Hicbir zengin sonuc tipine uymayan sade bilgi sayfalarinda eksikligi sorun degildir; orada yoksayabilirsin. Urun, tarif, etkinlik ve SSS sayfalarinda yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SERVER_ERROR_5XX",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa 5xx donuyor ya da hic getirilemedi (baglanti hatasi, zaman asimi). Bu sunucu kaynakli bir arizadir; arama motoru 5xx gorunce once tarama hizini dusurur, hata surerse sayfayi dizinden cikarir. 4xx'ten daha acildir, cunku sorun cogu zaman tek sayfada degil altyapinin tamaminda olur.", "Planli bakim penceresinde alinmis bir olcum yanlis pozitif olabilir. Bu durumda dogru davranis bulguyu yoksaymak degil, bakim sirasinda 500 yerine 503 donmektir." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SITEMAP_MISSING",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sitede okunabilir bir XML sitemap bulunamadi: robots.txt'te Sitemap satiri yok ve bilinen adreslerde gecerli bir dosya yanit vermiyor. Sitemap zorunlu degildir ama yeni ve derindeki sayfalarin kesfini hizlandirir, son guncelleme tarihini bildirir. Ozellikle ic linki zayif veya cok sayfali sitelerde fark buyuktur.", "Menuden her sayfaya erisilen birkac sayfalik sitelerde etkisi kucuktur; orada yoksayilabilir. Yuzlerce sayfali veya sik icerik eklenen sitelerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "THIN_CONTENT",
                columns: new[] { "description_tr", "how_to_fix_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa metni 300 kelimenin altinda. Kelime sayisi tek basina bir siralama faktoru degildir, ancak bu uzunluk cogu sorguda kullanicinin sorusunu karsilamaz ve sayfa dusuk degerli olarak degerlendirilir. Cok sayida ince sayfa, sitenin genel kalite algisini da asagi ceker.", "1) Sayfanin hedefledigi soruyu belirle ve cevabi eksiksiz ver: kapsam, ornek, sik sorulanlar.\n2) Baska sayfalardan kopyalanmis metin yerine ozgun icerik uret; uzunluk tek basina yeterli degildir.\n3) Ayni konuyu bolen cok sayida ince sayfayi tek guclu sayfada birlestir, eskilerini 301 ile yonlendir.\n4) Sayfa dogasi geregi kisaysa kurali site genelinde yoksay.", "Iletisim, giris, tesekkur gibi islevsel sayfalarda kisa metin normaldir; bunlarda kurali site genelinde yoksaymak dogrudur. Arama trafigi hedefleyen iceriklerde yoksayma." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "TOO_DEEP",
                columns: new[] { "description_tr", "when_to_ignore_tr" },
                values: new object[] { "Sayfa kok sayfadan 4 tiklamadan uzakta. Derin sayfalar hem kullanici hem tarayici tarafindan daha az ziyaret edilir; buyuk sitelerde tarama butcesi ust seviyelerde tukenir ve derindeki icerik gec guncellenir. Derinlik dogrudan bir ceza degildir, ama onemli sayfalarin derinde kalmasi gorunurluk kaybidir.", "Arsiv, eski sayfalama ve dusuk oncelikli listelerde derinlik dogaldir; orada yoksayabilirsin. Donusum getiren sayfalarda yoksayma." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "when_to_ignore_tr",
                table: "rules");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BLOCKED_BY_ROBOTS_TXT",
                column: "description_tr",
                value: "robots.txt icindeki bir Disallow kurali, taranmasi gereken ic adresleri kapatiyor. Engellenen adresi arama motoru indiremez; icerigi okunamadigi icin sayfa ya hic dizine girmez ya da basliksiz-ozetsiz bos bir kayit olarak listelenir. Yonetim paneli, ic arama sonuclari ve tekrar eden filtre adresleri icin engel dogrudur. Dikkat: robots.txt engeli noindex ile ayni sey degildir — engellenen sayfa dis linkler uzerinden yine de dizine dusebilir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_INTERNAL_LINK",
                column: "description_tr",
                value: "Bir ic link 4xx/5xx donen sayfaya gidiyor. Kullanici akisin ortasinda hata sayfasina duser; tarama acisindan da bosa harcanan bir istek olur ve linkle aktarilacak deger kaybolur. Kirik ic link cogunlukla yeniden yapilandirma, silinen urun veya elle yazilmis yanlis adresten kaynaklanir. Bilincli olarak 404 birakilan adresler varsa cozum onlara link vermemektir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "BROKEN_PAGE_4XX",
                column: "description_tr",
                value: "Sayfa 4xx (cogunlukla 404 veya 410) donuyor; icerik sunucuda yok. Kullanici hata sayfasina duser, arama motoru da adresi dizinden cikarir. Sayfaya ic link veya dis backlink geliyorsa o baglantilarin biriktirdigi deger de bosa gider. Silinmis kampanya adresleri icin 410 bilincli bir tercih olabilir, ancak adres hala bir yerden linkleniyorsa mutlaka ele alinmalidir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_MISSING",
                column: "description_tr",
                value: "Sayfada rel=canonical etiketi tanimli degil. Canonical, ayni icerige birden fazla adresten ulasildiginda (izleme parametreleri, siralama/filtre, http-https ve www farki) hangisinin asil kabul edilecegini soyler. Etiket yoksa asil adresi arama motoru kendi secer ve beklemedigin bir varyant dizine girebilir. Parametresiz, tek adresli kucuk sitelerde etkisi sinirlidir — bu yuzden onemi dusuk tutulur.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "CANONICAL_POINTS_ELSEWHERE",
                column: "description_tr",
                value: "rel=canonical sayfanin kendi adresini degil baska bir adresi gosteriyor. Bu, \"beni dizine ekleme, asil olan su\" demektir; niyet buysa dogru, degilse sayfa sessizce aramadan silinir ve bunu hicbir hata mesaji haber vermez. En sik sebep sablonda sabitlenmis canonical veya cogaltilmis sayfa duzenidir. Yinelenen varyantlarda (utm parametreli, filtreli adresler) beklenen davranistir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "DUPLICATE_CONTENT",
                column: "description_tr",
                value: "Ayni icerik parmak izi birden fazla adreste goruluyor. Arama motoru bunlardan yalnizca birini secer; secim senin istedigin adres olmayabilir ve gelen linklerin degeri varyantlar arasinda bolunur. Cogunlukla parametreli adresler, http-https ve www varyantlari, yazdirma surumleri veya kopyalanmis kategori sayfalari sebep olur. Sablonu ayni, govdesi farkli sayfalarda yanlis pozitif olabilir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "GENERIC_ANCHOR_TEXT",
                column: "description_tr",
                value: "Ic linklerde \"buraya tiklayin\", \"devamini oku\", \"detaylar\" gibi hedefi anlatmayan anchor metinleri var. Anchor metni, hedef sayfanin konusunu bildiren en guclu ic sinyallerden biridir; genel ifadeler bu bilgiyi hic tasimaz. Ekran okuyucu kullanicilari link listesinde yalnizca anchor metnini duyar, hedefsiz metinler gezinmeyi zorlastirir. Kart ve gorsel duzenlerinde kisa metin gerekiyorsa aria-label ile telafi edilebilir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MISSING",
                column: "description_tr",
                value: "Sayfada H1 basligi bulunmuyor. H1, icerik icin en ust seviye basliktir; sayfanin konusunu hem kullaniciya hem ekran okuyucuya ilk o bildirir. Yoksa icerik hiyerarsisi bastan kopuk olur ve sayfanin ana konusu zayif sinyallenir. Tasarim geregi buyuk bir baslik istemiyorsan dogru yol H1'i kaldirmak degil, CSS ile kucultmektir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "H1_MULTIPLE",
                column: "description_tr",
                value: "Sayfada birden cok H1 var. HTML5 bolum yapisinda teknik olarak gecerlidir, ancak pratikte sayfanin ana konusu bulaniklasir ve ekran okuyucuda gezinme zorlasir. En sik sebep, sablonun hem site adini hem sayfa basligini H1 olarak basmasidir. Tek sayfada birbirinden bagimsiz birden fazla makale varsa kabul edilebilir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "HEADING_HIERARCHY_BROKEN",
                column: "description_tr",
                value: "Baslik seviyeleri sirayla ilerlemiyor; en az bir seviye atlanmis (orn. h2'den sonra h4). Ekran okuyucu kullanicilari icerikte baslik seviyelerine gore gezinir; atlama olunca yapinin bir parcasi eksikmis gibi algilanir. Arama motorlari acisindan etkisi kucuktur, asil maliyet erisilebilirliktedir. Neredeyse her zaman baslik etiketinin anlam yerine yazi tipi boyutu icin secilmesinden kaynaklanir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_MISSING_ALT",
                column: "description_tr",
                value: "Sayfada alt niteligi tasimayan <img> etiketleri var. Alt metni, gorsel yuklenmediginde gosterilen ve ekran okuyucunun sesli okudugu metindir; ayrica Gorsel Arama'da sayfanin bulunmasini saglar. Eksik alt, erisilebilirlik acisindan dogrudan bir engeldir. Dekoratif gorsellerde alt bos birakilmalidir (alt=\"\") — nitelik tamamen kaldirilmamalidir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "IMAGE_TOO_LARGE",
                column: "description_tr",
                value: "Sayfadaki bir veya daha fazla gorsel 200 KB'i asiyor. Buyuk gorseller cogu sayfada en gec yuklenen parcadir ve LCP olcumunu dogrudan belirler; mobil baglantida fark saniyelerle olculur. Ayrica kullanicinin veri kotasini gereksiz harcar. Yuksek cozunurluk gerektiren galerilerde tek tek buyuk dosyalar kabul edilebilir, ama liste ve kapak gorsellerinde olmamalidir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INP_POOR",
                column: "description_tr",
                value: "Interaction to Next Paint 500 ms'nin uzerinde. INP, kullanicinin tikladiktan veya yazdiktan sonra ekranda gorsel bir karsilik gormesi icin gecen sureyi olcer; 200 ms alti iyi, 500 ms ustu kotu kabul edilir. Yuksek INP genellikle ana is parcacigini uzun sure mesgul eden JavaScript'ten kaynaklanir. Az etkilesimli tanitim sayfalarinda olcumun guvenilirligi dusuk olabilir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "INVALID_STRUCTURED_DATA",
                column: "description_tr",
                value: "Sayfada JSON-LD var ama bicimi bozuk: gecersiz JSON ya da tek script etiketi icinde birden fazla kok nesne. Bu durumda isaretlemenin tamami yok sayilir, yani emek harcanmis ama hicbir zengin sonuc kazanci olusmaz. En sik sebepler kacisi yapilmamis tirnak, sondaki fazla virgul ve sablonun yan yana bastigi iki nesnedir. Bicimsel bir hata oldugu icin istisnasi yoktur.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LANG_ATTR_MISSING",
                column: "description_tr",
                value: "<html> etiketinde lang niteligi bulunmuyor. Bu nitelik ekran okuyuculara hangi dilde okuyacagini, tarayicilara ceviri onerip onermeyecegini soyler. Eksikse ekran okuyucu yanlis telaffuzla okur; erisilebilirlik acisindan somut bir sorundur. Arama motoru dili icerikten de cikarabildigi icin siralama etkisi sinirlidir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "LCP_POOR",
                column: "description_tr",
                value: "Largest Contentful Paint 4 saniyenin uzerinde. LCP, ekrandaki en buyuk icerik ogesinin (genellikle kapak gorseli veya baslik blogu) gorunur olma suresidir ve \"sayfa acildi mi\" hissinin ana olcusudur. 2,5 sn alti iyi, 4 sn ustu kotu kabul edilir. Olcum PageSpeed Insights uzerinden alinir; tek seferlik bir olcumde ag dalgalanmasi payi olabilir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_DUPLICATE",
                column: "description_tr",
                value: "Ayni meta description birden fazla sayfada kullaniliyor. Aramada yan yana cikan sonuclar birbirinin ayni gorunur ve kullanici hangisine girecegini secemez; aciklamanin tiklama artirici islevi tamamen kaybolur. Genellikle sablondaki sabit metinden kaynaklanir. Sayfalar ayni icerigin varyantiysa asil sorun aciklama degil, eksik canonical'dir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_MISSING",
                column: "description_tr",
                value: "Sayfada meta description yok. Bu etiket dogrudan bir siralama faktoru degildir ama arama sonucundaki ozet metnini belirler; yoksa metin sayfa icinden secilir ve cogu zaman menu, cerez uyarisi veya yasal metin parcasi one cikar. Iyi yazilmis bir ozet, siralama degismeden tiklama oranini artirir. Otomatik uretilmis binlerce sayfada bos birakmak, hepsine ayni kotu aciklamayi yazmaktan daha iyidir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_DESC_TOO_LONG",
                column: "description_tr",
                value: "Meta description 160 karakterden uzun. Fazlasi arama sonucunda uc noktayla kesilir ve sonda kalan eylem cagrisi kullaniciya hic gorunmez. Uzun aciklama ceza almaz, sadece etkisiz kalir. Gorunen sinir dile ve cihaza gore degistigi icin hedef karakter sayisini biraz altta tutmak guvenlidir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_DUPLICATE",
                column: "description_tr",
                value: "Ayni title birden fazla sayfada kullaniliyor. Arama motoru hangi sayfanin hangi sorguya cevap oldugunu ayirt edemez; sayfalar birbirinin yerine gecerek gorunurlugu boler ve hicbiri tam guc kazanamaz. En sik sebep sablondan gelen sabit baslik ile sayfalanmis veya filtreli listelerdir. Sayfalar gercekten ayni icerigin varyantiysa cozum baslik degistirmek degil canonical vermektir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_MISSING",
                column: "description_tr",
                value: "Sayfada <title> etiketi bulunmuyor. Title, arama sonucundaki basligin ve tarayici sekmesinin ana kaynagidir; en guclu sayfa ici sinyallerden biridir. Etiket yoksa arama motoru basligi sayfa icinden veya gelen link metinlerinden kendi uretir ve sonuc cogu zaman anlamsiz cikar. Bu kuralin istisnasi yoktur: her HTML sayfasinda bir title bulunmalidir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_LONG",
                column: "description_tr",
                value: "Title 60 karakterden uzun. Arama sonucunda baslik karakter degil piksel genisligine gore kirpilir; sondaki kelimeler kullaniciya hic gorunmez ve cumle yarida kalinca guven duser. Uzunluk bir ceza sebebi degildir, ama gorunurluk kaybi gerceklesir. Uzun urun adlarinda kirpilma kacinilmaz olabilir; onemli olan ilk 60 karakterin kendi basina anlam tasimasidir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "META_TITLE_TOO_SHORT",
                column: "description_tr",
                value: "Title 30 karakterden kisa. Kisa basliklar sayfanin ne sundugunu anlatmaya yetmez, arama sonucunda tiklama oranini dusurur ve kelime cesitliligi olmadigi icin daha az sorguyla eslesir. Teknik bir hata degil, kacirilmis bir firsattir. Yalnizca marka ana sayfasi gibi tek kelimenin yeterli oldugu yerlerde kabul edilebilir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "OG_TAGS_MISSING",
                column: "description_tr",
                value: "og:title, og:description veya og:image tanimli degil. Open Graph etiketleri, link sosyal aglarda ve mesajlasma uygulamalarinda paylasildiginda gorunen kartin icerigini belirler. Eksikse baslik ve gorsel rastgele secilir ya da kart tamamen bos gorunur; bu da paylasimdan gelen tiklamayi dusurur. Arama siralamasina dogrudan etkisi yoktur.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ORPHAN_PAGE",
                column: "description_tr",
                value: "Sayfaya hicbir ic link isaret etmiyor. Ic link olmadan sayfa yalnizca sitemap veya dis linklerle bulunabilir; kesfi yavaslar ve site icindeki onem sinyali sifira yakin kalir. Kullanici da menuden ya da icerik icinden bu sayfaya ulasamaz. Kampanya acilis sayfalari gibi bilincli olarak menu disinda tutulan adreslerde beklenen durumdur.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "PAGE_NOT_IN_SITEMAP",
                column: "description_tr",
                value: "Dizinlenebilir bir sayfa sitemap'te listelenmiyor. Sitemap disindaki sayfa yine bulunabilir, ama kesfi tamamen ic linklere kalir; yeni yayinlanan iceriklerde bu gecikme gunlere yayilabilir. Ayrica sitemap kapsami ile gercek sayfa listesi arasindaki fark, Search Console raporlarini okumayi zorlastirir. Dizine girmemesi gereken sayfalar icin dogru cozum sitemap'e eklemek degil noindex vermektir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_CHAIN",
                column: "description_tr",
                value: "Sayfaya tek atlamada degil, birbirini izleyen birden fazla yonlendirmeyle ulasiliyor (orn. http > https > www > son adres). Her atlama ek gecikme demektir ve zincir uzadikca tarayicilarin takibi birakma ihtimali artar; tarama butcesi de gereksiz harcanir. Sinyal kaybi tek basina buyuk degildir, ama acilis suresi ve kesif verimliligi olculebilir sekilde duser. Alan adi tasima gibi gecislerde gecici olarak normaldir, kalicilasmamalidir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "REDIRECT_TARGET_INVALID",
                column: "description_tr",
                value: "Yonlendirmenin Location basligi http/https disi ya da ayristirilamayan bir degere isaret ediyor; hicbir istemci hedefe ulasamaz. Kullanici zincirin ortasinda kalir, arama motoru da adresi olu kabul eder. Sebep genellikle sablon hatasi, eksik alan adi ya da javascript:/tel: gibi yanlis semali bir degerdir. Bu kuralin bilincli bir kullanimi yoktur; her tetiklenmesi gercek hatadir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "ROBOTS_NOINDEX",
                columns: new[] { "description_tr", "how_to_fix_tr" },
                values: new object[] { "Sayfanin robots meta etiketinde ya da X-Robots-Tag yanit basliginda noindex var. Bu, arama motorlarina \"bu sayfayi dizine ekleme\" demektir; sayfa taransa bile sonuclarda hic gorunmez ve aldigi ic linklerin degeri bir yere aktarilmaz. Sepet, hesap, filtre veya tesekkur sayfalarinda bilincli bir tercih olabilir; trafik beklenen bir icerik sayfasinda ise en agir hatalardan biridir.", "1) Sayfa kaynagindaki <meta name=\"robots\"> etiketini ve HTTP yanitindaki X-Robots-Tag basligini birlikte kontrol et; noindex ikisinden birinde olabilir.\n2) Sayfa dizine girmeliyse degeri index,follow yap veya etiketi tamamen kaldir.\n3) CMS/tema ayarlarinda \"arama motorlarini engelle\" secenegi aciksa kapat; test ortamindan kopyalanan ayar en sik sebeptir.\n4) Duzeltmeden sonra Search Console > URL Denetimi ile sayfayi yeniden tara." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SCHEMA_MISSING",
                column: "description_tr",
                value: "Sayfada schema.org isaretlemesi bulunamadi. Yapisal veri siralamayi dogrudan degistirmez, ama arama sonucunda yildiz, fiyat, sik sorulanlar, tarif gibi zengin gosterimleri mumkun kilar; bu da tiklama oranini artirir. Ayrica sayfanin ne hakkinda oldugunu makineye net soyler. Hicbir zengin sonuc tipine uymayan sade sayfalarda eksikligi sorun degildir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SERVER_ERROR_5XX",
                column: "description_tr",
                value: "Sayfa 5xx donuyor ya da hic getirilemedi (baglanti hatasi, zaman asimi). Bu sunucu kaynakli bir arizadir; arama motoru 5xx gorunce once tarama hizini dusurur, hata surerse sayfayi dizinden cikarir. 4xx'ten daha acildir, cunku sorun cogu zaman tek sayfada degil altyapinin tamaminda olur. Planli bakim aninda alinan olcum ise yanlis pozitif olabilir — dogru davranis o sirada 503 donmektir.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "SITEMAP_MISSING",
                column: "description_tr",
                value: "Sitede okunabilir bir XML sitemap bulunamadi: robots.txt'te Sitemap satiri yok ve bilinen adreslerde gecerli bir dosya yanit vermiyor. Sitemap zorunlu degildir ama yeni ve derindeki sayfalarin kesfini hizlandirir, son guncelleme tarihini bildirir. Ozellikle ic linki zayif veya cok sayfali sitelerde fark buyuktur. Menuden her sayfaya erisilen birkac sayfalik sitelerde etkisi kucuktur.");

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "THIN_CONTENT",
                columns: new[] { "description_tr", "how_to_fix_tr" },
                values: new object[] { "Sayfa metni 300 kelimenin altinda. Kelime sayisi tek basina bir siralama faktoru degildir, ancak bu uzunluk cogu sorguda kullanicinin sorusunu karsilamaz ve sayfa dusuk degerli olarak degerlendirilir. Cok sayida ince sayfa, sitenin genel kalite algisini da asagi ceker. Iletisim, giris, tesekkur gibi islevsel sayfalarda kisa metin normaldir — bunlarda kurali yoksaymak dogrudur.", "1) Sayfanin hedefledigi soruyu belirle ve cevabi eksiksiz ver: kapsam, ornek, sik sorulanlar.\n2) Baska sayfalardan kopyalanmis metin yerine ozgun icerik uret; uzunluk tek basina yeterli degildir.\n3) Ayni konuyu bolen cok sayida ince sayfayi tek guclu sayfada birlestir, eskilerini 301 ile yonlendir.\n4) Sayfa dogasi geregi kisaysa (islevsel sayfa) kurali site genelinde yoksay." });

            migrationBuilder.UpdateData(
                table: "rules",
                keyColumn: "code",
                keyValue: "TOO_DEEP",
                column: "description_tr",
                value: "Sayfa kok sayfadan 4 tiklamadan uzakta. Derin sayfalar hem kullanici hem tarayici tarafindan daha az ziyaret edilir; buyuk sitelerde tarama butcesi ust seviyelerde tukenir ve derindeki icerik gec guncellenir. Derinlik dogrudan bir ceza degildir, ama onemli sayfalarin derinde kalmasi gorunurluk kaybidir. Arsiv ve eski sayfa listelerinde derinlik dogaldir.");
        }
    }
}
