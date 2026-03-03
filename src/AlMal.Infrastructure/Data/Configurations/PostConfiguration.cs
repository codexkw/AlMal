using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Content).HasMaxLength(2000).IsRequired();
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.VideoUrl).HasMaxLength(500);
        builder.Property(p => p.ReportReason).HasMaxLength(500);
        builder.Property(p => p.ReportedByUserId).HasMaxLength(450);

        builder.HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Repost relationship
        builder.HasOne(p => p.OriginalPost)
            .WithMany(p => p.Reposts)
            .HasForeignKey(p => p.OriginalPostId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
