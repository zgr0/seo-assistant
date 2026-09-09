using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeoCopilot.Domain.Entities.Content;

namespace SeoCopilot.Infrastructure.Persistence.Seed;

internal sealed class PlatformProfileSeed : IEntityTypeConfiguration<PlatformProfile>
{
    public void Configure(EntityTypeBuilder<PlatformProfile> b)
    {
        b.HasData(
            new PlatformProfile
            {
                Code = "instagram", DisplayName = "Instagram",
                MaxChars = 2200, RecommendedChars = 150, MaxHashtags = 30, SupportsLinks = false,
                GuidanceTr = "İlk satır kancadır; link biyoya alınır. Görsel odaklı, 3-5 anlamlı hashtag yeterli. Emoji dengeli kullanılır.",
                IsActive = true
            },
            new PlatformProfile
            {
                Code = "facebook", DisplayName = "Facebook",
                MaxChars = 63206, RecommendedChars = 120, MaxHashtags = 3, SupportsLinks = true,
                GuidanceTr = "Kısa metin + tek net CTA en iyi performansı verir. Link önizlemesi otomatik gelir, URL'i metinden çıkarabilirsin. Hashtag az kullanılır.",
                IsActive = true
            },
            new PlatformProfile
            {
                Code = "x", DisplayName = "X",
                MaxChars = 280, RecommendedChars = 240, MaxHashtags = 2, SupportsLinks = true,
                GuidanceTr = "Link içeren gönderilerde erişim düşer; linki ilk yanıta almayı öner. En fazla 1-2 hashtag. Net, iddialı tek cümle.",
                IsActive = true
            },
            new PlatformProfile
            {
                Code = "linkedin", DisplayName = "LinkedIn",
                MaxChars = 3000, RecommendedChars = 1500, MaxHashtags = 5, SupportsLinks = true,
                GuidanceTr = "Profesyonel ton, değerli içgörü ile başla. İlk 2 satır 'devamını gör' öncesi görünür. 3-5 sektörel hashtag. Emoji az.",
                IsActive = true
            }
        );
    }
}
