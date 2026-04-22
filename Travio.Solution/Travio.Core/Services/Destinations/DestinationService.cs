using FluentValidation;
using FluentValidation.Results;
using Mapster;
using Travio.Core.Contracts.Services.Destination;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Enums;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Domain.Specifications.Destinations;
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

    public DestinationService(
        IGenericRepository<Destination> destinationRepository,
        IGenericRepository<Country> countryRepository,
        IGenericRepository<DestinationReview> destinationReviewRepository,
        IValidator<DestinationReviewUpsertDto> destinationReviewValidator)
    {
        _destinationRepository = destinationRepository;
        _countryRepository = countryRepository;
        _destinationReviewRepository = destinationReviewRepository;
        _destinationReviewValidator = destinationReviewValidator;
    }

    public async Task<Pagination<DestinationDto>> GetAllAsync(int pageIndex, int pageSize, int? cityId, int? countryId, int? interestId, DestinationSortBy sortBy = DestinationSortBy.Newest)
    {
        var skip = (pageIndex - 1) * pageSize;
        var dataSpec = new DestinationsWithFiltersSpec(cityId, interestId, countryId, skip, pageSize, sortBy);
        var countSpec = new DestinationFilterSpec(cityId, interestId, countryId);
        var totalItems = await _destinationRepository.CountAsync(countSpec);
        var data = await _destinationRepository.ListAsync(dataSpec);
        var dataDto = data.Adapt<IEnumerable<DestinationDto>>();
        return new Pagination<DestinationDto>(pageIndex, pageSize, totalItems, dataDto.ToList());
    }

    public async Task<DestinationDto?> GetByIdAsync(int id)
    {
        var spec = new DestinationByIdSpec(id);
        var destination = await _destinationRepository.FirstOrDefaultAsync(spec);
        return destination?.Adapt<DestinationDto>();
    }

    public async Task<IEnumerable<DestinationDto>> GetTopRatedAsync(int count = 10)
    {
        var spec = new TopRatedDestinationsSpec(count);
        var destinations = await _destinationRepository.ListAsync(spec);
        return destinations.Adapt<IEnumerable<DestinationDto>>();
    }

    public async Task<Pagination<DestinationDto>> SearchByNameAsync(string keyword, int pageIndex, int pageSize)
    {
        var skip = (pageIndex - 1) * pageSize;
        var dataSpec = new DestinationSearchSpec(keyword, skip, pageSize);
        var countSpec = new DestinationSearchCountSpec(keyword);
        var totalItems = await _destinationRepository.CountAsync(countSpec);
        var data = await _destinationRepository.ListAsync(dataSpec);
        var dataDto = data.Adapt<IEnumerable<DestinationDto>>();
        return new Pagination<DestinationDto>(pageIndex, pageSize, totalItems, dataDto.ToList());
    }

    public async Task<IEnumerable<DestinationDto>> GetNearbyAsync(decimal latitude, decimal longitude, double radiusKm, int count = 10)
    {
        var spec = new NearbyDestinationsSpec(latitude, longitude, radiusKm, count);
        var destinations = await _destinationRepository.ListAsync(spec);
        return destinations.Adapt<IEnumerable<DestinationDto>>();
    }

    public async Task<IEnumerable<CountryDto>> GetFamousCountriesAsync()
    {
        var spec = new CountriesWithDestinationsSpec();
        var countries = await _countryRepository.ListAsync(spec);
        return countries.Adapt<IEnumerable<CountryDto>>();
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

        var listSpec = new DestinationReviewsByDestinationSpec(destinationId, skip, pageSize);
        var countSpec = new ActiveDestinationReviewStatsSpec(destinationId);

        var reviews = await _destinationReviewRepository.ListAsync(listSpec);
        var totalItems = await _destinationReviewRepository.CountAsync(countSpec);

        var items = reviews.Select(review => new DestinationReviewDto
        {
            ReviewId = review.ReviewId,
            ReviewerName = $"{review.User.FirstName} {review.User.LastName}".Trim(),
            ReviewerImageUrl = review.User.ProfilePictureURL,
            ReviewDateUtc = review.CreatedAtUtc,
            Rating = review.Rating,
            Comment = review.Comment,
            HelpfulVotes = review.HelpfulVotes,
            IsMine = !string.IsNullOrWhiteSpace(currentUserId) && review.UserId == currentUserId
        }).ToList();

        return new Pagination<DestinationReviewDto>(pageIndex, pageSize, totalItems, items);
    }

    public async Task<DestinationReviewMutationDto> UpsertMyReviewAsync(int destinationId, string userId, DestinationReviewUpsertDto dto)
    {
        await ValidateReviewInputAsync(dto);
        var destination = await EnsureDestinationExistsAsync(destinationId);

        var normalizedComment = NormalizeComment(dto.Comment);
        var now = DateTime.UtcNow;

        var spec = new DestinationReviewByUserAndDestinationSpec(destinationId, userId);
        var existingReview = await _destinationReviewRepository.FirstOrDefaultAsync(spec);

        if (existingReview is null)
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

            existingReview = await _destinationReviewRepository.AddAsync(newReview);
        }
        else
        {
            existingReview.Rating = dto.Rating;
            existingReview.Comment = normalizedComment;
            existingReview.UpdatedAtUtc = now;
            await _destinationReviewRepository.UpdateAsync(existingReview);
        }

        var aggregate = await RefreshDestinationReviewAggregatesAsync(destination);

        return new DestinationReviewMutationDto
        {
            ReviewId = existingReview.ReviewId,
            DestinationId = destinationId,
            Rating = existingReview.Rating,
            Comment = existingReview.Comment,
            UpdatedAtUtc = existingReview.UpdatedAtUtc,
            AverageRating = aggregate.averageRating,
            TotalReviews = aggregate.totalReviews
        };
    }

    public async Task<DestinationReviewMutationDto> UpdateMyReviewAsync(int destinationId, string userId, DestinationReviewUpsertDto dto)
    {
        await ValidateReviewInputAsync(dto);
        var destination = await EnsureDestinationExistsAsync(destinationId);

        var spec = new DestinationReviewByUserAndDestinationSpec(destinationId, userId);
        var existingReview = await _destinationReviewRepository.FirstOrDefaultAsync(spec)
            ?? throw new NotFoundException("Active review not found for this destination.");

        existingReview.Rating = dto.Rating;
        existingReview.Comment = NormalizeComment(dto.Comment);
        existingReview.UpdatedAtUtc = DateTime.UtcNow;

        await _destinationReviewRepository.UpdateAsync(existingReview);

        var aggregate = await RefreshDestinationReviewAggregatesAsync(destination);

        return new DestinationReviewMutationDto
        {
            ReviewId = existingReview.ReviewId,
            DestinationId = destinationId,
            Rating = existingReview.Rating,
            Comment = existingReview.Comment,
            UpdatedAtUtc = existingReview.UpdatedAtUtc,
            AverageRating = aggregate.averageRating,
            TotalReviews = aggregate.totalReviews
        };
    }

    public async Task<DestinationReviewDeleteResultDto> DeleteMyReviewAsync(int destinationId, string userId)
    {
        var destination = await EnsureDestinationExistsAsync(destinationId);

        var spec = new DestinationReviewByUserAndDestinationSpec(destinationId, userId);
        var existingReview = await _destinationReviewRepository.FirstOrDefaultAsync(spec)
            ?? throw new NotFoundException("Active review not found for this destination.");

        existingReview.IsActive = false;
        existingReview.UpdatedAtUtc = DateTime.UtcNow;

        await _destinationReviewRepository.UpdateAsync(existingReview);

        var aggregate = await RefreshDestinationReviewAggregatesAsync(destination);

        return new DestinationReviewDeleteResultDto
        {
            DestinationId = destinationId,
            Deleted = true,
            AverageRating = aggregate.averageRating,
            TotalReviews = aggregate.totalReviews
        };
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

    private async Task<Destination> EnsureDestinationExistsAsync(int destinationId)
    {
        var destination = await _destinationRepository.GetByIdAsync(destinationId);
        if (destination is null)
        {
            throw new NotFoundException("Destination", destinationId);
        }

        return destination;
    }

    private async Task<(decimal averageRating, int totalReviews)> RefreshDestinationReviewAggregatesAsync(Destination destination)
    {
        var aggregateSpec = new ActiveDestinationReviewStatsSpec(destination.DestinationID);
        var activeReviews = await _destinationReviewRepository.ListAsync(aggregateSpec);

        destination.TotalReviews = activeReviews.Count;
        destination.Rating = activeReviews.Count == 0 ? 0 : activeReviews.Average(x => x.Rating);

        await _destinationRepository.UpdateAsync(destination);

        return (Math.Round((decimal)destination.Rating, 1, MidpointRounding.AwayFromZero), destination.TotalReviews);
    }
}
