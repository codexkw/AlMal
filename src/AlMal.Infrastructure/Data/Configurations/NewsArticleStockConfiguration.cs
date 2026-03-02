using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class NewsArticleStockConfiguration : IEntityTypeConfiguration<NewsArticleStock>
{
    public void Configure(EntityTypeBuilder<NewsArticleStock> builder)
    {
        builder.ToTable("NewsArticleStocks");
        builder.HasKey(nas => new { nas.NewsArticleId, nas.StockId });

        builder.HasOne(nas => nas.NewsArticle)
            .WithMany(na => na.NewsArticleStocks)
            .HasForeignKey(nas => nas.NewsArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(nas => nas.Stock)
            .WithMany(s => s.NewsArticleStocks)
            .HasForeignKey(nas => nas.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
