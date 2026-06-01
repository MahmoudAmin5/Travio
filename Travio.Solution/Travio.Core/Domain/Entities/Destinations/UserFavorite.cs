using Travio.Core.Domain.Entities.Account_Mangement;

namespace Travio.Core.Domain.Entities.Destinations;

public class UserFavorite
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public int DestinationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Destination Destination { get; set; } = null!;
}
