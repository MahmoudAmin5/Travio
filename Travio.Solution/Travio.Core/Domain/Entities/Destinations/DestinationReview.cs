using Travio.Core.Domain.Entities.Account_Mangement;

namespace Travio.Core.Domain.Entities.Destinations;

public class DestinationReview
{
    public int ReviewId { get; set; }
    public int DestinationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int HelpfulVotes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Destination Destination { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
