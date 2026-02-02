namespace Travio.Core.Domain.Entities.Destinations;

public class City
{
    public int CityID { get; set; }
    public int CountryID { get; set; } // FK
    public string Name { get; set; } = string.Empty;

    // Navigation
    public Country Country { get; set; } = null!;
    public ICollection<Destination> Destinations { get; set; } = new HashSet<Destination>();
}

