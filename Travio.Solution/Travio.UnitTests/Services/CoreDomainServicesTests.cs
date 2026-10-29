using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Entities.Community;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Entities.TripPlaner;
using Travio.Core.DTOs.CommunityDTO;
using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.Services.Community;
using Travio.Core.Services.Destinations;
using Travio.Core.Services.Survey;
using Travio.Core.Services.TripPlaner;
using Travio.Infrastructure.Repositories;
using Travio.UnitTests.Fixtures;

namespace Travio.UnitTests.Services;

public class CoreDomainServicesTests
{
    private sealed class InlineReviewValidator : AbstractValidator<DestinationReviewUpsertDto>
    {
        public InlineReviewValidator()
        {
            RuleFor(x => x.Rating).InclusiveBetween(1, 5);
            RuleFor(x => x.Comment).MaximumLength(500);
        }
    }

    [Fact]
    public async Task DestinationService_GetAll_Search_UpsertReview_UpdateReview_DeleteReview_ShouldMaintainAggregatesAndProjectToDtos()
    {
        using var db = new SqliteTestDatabase();
        var destRepo = new GenericRepository<Destination>(db.Context);
        var countryRepo = new GenericRepository<Country>(db.Context);
        var reviewRepo = new GenericRepository<DestinationReview>(db.Context);
        var userRepo = new GenericRepository<ApplicationUser>(db.Context);
        var uow = new UnitOfWork(db.Context);

        // Seed Country, City, Destination, and ApplicationUser
        await uow.ExecuteAsync(async () =>
        {
            await userRepo.AddAsync(new ApplicationUser
            {
                Id = "user-rev-1",
                UserName = "sara@travio.com",
                Email = "sara@travio.com",
                FirstName = "Sara",
                LastName = "Ali",
                ProfilePictureURL = "https://example.com/sara.jpg"
            });

            db.Context.Continents.Add(new Continent
            {
                ContinentID = 1,
                Name = "Africa"
            });

            var country = new Country
            {
                CountryID = 1,
                Name = "Egypt",
                FlagURL = "https://example.com/eg-flag.jpg",
                ImageURL = "https://example.com/egypt.jpg",
                ContinentID = 1
            };
            await countryRepo.AddAsync(country);

            db.Context.Cities.Add(new City
            {
                CityID = 10,
                Name = "Luxor",
                CountryID = 1
            });

            await destRepo.AddAsync(new Destination
            {
                DestinationID = 100,
                Name = "Karnak Temple",
                Description = "Ancient Egyptian temple complex in Luxor",
                CityID = 10,
                Latitude = 25.7188m,
                Longitude = 32.6573m,
                Rating = 0d,
                TotalReviews = 0
            });
        });

        var service = new DestinationService(
            destRepo,
            countryRepo,
            reviewRepo,
            new InlineReviewValidator(),
            uow);

        // 1. Search by keyword
        var searchResult = await service.SearchByNameAsync("Karnak", 1, 10);
        searchResult.Count.Should().Be(1);
        searchResult.Data.First().Name.Should().Be("Karnak Temple");
        searchResult.Data.First().CityName.Should().Be("Luxor");

        // 2. Upsert Review (Initial Creation)
        var createReview = await service.UpsertMyReviewAsync(100, "user-rev-1", new DestinationReviewUpsertDto
        {
            Rating = 5,
            Comment = "Breathtaking historical monument!"
        });

        createReview.TotalReviews.Should().Be(1);
        createReview.AverageRating.Should().Be(5.0m);

        // 3. Upsert Review again by same user (Rule 3 Upsert should UPDATE in place via ExecuteUpdateAsync)
        var upsertUpdate = await service.UpsertMyReviewAsync(100, "user-rev-1", new DestinationReviewUpsertDto
        {
            Rating = 4,
            Comment = "Updated rating to 4 stars after second visit."
        });

        upsertUpdate.TotalReviews.Should().Be(1);
        upsertUpdate.AverageRating.Should().Be(4.0m);

        // 4. GetReviewsAsync (Rule 6 projection with IsMine)
        var reviewsPage = await service.GetReviewsAsync(100, 1, 10, "user-rev-1");
        reviewsPage.Count.Should().Be(1);
        reviewsPage.Data.First().IsMine.Should().BeTrue();
        reviewsPage.Data.First().ReviewerName.Should().Be("Sara Ali");

        // 5. DeleteMyReviewAsync (Soft-delete via ExecuteUpdateAsync + aggregate refresh)
        var deleteResult = await service.DeleteMyReviewAsync(100, "user-rev-1");
        deleteResult.Deleted.Should().BeTrue();
        deleteResult.TotalReviews.Should().Be(0);
        deleteResult.AverageRating.Should().Be(0m);
    }

