using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class WatchlistConfiguration : IEntityTypeConfiguration<Watchlist>
{
    public void Configure(EntityTypeBuilder<Watchlist> builder)
    {
        builder.ToTable("Watchlists");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.AlertPrice).HasPrecision(18, 3);

        builder.HasOne(w => w.User)
            .WithMany(u => u.Watchlists)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.Stock)
            .WithMany(s => s.Watchlists)
            .HasForeignKey(w => w.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
