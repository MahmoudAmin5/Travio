namespace Travio.Core.Domain.Entities.Destinations;

public class UserPreference
{
    public int Id { get; set; }
    public string UserID { get; set; } // Foreign Key
    public int CategoryID { get; set; }
    public int OptionId { get; set; }
}
