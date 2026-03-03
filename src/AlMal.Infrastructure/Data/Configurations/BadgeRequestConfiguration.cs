using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class BadgeRequestConfiguration : IEntityTypeConfiguration<BadgeRequest>
{
    public void Configure(EntityTypeBuilder<BadgeRequest> builder)
    {
        builder.ToTable("BadgeRequests");
        builder.HasKey(br => br.Id);

        builder.Property(br => br.Justification).HasMaxLength(1000);
        builder.Property(br => br.CertificateUrl).HasMaxLength(500);
        builder.Property(br => br.RejectionReason).HasMaxLength(500);
        builder.Property(br => br.ReviewedByUserId).HasMaxLength(450);

        builder.HasOne(br => br.User)
            .WithMany()
            .HasForeignKey(br => br.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(br => new { br.UserId, br.Status })
            .HasDatabaseName("IX_BadgeRequest_UserId_Status");
    }
}
