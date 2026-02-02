namespace Travio.Core.Domain.Entities.Destinations;

public class Interest
{
    public int InterestID { get; set; }
    public string InterestName { get; set; } = null!;
    // Navigation
    public ICollection<DestinationInterest> DestinationInterests { get; set; } = new HashSet<DestinationInterest>();

}
