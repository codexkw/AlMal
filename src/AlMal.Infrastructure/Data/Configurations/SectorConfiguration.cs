using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class SectorConfiguration : IEntityTypeConfiguration<Sector>
{
    public void Configure(EntityTypeBuilder<Sector> builder)
    {
        builder.ToTable("Sectors");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(s => s.NameEn).HasMaxLength(200);
        builder.Property(s => s.IndexValue).HasPrecision(18, 3);
        builder.Property(s => s.ChangePercent).HasPrecision(8, 4);
    }
}
