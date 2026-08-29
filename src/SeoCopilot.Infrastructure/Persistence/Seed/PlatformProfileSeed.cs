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
                GuidanceTr = "Ilk satir kancadir; link biyoya alinir. Gorsel odakli, 3-5 anlamli hashtag yeterli. Emoji dengeli kullanilir.",
                IsActive = true
            },
            new PlatformProfile
            {
                Code = "facebook", DisplayName = "Facebook",
                MaxChars = 63206, RecommendedChars = 120, MaxHashtags = 3, SupportsLinks = true,
                GuidanceTr = "Kisa metin + tek net CTA en iyi performansi verir. Link onizlemesi otomatik gelir, URL'i metinden cikarabilirsin. Hashtag az kullanilir.",
                IsActive = true
            },
            new PlatformProfile
            {
                Code = "x", DisplayName = "X",
                MaxChars = 280, RecommendedChars = 240, MaxHashtags = 2, SupportsLinks = true,
                GuidanceTr = "Link iceren gonderilerde erisim duser; linki ilk yanita almayi oner. En fazla 1-2 hashtag. Net, iddiali tek cumle.",
                IsActive = true
            },
            new PlatformProfile
            {
                Code = "linkedin", DisplayName = "LinkedIn",
                MaxChars = 3000, RecommendedChars = 1500, MaxHashtags = 5, SupportsLinks = true,
                GuidanceTr = "Profesyonel ton, degerli icgoru ile basla. Ilk 2 satir 'devamini gor' oncesi gorunur. 3-5 sektorel hashtag. Emoji az.",
                IsActive = true
            }
        );
    }
}
