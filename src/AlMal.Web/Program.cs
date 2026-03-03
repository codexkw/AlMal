using AlMal.Domain.Entities;
using AlMal.Application.Interfaces;
using AlMal.Infrastructure.Data;
using AlMal.Infrastructure.ExternalApis;
using AlMal.Infrastructure.Identity;
using AlMal.Infrastructure.Jobs;
using AlMal.Infrastructure.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using QuestPDF.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Cloudflare forwarded headers
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("Logs/almal-web-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

    // Database
    builder.Services.AddDbContext<AlMalDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AlMalDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

    // Anti-forgery cookie must also be Lax behind Cloudflare TLS termination
    builder.Services.AddAntiforgery(options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    // Hangfire
    var hangfireConnection = builder.Configuration.GetConnectionString("HangfireConnection");
    if (!string.IsNullOrEmpty(hangfireConnection))
    {
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(hangfireConnection, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));
        builder.Services.AddHangfireServer();
    }

    // Redis
    var redisConnection = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConnection))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "AlMal:";
        });
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
    }

    // HttpClient for scraper
    builder.Services.AddHttpClient<BoursakuwaitScraper>();
    builder.Services.AddScoped<IMarketDataProvider, BoursakuwaitScraper>();

    // HttpClient for news
    builder.Services.AddHttpClient<NewsDataClient>();
    builder.Services.AddScoped<INewsProvider, NewsDataClient>();

    // AI service
    builder.Services.AddScoped<IAiAnalysisService, ClaudeAiService>();

    // Simulation service
    builder.Services.AddScoped<ISimulationService, SimulationService>();

    // Post service
    builder.Services.AddScoped<IPostService, PostService>();

    // WhatsApp service
    builder.Services.AddHttpClient<WhatsAppClient>();
    builder.Services.AddScoped<IWhatsAppService, WhatsAppClient>();

    // Academy & Simulation services
    builder.Services.AddScoped<ICourseService, CourseService>();
    builder.Services.AddScoped<IQuizService, QuizService>();
    builder.Services.AddScoped<ICertificateService, CertificateService>();

    // QuestPDF license
    QuestPDF.Settings.License = LicenseType.Community;

    // Services
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<MarketDataScraperJob>();
    builder.Services.AddScoped<OrderBookScraperJob>();
    builder.Services.AddScoped<DisclosureScraperJob>();
    builder.Services.AddScoped<StockPriceHistoryJob>();
    builder.Services.AddScoped<NewsFetcherJob>();
    builder.Services.AddScoped<AiDisclosureProcessorJob>();
    builder.Services.AddScoped<AiNewsProcessorJob>();
    builder.Services.AddScoped<AlertEngineJob>();
    builder.Services.AddScoped<DailyMarketSummaryJob>();

    builder.Services.AddSignalR();

    // Health checks
    builder.Services.AddHealthChecks()
        .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

    // Output caching
    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));
    });

    // Response compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    // Seed roles
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = ["User", "ProAnalyst", "CertifiedAnalyst", "Moderator", "Admin", "SuperAdmin"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Seed database (admin user, sectors, stocks, indices)
    await DatabaseSeeder.SeedAsync(app.Services);

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
    }

    app.UseForwardedHeaders();
    app.UseResponseCompression();
    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.UseOutputCache();

    app.UseAuthentication();
    app.UseAuthorization();

    // Hangfire dashboard and recurring jobs
    if (!string.IsNullOrEmpty(hangfireConnection))
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [] // TODO: Add admin-only authorization filter
        });

        // Register recurring jobs
        RecurringJob.AddOrUpdate<MarketDataScraperJob>(
            "market-data-scraper",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/30 * * * * *"); // Every 30 seconds (job self-checks market hours)

        RecurringJob.AddOrUpdate<OrderBookScraperJob>(
            "order-book-scraper",
            job => job.ExecuteAsync(CancellationToken.None),
            "* * * * *"); // Every minute (job self-checks market hours)

        RecurringJob.AddOrUpdate<DisclosureScraperJob>(
            "disclosure-scraper",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/5 * * * *"); // Every 5 minutes

        RecurringJob.AddOrUpdate<StockPriceHistoryJob>(
            "stock-price-history",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 13 * * 0-4"); // Daily at 1 PM UTC (4 PM KWT) after market close, Sun-Thu

        RecurringJob.AddOrUpdate<NewsFetcherJob>(
            "news-fetcher",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/15 * * * *"); // Every 15 minutes

        RecurringJob.AddOrUpdate<AiDisclosureProcessorJob>(
            "ai-disclosure-processor",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/10 * * * *"); // Every 10 minutes

        RecurringJob.AddOrUpdate<AiNewsProcessorJob>(
            "ai-news-processor",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/10 * * * *"); // Every 10 minutes

        RecurringJob.AddOrUpdate<AlertEngineJob>(
            "alert-engine",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/30 * * * * *"); // Every 30 seconds (job self-checks market hours)

        RecurringJob.AddOrUpdate<DailyMarketSummaryJob>(
            "daily-market-summary",
            job => job.ExecuteAsync(CancellationToken.None),
            "45 9 * * 0-4"); // 9:45 AM UTC = 12:45 PM KWT, Sun-Thu
    }

    app.MapHealthChecks("/health");

    app.MapStaticAssets();

    app.MapHub<AlMal.Web.Hubs.MarketHub>("/hubs/market");

    app.MapControllerRoute(
        name: "market",
        pattern: "market",
        defaults: new { controller = "Market", action = "Index" });

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Market}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
