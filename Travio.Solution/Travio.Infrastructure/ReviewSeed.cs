using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Infrastructure;

public static class ReviewSeed
{
    private static readonly (string FirstName, string LastName, string Email)[] FakeUsers =
    [
        ("Sarah", "Mitchell", "sarah.mitchell@seed.travio.com"),
        ("Ahmed", "Al-Rashid", "ahmed.alrashid@seed.travio.com"),
        ("Emily", "Chen", "emily.chen@seed.travio.com"),
        ("Marco", "Rossi", "marco.rossi@seed.travio.com"),
        ("Fatima", "Hassan", "fatima.hassan@seed.travio.com"),
        ("James", "O'Brien", "james.obrien@seed.travio.com"),
        ("Yuki", "Tanaka", "yuki.tanaka@seed.travio.com"),
        ("Lena", "Schneider", "lena.schneider@seed.travio.com"),
        ("Carlos", "Mendoza", "carlos.mendoza@seed.travio.com"),
        ("Priya", "Sharma", "priya.sharma@seed.travio.com"),
        ("David", "Williams", "david.williams@seed.travio.com"),
        ("Sofia", "Martinez", "sofia.martinez@seed.travio.com"),
        ("Omar", "El-Sayed", "omar.elsayed@seed.travio.com"),
        ("Anna", "Petrova", "anna.petrova@seed.travio.com"),
        ("Lucas", "Dubois", "lucas.dubois@seed.travio.com"),
        ("Mei", "Wong", "mei.wong@seed.travio.com"),
        ("Hassan", "Yilmaz", "hassan.yilmaz@seed.travio.com"),
        ("Isabella", "Santos", "isabella.santos@seed.travio.com"),
        ("Daniel", "Andersen", "daniel.andersen@seed.travio.com"),
        ("Amara", "Okafor", "amara.okafor@seed.travio.com"),
        ("Noah", "Taylor", "noah.taylor@seed.travio.com"),
        ("Rania", "Khalil", "rania.khalil@seed.travio.com"),
        ("Viktor", "Novak", "viktor.novak@seed.travio.com"),
        ("Chloe", "Laurent", "chloe.laurent@seed.travio.com"),
        ("Raj", "Patel", "raj.patel@seed.travio.com"),
        ("Elena", "Vasquez", "elena.vasquez@seed.travio.com"),
        ("Kenji", "Nakamura", "kenji.nakamura@seed.travio.com"),
        ("Maria", "Kowalski", "maria.kowalski@seed.travio.com"),
        ("Ali", "Demir", "ali.demir@seed.travio.com"),
        ("Grace", "Kim", "grace.kim@seed.travio.com"),
    ];

    private static readonly string[] PositiveComments =
    [
        "Absolutely breathtaking! One of the best places I've ever visited.",
        "A must-see destination. The beauty of this place is beyond words.",
        "Incredible experience! The atmosphere is so unique and magical.",
        "Stunning views and rich history. Worth every minute spent here.",
        "We had an amazing time. The locals were so friendly and welcoming.",
        "One of the highlights of our trip. Highly recommend visiting!",
        "Perfect spot for photography lovers. Every angle is picture-worthy.",
        "A truly unforgettable experience. Can't wait to come back!",
        "Beautiful architecture and amazing surroundings. A real gem!",
        "This place exceeded all my expectations. Absolutely wonderful!",
    ];

    private static readonly string[] NeutralComments =
    [
        "Nice place to visit. It was a decent experience overall.",
        "Good destination, but it can get quite crowded during peak hours.",
        "Worth a visit if you're in the area. The scenery is pleasant.",
        "Interesting place with some history. Could use better facilities.",
        "A solid experience. Not the best I've seen, but still enjoyable.",
    ];

    private static readonly string[] MixedComments =
    [
        "The location is great, but it was a bit overpriced for what it offers.",
        "Beautiful but overcrowded. Try visiting early morning for a better experience.",
        "Scenic views, though the area could be maintained better.",
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Skip if reviews already exist
        if (await context.DestinationReviews.AnyAsync())
            return;

        // 1. Create or retrieve the seed users
        var seedUserIds = new List<string>();

        foreach (var (firstName, lastName, email) in FakeUsers)
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                seedUserIds.Add(existingUser.Id);
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                RegistrationDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 365))
            };

            var result = await userManager.CreateAsync(user, "Seed@Review123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "User");
                seedUserIds.Add(user.Id);
            }
            else
            {
                Console.WriteLine($"[ReviewSeed] Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        if (seedUserIds.Count == 0)
        {
            Console.WriteLine("[ReviewSeed] No seed users available. Skipping review seeding.");
            return;
        }

        // 2. Load all destinations
        var destinations = await context.Destinations.ToListAsync();

        if (destinations.Count == 0)
        {
            Console.WriteLine("[ReviewSeed] No destinations found. Skipping review seeding.");
            return;
        }

        var random = new Random(42); // fixed seed for reproducibility
        var now = DateTime.UtcNow;
        var reviews = new List<DestinationReview>();

        foreach (var destination in destinations)
        {
            // Each destination gets exactly 30 reviews
            var reviewCount = 30;

            // Shuffle users and pick reviewCount of them
            var shuffledUsers = seedUserIds.OrderBy(_ => random.Next()).Take(reviewCount).ToList();

            var destinationRatings = new List<int>();

            foreach (var userId in shuffledUsers)
            {
                // Weighted rating: mostly 4-5, some 3, rare 2
                int rating = random.Next(1, 101) switch
                {
                    <= 5 => 2,
                    <= 15 => 3,
                    <= 45 => 4,
                    _ => 5
                };

                destinationRatings.Add(rating);

                // Pick a comment based on rating
                string? comment = rating switch
                {
                    >= 4 => PositiveComments[random.Next(PositiveComments.Length)],
                    3 => NeutralComments[random.Next(NeutralComments.Length)],
                    _ => MixedComments[random.Next(MixedComments.Length)],
                };

                // 15% chance of no comment (rating only)
                if (random.Next(1, 101) <= 15)
                    comment = null;

                var createdAt = now.AddDays(-random.Next(7, 300)).AddHours(-random.Next(0, 24));

                reviews.Add(new DestinationReview
                {
                    DestinationId = destination.DestinationID,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    HelpfulVotes = random.Next(0, 50),
                    IsActive = true,
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt
                });
            }

            // Update destination aggregates
            destination.TotalReviews = destinationRatings.Count;
            destination.Rating = Math.Round(destinationRatings.Average(), 1);
        }

        context.DestinationReviews.AddRange(reviews);
        context.Destinations.UpdateRange(destinations);
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ ReviewSeed: Created {seedUserIds.Count} seed users and {reviews.Count} reviews across {destinations.Count} destinations.");
    }
}
