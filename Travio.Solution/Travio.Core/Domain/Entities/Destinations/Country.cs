namespace Travio.Core.Domain.Entities.Destinations;

public class Country
{
    public int CountryID { get; set; }
    public int ContinentID { get; set; } // FK
    public string Name { get; set; } = string.Empty;
    public string FlagURL { get; set; } = string.Empty;

    // Navigation
    public Continent Continent { get; set; } = null!;
    public ICollection<City> Cities { get; set; } = new HashSet<City>();
}
