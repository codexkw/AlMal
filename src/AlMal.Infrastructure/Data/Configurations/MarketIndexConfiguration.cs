using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class MarketIndexConfiguration : IEntityTypeConfiguration<MarketIndex>
{
    public void Configure(EntityTypeBuilder<MarketIndex> builder)
    {
        builder.ToTable("MarketIndices");
        builder.HasKey(mi => mi.Id);

        builder.Property(mi => mi.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(mi => mi.Value).HasPrecision(18, 3);
        builder.Property(mi => mi.Change).HasPrecision(18, 3);
        builder.Property(mi => mi.ChangePercent).HasPrecision(8, 4);
    }
}
