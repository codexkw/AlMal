using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
{
    public void Configure(EntityTypeBuilder<UserFollow> builder)
    {
        builder.ToTable("UserFollows");
        builder.HasKey(uf => new { uf.FollowerId, uf.FollowingId });

        builder.HasOne(uf => uf.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(uf => uf.FollowerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(uf => uf.FollowingUser)
            .WithMany(u => u.Followers)
            .HasForeignKey(uf => uf.FollowingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t => t.HasCheckConstraint("CK_UserFollow_NoSelfFollow", "[FollowerId] <> [FollowingId]"));
    }
}
