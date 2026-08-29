using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SeoCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    ip = table.Column<IPAddress>(type: "inet", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_profiles",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    max_chars = table.Column<int>(type: "integer", nullable: false),
                    recommended_chars = table.Column<int>(type: "integer", nullable: false),
                    max_hashtags = table.Column<int>(type: "integer", nullable: false),
                    supports_links = table.Column<bool>(type: "boolean", nullable: false),
                    guidance_tr = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_profiles", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "rules",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false),
                    title_tr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description_tr = table.Column<string>(type: "text", nullable: false),
                    how_to_fix_tr = table.Column<string>(type: "text", nullable: false),
                    doc_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rules", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    page_quota = table.Column<int>(type: "integer", nullable: false),
                    pages_used = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ai_token_quota = table.Column<long>(type: "bigint", nullable: false),
                    ai_tokens_used = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    quota_reset_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    base_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    verification_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    verification_token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    schedule_cron = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    default_brand_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    crawl_settings = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sites", x => x.id);
                    table.ForeignKey(
                        name: "fk_sites_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "brand_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    address_form = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    emoji_usage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    banned_phrases = table.Column<List<string>>(type: "text[]", nullable: false),
                    default_hashtags = table.Column<List<string>>(type: "text[]", nullable: false),
                    target_audience = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    extra_context = table.Column<string>(type: "text", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brand_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_brand_profiles_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_brand_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "crawls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    trigger = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    pages_discovered = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    pages_crawled = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    overall_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    category_scores = table.Column<string>(type: "jsonb", nullable: false),
                    issue_counts = table.Column<string>(type: "jsonb", nullable: false),
                    scoring_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_crawls", x => x.id);
                    table.ForeignKey(
                        name: "fk_crawls_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "issue_ignores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    url_pattern = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_ignores", x => x.id);
                    table.ForeignKey(
                        name: "fk_issue_ignores_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_issue_ignores_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    crawl_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    url_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    redirect_to = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    response_time_ms = table.Column<int>(type: "integer", nullable: true),
                    html_size_bytes = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    title_length = table.Column<int>(type: "integer", nullable: true),
                    meta_description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    meta_desc_length = table.Column<int>(type: "integer", nullable: true),
                    h1texts = table.Column<List<string>>(type: "text[]", nullable: false),
                    h2count = table.Column<int>(type: "integer", nullable: false),
                    word_count = table.Column<int>(type: "integer", nullable: false),
                    canonical_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    robots_meta = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    og_data = table.Column<string>(type: "jsonb", nullable: true),
                    schema_types = table.Column<List<string>>(type: "text[]", nullable: false),
                    images_total = table.Column<int>(type: "integer", nullable: false),
                    images_no_alt = table.Column<int>(type: "integer", nullable: false),
                    inlink_count = table.Column<int>(type: "integer", nullable: false),
                    outlink_internal = table.Column<int>(type: "integer", nullable: false),
                    outlink_external = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    main_text = table.Column<string>(type: "text", nullable: true),
                    lang = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    crawled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pages", x => x.id);
                    table.ForeignKey(
                        name: "fk_pages_crawls_crawl_id",
                        column: x => x.crawl_id,
                        principalTable: "crawls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    crawl_id = table.Column<Guid>(type: "uuid", nullable: false),
                    compare_crawl_id = table.Column<Guid>(type: "uuid", nullable: true),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_reports_crawls_crawl_id",
                        column: x => x.crawl_id,
                        principalTable: "crawls",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_reports_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vitals",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    crawl_id = table.Column<Guid>(type: "uuid", nullable: true),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    device = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    lcp_ms = table.Column<int>(type: "integer", nullable: true),
                    inp_ms = table.Column<int>(type: "integer", nullable: true),
                    cls = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: true),
                    ttfb_ms = table.Column<int>(type: "integer", nullable: true),
                    fcp_ms = table.Column<int>(type: "integer", nullable: true),
                    perf_score = table.Column<int>(type: "integer", nullable: true),
                    collected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vitals", x => x.id);
                    table.ForeignKey(
                        name: "fk_vitals_crawls_crawl_id",
                        column: x => x.crawl_id,
                        principalTable: "crawls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_vitals_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: true),
                    page_id = table.Column<Guid>(type: "uuid", nullable: true),
                    brand_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    platform_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    input = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    tokens_in = table.Column<int>(type: "integer", nullable: false),
                    tokens_out = table.Column<int>(type: "integer", nullable: false),
                    cost_usd = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_content_jobs_brand_profiles_brand_profile_id",
                        column: x => x.brand_profile_id,
                        principalTable: "brand_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_content_jobs_pages_page_id",
                        column: x => x.page_id,
                        principalTable: "pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_content_jobs_platform_profiles_platform_code",
                        column: x => x.platform_code,
                        principalTable: "platform_profiles",
                        principalColumn: "code");
                    table.ForeignKey(
                        name: "fk_content_jobs_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_content_jobs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_content_jobs_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "issues",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    crawl_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rule_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "open"),
                    first_seen_crawl_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_crawl_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    evidence = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issues", x => x.id);
                    table.ForeignKey(
                        name: "fk_issues_crawls_crawl_id",
                        column: x => x.crawl_id,
                        principalTable: "crawls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_issues_pages_page_id",
                        column: x => x.page_id,
                        principalTable: "pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_issues_rules_rule_code",
                        column: x => x.rule_code,
                        principalTable: "rules",
                        principalColumn: "code");
                    table.ForeignKey(
                        name: "fk_issues_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "page_links",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    crawl_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_page_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    to_page_id = table.Column<Guid>(type: "uuid", nullable: true),
                    anchor_text = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_internal = table.Column<bool>(type: "boolean", nullable: false),
                    is_nofollow = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_page_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_page_links_pages_from_page_id",
                        column: x => x.from_page_id,
                        principalTable: "pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_page_links_pages_to_page_id",
                        column: x => x.to_page_id,
                        principalTable: "pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "content_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_index = table.Column<int>(type: "integer", nullable: false),
                    angle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    body = table.Column<string>(type: "text", nullable: false),
                    hashtags = table.Column<List<string>>(type: "text[]", nullable: false),
                    cta = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    char_count = table.Column<int>(type: "integer", nullable: false),
                    is_favorite = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_content_variants_content_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "content_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "platform_profiles",
                columns: new[] { "code", "display_name", "guidance_tr", "is_active", "max_chars", "max_hashtags", "recommended_chars", "supports_links" },
                values: new object[,]
                {
                    { "facebook", "Facebook", "Kisa metin + tek net CTA en iyi performansi verir. Link onizlemesi otomatik gelir, URL'i metinden cikarabilirsin. Hashtag az kullanilir.", true, 63206, 3, 120, true },
                    { "instagram", "Instagram", "Ilk satir kancadir; link biyoya alinir. Gorsel odakli, 3-5 anlamli hashtag yeterli. Emoji dengeli kullanilir.", true, 2200, 30, 150, false },
                    { "linkedin", "LinkedIn", "Profesyonel ton, degerli icgoru ile basla. Ilk 2 satir 'devamini gor' oncesi gorunur. 3-5 sektorel hashtag. Emoji az.", true, 3000, 5, 1500, true },
                    { "x", "X", "Link iceren gonderilerde erisim duser; linki ilk yanita almayi oner. En fazla 1-2 hashtag. Net, iddiali tek cumle.", true, 280, 2, 240, true }
                });

            migrationBuilder.InsertData(
                table: "rules",
                columns: new[] { "code", "category", "description_tr", "doc_url", "how_to_fix_tr", "is_active", "severity", "title_tr", "weight" },
                values: new object[,]
                {
                    { "BROKEN_INTERNAL_LINK", "links", "Ic link 4xx/5xx donen bir sayfaya gidiyor.", null, "Hedefi guncelle veya linki kaldir.", true, "high", "Kirik ic link", 6 },
                    { "CANONICAL_MISSING", "indexability", "rel=canonical etiketi tanimli degil.", null, "Kendine referans veren bir canonical URL ekle.", true, "low", "Canonical yok", 3 },
                    { "DUPLICATE_CONTENT", "content", "Ayni content_hash birden fazla sayfada goruluyor.", null, "Icerigi farklilastir veya canonical ile asil sayfayi isaret et.", true, "high", "Yinelenen icerik", 7 },
                    { "H1_MISSING", "content", "Sayfada H1 basligi bulunmuyor.", null, "Sayfa basina tek ve aciklayici bir H1 ekle.", true, "high", "H1 yok", 6 },
                    { "H1_MULTIPLE", "content", "Sayfada birden cok H1 var.", null, "Tek H1 birak, digerlerini H2/H3 yap.", true, "medium", "Birden fazla H1", 4 },
                    { "HREFLANG_INVALID", "i18n", "hreflang deger(ler)i gecersiz veya karsilikli degil.", null, "Dil-bolge kodlarini duzelt ve karsilikli hreflang tanimla.", true, "medium", "Gecersiz hreflang", 4 },
                    { "HTTP_STATUS", "indexability", "Sayfa 4xx/5xx durum kodu donuyor ve dizine eklenemez.", null, "Sunucu/yonlendirme yapilandirmasini duzelt, kalici 200 don.", true, "critical", "Sayfa 2xx donmuyor", 10 },
                    { "IMAGE_ALT_MISSING", "images", "Bir veya daha fazla <img> alt niteligi tasimiyor.", null, "Anlamli gorsellere aciklayici alt metni ekle.", true, "low", "Alt metni eksik gorseller", 3 },
                    { "META_DESCRIPTION_LENGTH", "meta", "Meta description 70 karakterden kisa veya 160 karakterden uzun.", null, "Uzunlugu 70-160 karakter araligina cek.", true, "low", "Meta description uzunlugu ideal degil", 3 },
                    { "META_DESCRIPTION_MISSING", "meta", "Sayfada meta description yok; SERP snippet'i kontrolsuz.", null, "70-160 karakter, tiklama tesvik eden bir meta description yaz.", true, "high", "Meta description yok", 6 },
                    { "META_TITLE_LENGTH", "meta", "Title 30 karakterden kisa veya 60 karakterden uzun.", null, "Title'i 30-60 karakter araligina getir.", true, "medium", "Title uzunlugu ideal degil", 5 },
                    { "META_TITLE_MISSING", "meta", "Sayfada <title> etiketi bulunmuyor.", null, "Her sayfaya 30-60 karakter, anahtar kelime iceren benzersiz bir title ekle.", true, "critical", "Title etiketi yok", 9 },
                    { "NOINDEX_DETECTED", "indexability", "robots meta veya X-Robots-Tag noindex iceriyor.", null, "Dizine girmesi gereken sayfalarda noindex'i kaldir.", true, "critical", "noindex etiketi", 9 },
                    { "POOR_LCP", "performance", "Largest Contentful Paint 2.5 sn'nin uzerinde.", null, "Kritik gorselleri onceliklendir, render-blocking kaynaklari azalt.", true, "high", "Kotu LCP", 7 },
                    { "SLOW_TTFB", "performance", "Ilk bayt suresi 800 ms'nin uzerinde.", null, "Sunucu yanit suresini, cache ve CDN kullanimini iyilestir.", true, "medium", "Yavas TTFB", 5 },
                    { "STRUCTURED_DATA_MISSING", "structured_data", "Sayfada schema.org isaretlemesi bulunamadi.", null, "Uygun schema tipini (Article, Product, FAQ...) JSON-LD olarak ekle.", true, "low", "Yapisal veri yok", 3 },
                    { "THIN_CONTENT", "content", "Sayfa metni 300 kelimenin altinda.", null, "Icerigi ozgun ve kullaniciya deger katacak sekilde genislet.", true, "medium", "Zayif icerik", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id_created_at",
                table: "audit_logs",
                columns: new[] { "tenant_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_brand_profiles_site_id",
                table: "brand_profiles",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "ix_brand_profiles_tenant_id",
                table: "brand_profiles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_jobs_brand_profile_id",
                table: "content_jobs",
                column: "brand_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_jobs_created_by",
                table: "content_jobs",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_content_jobs_page_id",
                table: "content_jobs",
                column: "page_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_jobs_platform_code",
                table: "content_jobs",
                column: "platform_code");

            migrationBuilder.CreateIndex(
                name: "ix_content_jobs_site_id",
                table: "content_jobs",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_jobs_tenant_id_created_at",
                table: "content_jobs",
                columns: new[] { "tenant_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_content_variants_job_id_variant_index",
                table: "content_variants",
                columns: new[] { "job_id", "variant_index" });

            migrationBuilder.CreateIndex(
                name: "ix_crawls_site_id_created_at",
                table: "crawls",
                columns: new[] { "site_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_issue_ignores_created_by",
                table: "issue_ignores",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_issue_ignores_site_id_rule_code",
                table: "issue_ignores",
                columns: new[] { "site_id", "rule_code" });

            migrationBuilder.CreateIndex(
                name: "ix_issues_crawl_id_severity",
                table: "issues",
                columns: new[] { "crawl_id", "severity" });

            migrationBuilder.CreateIndex(
                name: "ix_issues_page_id",
                table: "issues",
                column: "page_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_rule_code",
                table: "issues",
                column: "rule_code");

            migrationBuilder.CreateIndex(
                name: "ix_issues_site_id_rule_code_status",
                table: "issues",
                columns: new[] { "site_id", "rule_code", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_page_links_crawl_id_from_page_id",
                table: "page_links",
                columns: new[] { "crawl_id", "from_page_id" });

            migrationBuilder.CreateIndex(
                name: "ix_page_links_crawl_id_to_page_id",
                table: "page_links",
                columns: new[] { "crawl_id", "to_page_id" });

            migrationBuilder.CreateIndex(
                name: "ix_page_links_from_page_id",
                table: "page_links",
                column: "from_page_id");

            migrationBuilder.CreateIndex(
                name: "ix_page_links_to_page_id",
                table: "page_links",
                column: "to_page_id");

            migrationBuilder.CreateIndex(
                name: "ix_pages_crawl_id_content_hash",
                table: "pages",
                columns: new[] { "crawl_id", "content_hash" });

            migrationBuilder.CreateIndex(
                name: "ix_pages_crawl_id_status_code",
                table: "pages",
                columns: new[] { "crawl_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "ix_pages_crawl_id_url_hash",
                table: "pages",
                columns: new[] { "crawl_id", "url_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id_expires_at",
                table: "refresh_tokens",
                columns: new[] { "user_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_crawl_id",
                table: "reports",
                column: "crawl_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_site_id_crawl_id",
                table: "reports",
                columns: new[] { "site_id", "crawl_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sites_tenant_id_is_active",
                table: "sites",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_tenants_slug",
                table: "tenants",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id_email",
                table: "users",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vitals_crawl_id",
                table: "vitals",
                column: "crawl_id");

            migrationBuilder.CreateIndex(
                name: "ix_vitals_site_id_url_collected_at",
                table: "vitals",
                columns: new[] { "site_id", "url", "collected_at" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "content_variants");

            migrationBuilder.DropTable(
                name: "issue_ignores");

            migrationBuilder.DropTable(
                name: "issues");

            migrationBuilder.DropTable(
                name: "page_links");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "vitals");

            migrationBuilder.DropTable(
                name: "content_jobs");

            migrationBuilder.DropTable(
                name: "rules");

            migrationBuilder.DropTable(
                name: "brand_profiles");

            migrationBuilder.DropTable(
                name: "pages");

            migrationBuilder.DropTable(
                name: "platform_profiles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "crawls");

            migrationBuilder.DropTable(
                name: "sites");

            migrationBuilder.DropTable(
                name: "tenants");
        }
    }
}
