using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Infrastructure.Configrations.Destinations;

public class DestinationInterestConfiguration
    : IEntityTypeConfiguration<DestinationInterest>
{
    public void Configure(EntityTypeBuilder<DestinationInterest> builder)
    {
        builder.ToTable("Destination_Interest");
        builder.HasOne(x => x.Destination)
               .WithMany(d => d.DestinationInterests)
               .HasForeignKey(x => x.DestinationID)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Interest)
               .WithMany(i => i.DestinationInterests)
               .HasForeignKey(x => x.InterestID)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