    [Fact]
    public async Task UserFavoriteService_Add_Get_And_ExecuteDelete_ShouldWorkCorrectly()
    {
        using var db = new SqliteTestDatabase();
        var favRepo = new GenericRepository<UserFavorite>(db.Context);
        var destRepo = new GenericRepository<Destination>(db.Context);
        var uow = new UnitOfWork(db.Context);

        await uow.ExecuteAsync(async () =>
        {
            db.Context.Cities.Add(new City
            {
                CityID = 20,
                Name = "Aswan",
                CountryID = 1
            });

            await destRepo.AddAsync(new Destination
            {
                DestinationID = 200,
                Name = "Philae Temple",
                Description = "Island temple in Aswan",
                CityID = 20,
                Rating = 4.9d,
                TotalReviews = 42
            });
        });

        var favService = new UserFavoriteService(favRepo, uow);

        // Add favorite
        var addRes = await favService.AddFavoriteAsync("user-fav-1", 200);
        addRes.Success.Should().BeTrue();

        // Duplicate add should fail gracefully
        var dupRes = await favService.AddFavoriteAsync("user-fav-1", 200);
        dupRes.Success.Should().BeFalse();

        // Get favorites via Rule 6 projection
        var listRes = await favService.GetUserFavoritesAsync(1, 10, "user-fav-1");
        listRes.Success.Should().BeTrue();
        listRes.Data!.Count.Should().Be(1);
        listRes.Data.Data.First().Name.Should().Be("Philae Temple");

        // Delete favorite via Rule 3 ExecuteDeleteAsync
        var delRes = await favService.DeleteFavoriteAsync("user-fav-1", 200);
        delRes.Success.Should().BeTrue();

        var afterDel = await favService.GetUserFavoritesAsync(1, 10, "user-fav-1");
        afterDel.Data!.Count.Should().Be(0);
    }

