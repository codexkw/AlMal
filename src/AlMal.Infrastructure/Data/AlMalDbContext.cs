using AlMal.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Infrastructure.Data;

public class AlMalDbContext : IdentityDbContext<ApplicationUser>
{
    public AlMalDbContext(DbContextOptions<AlMalDbContext> options) : base(options) { }

    // Market Data
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<StockPrice> StockPrices => Set<StockPrice>();
    public DbSet<MarketIndex> MarketIndices => Set<MarketIndex>();
    public DbSet<OrderBook> OrderBooks => Set<OrderBook>();
    public DbSet<FinancialStatement> FinancialStatements => Set<FinancialStatement>();
    public DbSet<Disclosure> Disclosures => Set<Disclosure>();

    // Community
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostStockMention> PostStockMentions => Set<PostStockMention>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<Watchlist> Watchlists => Set<Watchlist>();

    // Simulation & Education
    public DbSet<SimulationPortfolio> SimulationPortfolios => Set<SimulationPortfolio>();
    public DbSet<SimulationTrade> SimulationTrades => Set<SimulationTrade>();
    public DbSet<SimulationHolding> SimulationHoldings => Set<SimulationHolding>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    // News & Alerts
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<NewsArticleStock> NewsArticleStocks => Set<NewsArticleStock>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertHistory> AlertHistories => Set<AlertHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlMalDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
