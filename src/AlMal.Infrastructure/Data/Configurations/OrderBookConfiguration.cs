using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class OrderBookConfiguration : IEntityTypeConfiguration<OrderBook>
{
    public void Configure(EntityTypeBuilder<OrderBook> builder)
    {
        builder.ToTable("OrderBooks");
        builder.HasKey(ob => ob.Id);

        builder.Property(ob => ob.BidPrice).HasPrecision(18, 3);
        builder.Property(ob => ob.AskPrice).HasPrecision(18, 3);

        builder.HasOne(ob => ob.Stock)
            .WithMany(s => s.OrderBooks)
            .HasForeignKey(ob => ob.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
