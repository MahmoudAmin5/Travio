namespace Travio.Core.DTOs.DestinationDTO;

public class DestinationReviewDto
{
    public int ReviewId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string? ReviewerImageUrl { get; set; }
    public DateTime ReviewDateUtc { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int HelpfulVotes { get; set; }
    public bool IsMine { get; set; }
}

public class DestinationReviewMutationDto
{
    public int ReviewId { get; set; }
    public int DestinationId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
}

public class DestinationReviewDeleteResultDto
{
    public int DestinationId { get; set; }
    public bool Deleted { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
}
