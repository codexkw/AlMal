using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class PostStockMentionConfiguration : IEntityTypeConfiguration<PostStockMention>
{
    public void Configure(EntityTypeBuilder<PostStockMention> builder)
    {
        builder.ToTable("PostStockMentions");
        builder.HasKey(psm => new { psm.PostId, psm.StockId });

        builder.HasOne(psm => psm.Post)
            .WithMany(p => p.PostStockMentions)
            .HasForeignKey(psm => psm.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(psm => psm.Stock)
            .WithMany(s => s.PostStockMentions)
            .HasForeignKey(psm => psm.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
