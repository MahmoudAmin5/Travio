using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Infrastructure.Configrations.Destinations;

public class DestinationReviewConfiguration : IEntityTypeConfiguration<DestinationReview>
{
    public void Configure(EntityTypeBuilder<DestinationReview> builder)
    {
        builder.ToTable("DestinationReviews");

        builder.HasKey(x => x.ReviewId);

        builder.Property(x => x.Rating)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(500);

        builder.Property(x => x.HelpfulVotes)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Destination)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.DestinationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.DestinationReviews)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.DestinationId, x.UserId })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasIndex(x => new { x.DestinationId, x.IsActive, x.CreatedAtUtc, x.ReviewId });
    }
}
