using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class DisclosureConfiguration : IEntityTypeConfiguration<Disclosure>
{
    public void Configure(EntityTypeBuilder<Disclosure> builder)
    {
        builder.ToTable("Disclosures");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.TitleAr).HasMaxLength(500).IsRequired();
        builder.Property(d => d.ContentAr).HasColumnType("nvarchar(max)");
        builder.Property(d => d.SourceUrl).HasMaxLength(1000);
        builder.Property(d => d.AiSummary).HasColumnType("nvarchar(max)");

        builder.HasOne(d => d.Stock)
            .WithMany(s => s.Disclosures)
            .HasForeignKey(d => d.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
