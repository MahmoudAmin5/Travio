namespace Travio.Core.Domain.Entities.Destinations;

public class Continent
{
    public int ContinentID { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Country> Countries { get; set; } = new HashSet<Country>();
}
