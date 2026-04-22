namespace Travio.Core.DTOs.DestinationDTO;

public class DestinationReviewPageDto
{
    public int DestinationId { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public List<DestinationReviewDto> Items { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
    public bool IsEmpty { get; set; }
}
