using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Travio.Core.Contracts.Services.Destination;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Enums;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.EntityErrors;
using Travio.Core.Helpers;

namespace Travio.Core.Services.Destinations;

public class DestinationService : IDestinationService
{
    private readonly IGenericRepository<Destination> _destinationRepository;
    private readonly IGenericRepository<Country> _countryRepository;
    private readonly IGenericRepository<DestinationReview> _destinationReviewRepository;
    private readonly IValidator<DestinationReviewUpsertDto> _destinationReviewValidator;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Expression<Func<Destination, DestinationDto>> ToDestinationDto = d => new DestinationDto
    {
        DestinationID = d.DestinationID,
        Name = d.Name,
        Description = d.Description,
        Rating = d.Rating,
        TotalReviews = d.TotalReviews,
        Latitude = d.Latitude,
        Longitude = d.Longitude,
        CityName = d.City.Name,
        ImageUrls = d.Images.Select(img => img.ImageURL).ToList(),
        Interests = d.DestinationInterests.Select(di => new InterestDto
        {
            InterestID = di.Interest.InterestID,
            InterestName = di.Interest.InterestName
        }).ToList()
    };

    public DestinationService(
        IGenericRepository<Destination> destinationRepository,
        IGenericRepository<Country> countryRepository,
        IGenericRepository<DestinationReview> destinationReviewRepository,
        IValidator<DestinationReviewUpsertDto> destinationReviewValidator,
        IUnitOfWork unitOfWork)
    {
        _destinationRepository = destinationRepository;
        _countryRepository = countryRepository;
        _destinationReviewRepository = destinationReviewRepository;
        _destinationReviewValidator = destinationReviewValidator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Pagination<DestinationDto>> GetAllAsync(int pageIndex, int pageSize, int? cityId, int? countryId, int? interestId, DestinationSortBy sortBy = DestinationSortBy.Newest)
    {
        var skip = (pageIndex - 1) * pageSize;
        var query = _destinationRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => (!cityId.HasValue || x.CityID == cityId) &&
                        (!interestId.HasValue || x.DestinationInterests.Any(di => di.InterestID == interestId)) &&
                        (!countryId.HasValue || x.City.CountryID == countryId));

        var totalItems = await query.CountAsync();

        var sortedQuery = sortBy switch
        {
            DestinationSortBy.Rating => query.OrderByDescending(x => x.Rating),
            DestinationSortBy.RatingAsc => query.OrderBy(x => x.Rating),
            DestinationSortBy.Name => query.OrderBy(x => x.Name),
            DestinationSortBy.NameDesc => query.OrderByDescending(x => x.Name),
            DestinationSortBy.Reviews => query.OrderByDescending(x => x.TotalReviews),
            _ => query.OrderByDescending(x => x.DestinationID)
        };

        // Rule 6: Project directly to DTO via .Select(...)
        var dataDto = await sortedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(ToDestinationDto)
            .ToListAsync();

        return new Pagination<DestinationDto>(pageIndex, pageSize, totalItems, dataDto);
    }

    public async Task<DestinationDto?> GetByIdAsync(int id)
    {
        return await _destinationRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.DestinationID == id)
            .Select(ToDestinationDto)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DestinationDto>> GetTopRatedAsync(int count = 10)
    {
        return await _destinationRepository.GetQueryable()
            .AsNoTracking()
            .OrderByDescending(x => x.Rating)
            .Take(count)
            .Select(ToDestinationDto)
            .ToListAsync();
    }

