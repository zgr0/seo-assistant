namespace SeoCopilot.Domain.Enums;

// Tum enum'lar veritabaninda snake_case metin olarak saklanir
// (bkz. Infrastructure/Persistence/Conventions/SnakeCaseEnumConverter).

public enum TenantPlan { Trial, Starter, Pro, Agency }

public enum UserRole { Owner, Admin, Member, Viewer }

public enum CrawlStatus { Queued, Running, Completed, Partial, Failed, Cancelled }

public enum CrawlTrigger { Manual, Scheduled }

public enum RuleCategory
{
    Indexability,
    Meta,
    Content,
    Links,
    Performance,
    StructuredData,
    Images,
    I18n
}

public enum IssueStatus { Open, Ignored, Fixed }

public enum VitalsDevice { Mobile, Desktop }

public enum VitalsSource { PsiLab, PsiField }

public enum BrandTone { Kurumsal, Samimi, Teknik, SatisOdakli }

public enum AddressForm { Sen, Siz }

public enum EmojiUsage { None, Light, Heavy }

public enum ContentJobType
{
    Title,
    MetaDescription,
    H1,
    ProductDescription,
    BlogOutline,
    FixAdvice,
    SocialPost,
    SocialBatch,
    HashtagSet,

    /// <summary>Site geneli sosyal medya paketi: sayfa secimi + gorsel uretimi dahil.</summary>
    SocialKit
}

public enum ContentJobStatus { Queued, Running, Done, Failed }

/// <summary>Ham = modelden gelen gorsel; yazili = uzerine baslik ve marka satiri basilmis kopya.</summary>
public enum ContentAssetKind { Raw, Captioned }

public enum ReportStatus { Queued, Running, Done, Failed }