    [Fact]
    public async Task CommunityService_CreatePost_Feed_ToggleLike_Comment_Delete_ShouldWorkCorrectly()
    {
        using var db = new SqliteTestDatabase();
        var postRepo = new GenericRepository<Post>(db.Context);
        var imageRepo = new GenericRepository<PostImage>(db.Context);
        var likeRepo = new GenericRepository<PostLike>(db.Context);
        var commentRepo = new GenericRepository<Comment>(db.Context);
        var userRepo = new GenericRepository<ApplicationUser>(db.Context);
        var uow = new UnitOfWork(db.Context);

        await uow.ExecuteAsync(async () =>
        {
            await userRepo.AddAsync(new ApplicationUser
            {
                Id = "user-comm-1",
                UserName = "karim@travio.com",
                Email = "karim@travio.com",
                FirstName = "Karim",
                LastName = "Nabil",
                ProfilePictureURL = "https://example.com/karim.jpg"
            });
        });

        var communityService = new CommunityService(
            postRepo,
            imageRepo,
            likeRepo,
            commentRepo,
            userRepo,
            uow);

        // 1. Create Post
        var createRes = await communityService.CreatePostAsync("user-comm-1", new CreatePostDTO
        {
            Location = "Luxor, Egypt",
            Content = "Sailing from Luxor to Aswan was unforgettable!"
        });
        createRes.Success.Should().BeTrue();
        var postId = createRes.Data!.Id;

        // 2. Toggle Like (Like)
        var likeRes = await communityService.ToggleLikeAsync(postId, "user-comm-1");
        likeRes.Success.Should().BeTrue();
        likeRes.Data.Should().BeTrue("First toggle likes the post");

        // 3. Add Comment
        var commentRes = await communityService.AddCommentAsync(postId, "user-comm-1", new CreateCommentDTO
        {
            Content = "Totally agree, the sunset views are magical!"
        });
        commentRes.Success.Should().BeTrue();
        var commentId = commentRes.Data!.Id;

        // 4. Get Post Details via Rule 6 projection
        var detailsRes = await communityService.GetPostByIdAsync(postId, "user-comm-1");
        detailsRes.Success.Should().BeTrue();
        detailsRes.Data!.LikesCount.Should().Be(1);
        detailsRes.Data.IsLikedByCurrentUser.Should().BeTrue();
        detailsRes.Data.Comments.Should().ContainSingle(c => c.Id == commentId);

        // 5. Toggle Like again (Unlike via ExecuteDeleteAsync)
        var unlikeRes = await communityService.ToggleLikeAsync(postId, "user-comm-1");
        unlikeRes.Success.Should().BeTrue();
        unlikeRes.Data.Should().BeFalse("Second toggle unlikes the post");

        // 6. Delete Comment via ExecuteDeleteAsync
        var delCommentRes = await communityService.DeleteCommentAsync(commentId, "user-comm-1");
        delCommentRes.Success.Should().BeTrue();

        // 7. Delete Post via ExecuteDeleteAsync
        var delPostRes = await communityService.DeletePostAsync(postId, "user-comm-1");
        delPostRes.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Survey_ChatHistory_And_SavedTripService_ShouldProjectAndMutateViaRule3AndRule6()
    {
        using var db = new SqliteTestDatabase();
        var prefRepo = new GenericRepository<UserPreference>(db.Context);
        var sessionRepo = new GenericRepository<ChatSession>(db.Context);
        var messageRepo = new GenericRepository<ChatMessage>(db.Context);
        var tripRepo = new GenericRepository<SavedTrip>(db.Context);
        var uow = new UnitOfWork(db.Context);

        // 1. SurveyService bulk insert inside UnitOfWork
        var surveyService = new SurveyService(prefRepo, uow);
        await surveyService.AddUserPreferencesAsync("user-trip-1", new List<(int CategoryId, int OptionId)>
        {
            (1, 101),
            (2, 202),
            (3, 303)
        });

        var prefsCount = await db.Context.Set<UserPreference>().CountAsync(p => p.UserID == "user-trip-1");
        prefsCount.Should().Be(3);

        // 2. ChatHistoryService
        var chatSession = new ChatSession
        {
            UserId = "user-trip-1",
            ThreadId = "thread-abc-123",
            Title = "5 Days in Rome",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await uow.ExecuteAsync(async () =>
        {
            await sessionRepo.AddAsync(chatSession);
        });
        await uow.ExecuteAsync(async () =>
        {
            await messageRepo.AddAsync(new ChatMessage
            {
                ChatSessionId = chatSession.Id,
                Role = "user",
                Content = "Plan a 5-day trip to Rome",
                MessageType = "text",
                SentAt = DateTimeOffset.UtcNow
            });
        });

        var chatHistoryService = new ChatHistoryService(sessionRepo, messageRepo);
        var sessionsRes = await chatHistoryService.GetUserSessionsAsync(1, 10, "user-trip-1");
        sessionsRes.Success.Should().BeTrue();
        sessionsRes.Data!.Count.Should().Be(1);

        var messagesRes = await chatHistoryService.GetSessionMessagesAsync(chatSession.Id, 1, 50, "user-trip-1");
        messagesRes.Success.Should().BeTrue();
        messagesRes.Data!.Count.Should().Be(1);

        var deleteSessionRes = await chatHistoryService.DeleteSessionAsync(chatSession.Id, "user-trip-1");
        deleteSessionRes.Success.Should().BeTrue();

        // 3. SavedTripService
        var savedTrip = new SavedTrip
        {
            UserId = "user-trip-1",
            Title = "Roman Holiday",
            DestinationName = "Rome, Italy",
            TotalDays = 3,
            IsFavorite = false,
            CreatedAt = DateTimeOffset.UtcNow,
            Days = new List<SavedTripDay>
            {
                new()
                {
                    DayNumber = 1,
                    Theme = "Ancient Rome",
                    Activities = new List<SavedTripActivity>
                    {
                        new() { PlaceName = "Colosseum Tour", SuggestedTime = "Morning" }
                    }
                }
            },
            Hotels = new List<SavedTripHotel>
            {
                new() { Name = "Hotel Artemide", Rating = 4.8 }
            }
        };

        await uow.ExecuteAsync(async () =>
        {
            await tripRepo.AddAsync(savedTrip);
        });

        var savedTripService = new SavedTripService(tripRepo);

        var detailRes = await savedTripService.GetTripByIdAsync(savedTrip.Id, "user-trip-1");
        detailRes.Success.Should().BeTrue();
        detailRes.Data!.Title.Should().Be("Roman Holiday");
        detailRes.Data.Days.Should().HaveCount(1);
        detailRes.Data.Hotels.Should().HaveCount(1);

        // Toggle Favorite via ExecuteUpdateAsync
        var favToggleRes = await savedTripService.ToggleFavoriteAsync(savedTrip.Id, "user-trip-1");
        favToggleRes.Success.Should().BeTrue();
        favToggleRes.Data.Should().BeTrue();

        var favTripsRes = await savedTripService.GetUserFavoriteTripsAsync(1, 10, "user-trip-1");
        favTripsRes.Data!.Count.Should().Be(1);

        // Delete Trip via ExecuteDeleteAsync
        var delTripRes = await savedTripService.DeleteTripAsync(savedTrip.Id, "user-trip-1");
        delTripRes.Success.Should().BeTrue();
    }
}
