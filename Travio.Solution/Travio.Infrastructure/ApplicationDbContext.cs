using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Entities.Community;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Entities.Duffel;
using Travio.Core.Domain.Entities.Hotelbeds;
using Travio.Core.Domain.Entities.TripPlaner;
using Travio.Infrastructure.Configrations.UserMangement;

namespace Travio.Infrastructure
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> Options) : base(Options) { }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Apply all IEntityTypeConfiguration<T> from the assembly
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // configure identity relation tables (schema + names)
            IdentityTablesConfiguration.Configure(builder);

        }
        public DbSet<UserCode> UserCodes { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Continent> Continents { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Destination> Destinations { get; set; }
        public DbSet<DestinationReview> DestinationReviews { get; set; }
        public DbSet<Interest> Interests { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<PostImage> PostImages { get; set; }
        public DbSet<FlightBooking> FlightBookings { get; set; }
        public DbSet<HotelBooking> HotelBookings { get; set; }
        public DbSet<HotelDestination> HotelDestinations { get; set; }
        public DbSet<HotelFacility> HotelFacilities { get; set; }

        // Chat & Trip Persistence
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<SavedTrip> SavedTrips { get; set; }
        public DbSet<SavedTripDay> SavedTripDays { get; set; }
        public DbSet<SavedTripActivity> SavedTripActivities { get; set; }
        public DbSet<SavedTripHotel> SavedTripHotels { get; set; }
    }
}

