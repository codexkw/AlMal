using AlMal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMal.Infrastructure.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TitleAr).HasMaxLength(300).IsRequired();
        builder.Property(c => c.DescriptionAr).HasMaxLength(2000);
        builder.Property(c => c.ThumbnailUrl).HasMaxLength(500);
        builder.Property(c => c.Price).HasPrecision(18, 3);
    }
}
