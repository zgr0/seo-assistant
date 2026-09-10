namespace SeoCopilot.Application.Services.Social;

/// <summary>
/// Anahtar kelime ve hashtag cikariminda atlanan kelimeler: baglaclar, zarflar ve
/// hicbir konu tasimayan genel sifat/fiiller. Gorsel istemi ile gonderi ureticisi
/// ayni listeyi kullanir ki cikti tutarli olsun.
/// </summary>
public static class TurkishStopWords
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // baglac / edat
        "için", "ile", "veya", "ancak", "ama", "yani", "hem", "göre", "üzere", "kadar",
        "sonra", "önce", "arasında", "üzerine", "hakkında", "ayrıca", "böylece", "çünkü",

        // zamir / isaret
        "bunlar", "şunlar", "onlar", "bizim", "sizin", "bize", "size", "kendi", "herhangi",

        // genel sifat / zarf
        "daha", "çok", "gibi", "tüm", "bütün", "çeşitli", "farklı", "birçok", "büyük",
        "yeni", "diğer", "geniş", "gelişmiş", "akıllı", "özel", "genel", "önemli",
        "yüksek", "uygun", "hızlı", "kolay", "detaylı", "ayrıntılı", "fazla", "aşkın",

        // fiil / kalip
        "olan", "olarak", "olup", "iken", "sahip", "bulunan", "edilir", "edilen", "eder",
        "yapılır", "verilir", "sunulan", "sağlar", "nedir", "nasıl", "neden",

        // site kaliplari
        "burada", "şimdi", "lütfen", "tıklayın", "devamı", "ilgili", "şekilde", "amacıyla",
        "tarafından", "sayfa", "sayfası", "sayfamıza", "siteyi", "sitesi", "anasayfa",
        "iletişim", "hakkımızda", "www", "http", "https", "html",

        // ingilizce kaliplar
        "this", "that", "with", "from", "your", "have", "will", "about", "which", "their",
        "more", "page", "home", "contact"
    };

    public static bool Contains(string word) => All.Contains(word);
}
