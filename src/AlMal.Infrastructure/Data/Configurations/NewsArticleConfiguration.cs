using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> builder)
    {
        builder.ToTable("NewsArticles");
        builder.HasKey(na => na.Id);

        builder.Property(na => na.TitleAr).HasMaxLength(500).IsRequired();
        builder.Property(na => na.Source).HasMaxLength(200).IsRequired();
        builder.Property(na => na.SourceUrl).HasMaxLength(1000);
        builder.Property(na => na.Summary).HasMaxLength(1000);
        builder.Property(na => na.ContextData).HasColumnType("nvarchar(max)");
        builder.Property(na => na.ExternalId).HasMaxLength(100);
        builder.Property(na => na.ImageUrl).HasMaxLength(500);
    }
}