    public async Task<Pagination<DestinationDto>> SearchByNameAsync(string? keyword, int pageIndex, int pageSize, List<int>? interestIds = null)
    {
        var skip = (pageIndex - 1) * pageSize;
        var query = _destinationRepository.GetQueryable().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lowerKeyword = keyword.ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(lowerKeyword) ||
                x.Description.ToLower().Contains(lowerKeyword) ||
                x.City.Name.ToLower().Contains(lowerKeyword) ||
                x.City.Country.Name.ToLower().Contains(lowerKeyword) ||
                x.City.Country.Continent.Name.ToLower().Contains(lowerKeyword) ||
                x.DestinationInterests.Any(di =>
                    di.Interest.InterestName.ToLower().Contains(lowerKeyword)));
        }

        if (interestIds is { Count: > 0 })
        {
            query = query.Where(x => x.DestinationInterests.Any(di => interestIds.Contains(di.InterestID)));
        }

        var totalItems = await query.CountAsync();

        var dataDto = await query
            .OrderByDescending(x => x.Rating)
            .Skip(skip)
            .Take(pageSize)
            .Select(ToDestinationDto)
            .ToListAsync();

        return new Pagination<DestinationDto>(pageIndex, pageSize, totalItems, dataDto);
    }

    public async Task<IEnumerable<DestinationDto>> GetNearbyAsync(decimal latitude, decimal longitude, double radiusKm, int count = 10)
    {
        var degreeRadius = (decimal)(radiusKm / 111.0);

        return await _destinationRepository.GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.Latitude >= latitude - degreeRadius && x.Latitude <= latitude + degreeRadius &&
                x.Longitude >= longitude - degreeRadius && x.Longitude <= longitude + degreeRadius)
            .OrderByDescending(x => x.Rating)
            .Take(count)
            .Select(ToDestinationDto)
            .ToListAsync();
    }

    public async Task<IEnumerable<CountryDto>> GetFamousCountriesAsync()
    {
        return await _countryRepository.GetQueryable()
            .AsNoTracking()
            .Where(c => c.Cities.Any(city => city.Destinations.Any()))
            .Select(c => new CountryDto
            {
                CountryID = c.CountryID,
                Name = c.Name,
                ImageURL = c.ImageURL
            })
            .ToListAsync();
    }

    public async Task<Pagination<DestinationReviewDto>> GetReviewsAsync(int destinationId, int pageIndex = 1, int pageSize = 10, string? currentUserId = null)
    {
        if (pageIndex <= 0)
        {
            throw new ArgumentException("Page index must be greater than 0.", nameof(pageIndex));
        }

        if (pageSize <= 0 || pageSize > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));
        }

        await EnsureDestinationExistsAsync(destinationId);

        var skip = (pageIndex - 1) * pageSize;
        var baseQuery = _destinationReviewRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.DestinationId == destinationId && x.IsActive);

        var totalItems = await baseQuery.CountAsync();

        // Rule 6: Direct projection to DTO via .Select(...)
        var items = await baseQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.ReviewId)
            .Skip(skip)
            .Take(pageSize)
            .Select(review => new DestinationReviewDto
            {
                ReviewId = review.ReviewId,
                ReviewerName = (review.User.FirstName + " " + review.User.LastName).Trim(),
                ReviewerImageUrl = review.User.ProfilePictureURL,
                ReviewDateUtc = review.CreatedAtUtc,
                Rating = review.Rating,
                Comment = review.Comment,
                HelpfulVotes = review.HelpfulVotes,
                IsMine = !string.IsNullOrWhiteSpace(currentUserId) && review.UserId == currentUserId
            })
            .ToListAsync();

        return new Pagination<DestinationReviewDto>(pageIndex, pageSize, totalItems, items);
    }

    public async Task<DestinationReviewMutationDto> UpsertMyReviewAsync(int destinationId, string userId, DestinationReviewUpsertDto dto)
    {
        await ValidateReviewInputAsync(dto);
        await EnsureDestinationExistsAsync(destinationId);

        var normalizedComment = NormalizeComment(dto.Comment);
        var now = DateTime.UtcNow;

        return await _unitOfWork.ExecuteAsync(async () =>
        {
            // Rule 3 Upsert Pattern: Attempt ExecuteUpdateAsync first; if rowsAffected == 0, call AddAsync
            var rowsAffected = await _destinationReviewRepository.GetQueryable()
                .Where(x => x.DestinationId == destinationId && x.UserId == userId && x.IsActive)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Rating, dto.Rating)
                    .SetProperty(r => r.Comment, normalizedComment)
                    .SetProperty(r => r.UpdatedAtUtc, now));

            int reviewId;
            if (rowsAffected == 0)
            {
                var newReview = new DestinationReview
                {
                    DestinationId = destinationId,
                    UserId = userId,
                    Rating = dto.Rating,
                    Comment = normalizedComment,
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };

                await _destinationReviewRepository.AddAsync(newReview);
                await _unitOfWork.SaveChangesAsync();
                reviewId = newReview.ReviewId;
            }
            else
            {
                reviewId = await _destinationReviewRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(x => x.DestinationId == destinationId && x.UserId == userId && x.IsActive)
                    .Select(x => x.ReviewId)
                    .FirstAsync();
            }

            var aggregate = await RefreshDestinationReviewAggregatesAsync(destinationId);

            return new DestinationReviewMutationDto
            {
                ReviewId = reviewId,
                DestinationId = destinationId,
                Rating = dto.Rating,
                Comment = normalizedComment,
                UpdatedAtUtc = now,
                AverageRating = aggregate.averageRating,
                TotalReviews = aggregate.totalReviews
            };
        });
    }

    public async Task<DestinationReviewMutationDto> UpdateMyReviewAsync(int destinationId, string userId, DestinationReviewUpsertDto dto)
    {
        await ValidateReviewInputAsync(dto);
        await EnsureDestinationExistsAsync(destinationId);

        var normalizedComment = NormalizeComment(dto.Comment);
        var now = DateTime.UtcNow;

        return await _unitOfWork.ExecuteAsync(async () =>
        {
            var rowsAffected = await _destinationReviewRepository.GetQueryable()
                .Where(x => x.DestinationId == destinationId && x.UserId == userId && x.IsActive)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Rating, dto.Rating)
                    .SetProperty(r => r.Comment, normalizedComment)
                    .SetProperty(r => r.UpdatedAtUtc, now));

            if (rowsAffected == 0)
            {
                throw new NotFoundException("Active review not found for this destination.");
            }

            var reviewId = await _destinationReviewRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.DestinationId == destinationId && x.UserId == userId && x.IsActive)
                .Select(x => x.ReviewId)
                .FirstAsync();

            var aggregate = await RefreshDestinationReviewAggregatesAsync(destinationId);

            return new DestinationReviewMutationDto
            {
                ReviewId = reviewId,
                DestinationId = destinationId,
                Rating = dto.Rating,
                Comment = normalizedComment,
                UpdatedAtUtc = now,
                AverageRating = aggregate.averageRating,
                TotalReviews = aggregate.totalReviews
            };
        });
    }

    public async Task<DestinationReviewDeleteResultDto> DeleteMyReviewAsync(int destinationId, string userId)
    {
        await EnsureDestinationExistsAsync(destinationId);

        return await _unitOfWork.ExecuteAsync(async () =>
        {
            var now = DateTime.UtcNow;
            var rowsAffected = await _destinationReviewRepository.GetQueryable()
                .Where(x => x.DestinationId == destinationId && x.UserId == userId && x.IsActive)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.IsActive, false)
                    .SetProperty(r => r.UpdatedAtUtc, now));

            if (rowsAffected == 0)
            {
                throw new NotFoundException("Active review not found for this destination.");
            }

            var aggregate = await RefreshDestinationReviewAggregatesAsync(destinationId);

            return new DestinationReviewDeleteResultDto
            {
                DestinationId = destinationId,
                Deleted = true,
                AverageRating = aggregate.averageRating,
                TotalReviews = aggregate.totalReviews
            };
        });
    }

    private static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        return comment.Trim();
    }

    private async Task ValidateReviewInputAsync(DestinationReviewUpsertDto dto)
    {
        ValidationResult validationResult = await _destinationReviewValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var message = string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage));
            throw new ValidationException(message, validationResult.Errors);
        }
    }

    private async Task EnsureDestinationExistsAsync(int destinationId)
    {
        var exists = await _destinationRepository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(d => d.DestinationID == destinationId);

        if (!exists)
        {
            throw new NotFoundException("Destination", destinationId);
        }
    }

    private async Task<(decimal averageRating, int totalReviews)> RefreshDestinationReviewAggregatesAsync(int destinationId)
    {
        var stats = await _destinationReviewRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.DestinationId == destinationId && x.IsActive)
            .GroupBy(x => x.DestinationId)
            .Select(g => new
            {
                TotalReviews = g.Count(),
                AverageRating = g.Average(x => (double)x.Rating)
            })
            .FirstOrDefaultAsync();

        var totalReviews = stats?.TotalReviews ?? 0;
        var avgRating = stats?.AverageRating ?? 0.0;

        // Rule 3: Bulk update destination aggregates without loading tracked entity
        await _destinationRepository.GetQueryable()
            .Where(d => d.DestinationID == destinationId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.TotalReviews, totalReviews)
                .SetProperty(d => d.Rating, avgRating));

        return (Math.Round((decimal)avgRating, 1, MidpointRounding.AwayFromZero), totalReviews);
    }

    public async Task<IEnumerable<DestinationDto>> GetSuggestedAsync(int destinationId, int count = 10)
    {
        var currentDest = await _destinationRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.DestinationID == destinationId)
            .Select(x => new
            {
                x.City.CountryID,
                ContinentID = (int?)x.City.Country.ContinentID ?? 0,
                InterestIds = x.DestinationInterests.Select(di => di.InterestID).ToList()
            })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Destination", destinationId);

        var interestIds = currentDest.InterestIds;
        var countryId = currentDest.CountryID;
        var continentId = currentDest.ContinentID;

        if (interestIds.Count > 0)
        {
            return await _destinationRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.DestinationID != destinationId &&
                            x.DestinationInterests.Any(di => interestIds.Contains(di.InterestID)))
                .OrderByDescending(x => x.City.CountryID == countryId ? 1 : 0)
                .ThenByDescending(x => x.City.Country.ContinentID == continentId ? 1 : 0)
                .ThenByDescending(x => x.DestinationInterests.Count(di => interestIds.Contains(di.InterestID)))
                .ThenByDescending(x => x.Rating)
                .Take(count)
                .Select(ToDestinationDto)
                .ToListAsync();
        }

        return await _destinationRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.DestinationID != destinationId && x.City.CountryID == countryId)
            .OrderByDescending(x => x.Rating)
            .Take(count)
            .Select(ToDestinationDto)
            .ToListAsync();
    }
}
