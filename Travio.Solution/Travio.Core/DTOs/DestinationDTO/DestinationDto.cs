namespace Travio.Core.DTOs.DestinationDTO;

public class DestinationDto
{
    public int DestinationID { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public double Rating { get; set; }
    public int TotalReviews { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string CityName { get; set; } = string.Empty;


    public List<string> ImageUrls { get; set; } = new List<string>();
    public List<InterestDto> Interests { get; set; } = new List<InterestDto>();
}
