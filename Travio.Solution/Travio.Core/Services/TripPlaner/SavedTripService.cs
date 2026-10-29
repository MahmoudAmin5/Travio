using Microsoft.EntityFrameworkCore;
using Travio.Core.Contracts.Services.TripPlaner;
using Travio.Core.Domain.Entities.TripPlaner;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.DTOs.TripPlanerDTOs;
using Travio.Core.Helpers;

namespace Travio.Core.Services.TripPlaner;

public class SavedTripService : ISavedTripService
{
    private readonly IGenericRepository<SavedTrip> _tripRepo;

    public SavedTripService(IGenericRepository<SavedTrip> tripRepo)
    {
        _tripRepo = tripRepo;
    }

    public async Task<ServiceResponse<Pagination<SavedTripSummaryDto>>> GetUserTripsAsync(int pageIndex, int pageSize, string userId)
    {
        var baseQuery = _tripRepo.GetQueryable()
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        var totalCount = await baseQuery.CountAsync();

        // Rule 6: Direct projection to DTO via .Select(...)
        var pagedTrips = await baseQuery
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new SavedTripSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                DestinationName = t.DestinationName,
                CityHeroImage = t.CityHeroImage,
                TotalDays = t.TotalDays,
                IsFavorite = t.IsFavorite,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        var pagination = new Pagination<SavedTripSummaryDto>(pageIndex, pageSize, totalCount, pagedTrips);
        return new ServiceResponse<Pagination<SavedTripSummaryDto>>(pagination, "Trips retrieved successfully.");
    }

    public async Task<ServiceResponse<Pagination<SavedTripSummaryDto>>> GetUserFavoriteTripsAsync(int pageIndex, int pageSize, string userId)
    {
        var baseQuery = _tripRepo.GetQueryable()
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.IsFavorite);

        var totalCount = await baseQuery.CountAsync();

        // Rule 6: Direct projection to DTO via .Select(...)
        var pagedTrips = await baseQuery
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new SavedTripSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                DestinationName = t.DestinationName,
                CityHeroImage = t.CityHeroImage,
                TotalDays = t.TotalDays,
                IsFavorite = t.IsFavorite,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        var pagination = new Pagination<SavedTripSummaryDto>(pageIndex, pageSize, totalCount, pagedTrips);
        return new ServiceResponse<Pagination<SavedTripSummaryDto>>(pagination, "Favorite trips retrieved successfully.");
    }

    public async Task<ServiceResponse<SavedTripDetailDto>> GetTripByIdAsync(int tripId, string userId)
    {
        // Rule 6: Direct projection to DTO via .Select(...)
        var dto = await _tripRepo.GetQueryable()
            .AsNoTracking()
            .Where(t => t.Id == tripId && t.UserId == userId)
            .Select(t => new SavedTripDetailDto
            {
                Id = t.Id,
                Title = t.Title,
                DestinationName = t.DestinationName,
                CityHeroImage = t.CityHeroImage,
                TotalDays = t.TotalDays,
                IsFavorite = t.IsFavorite,
                CreatedAt = t.CreatedAt,
                Days = t.Days
                    .OrderBy(d => d.DayNumber)
                    .Select(d => new SavedTripDayDto
                    {
                        DayNumber = d.DayNumber,
                        Theme = d.Theme,
                        Activities = d.Activities.Select(a => new SavedTripActivityDto
                        {
                            ActivityType = a.ActivityType,
                            PlaceName = a.PlaceName,
                            SuggestedTime = a.SuggestedTime,
                            Description = a.Description,
                            Address = a.Address,
                            FeaturedImage = a.FeaturedImage,
                            Latitude = a.Latitude,
                            Longitude = a.Longitude
                        }).ToList()
                    }).ToList(),
                Hotels = t.Hotels.Select(h => new SavedTripHotelDto
                {
                    Name = h.Name,
                    Description = h.Description,
                    Rating = h.Rating,
                    Address = h.Address,
                    Link = h.Link,
                    FeaturedImage = h.FeaturedImage,
                    Latitude = h.Latitude,
                    Longitude = h.Longitude
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (dto == null)
            return new ServiceResponse<SavedTripDetailDto>("Trip not found.");

        return new ServiceResponse<SavedTripDetailDto>(dto, "Trip retrieved successfully.");
    }

    public async Task<ServiceResponse<bool>> ToggleFavoriteAsync(int tripId, string userId)
    {
        var currentFav = await _tripRepo.GetQueryable()
            .AsNoTracking()
            .Where(t => t.Id == tripId && t.UserId == userId)
            .Select(t => (bool?)t.IsFavorite)
            .FirstOrDefaultAsync();

        if (currentFav == null)
            return new ServiceResponse<bool>("Trip not found.");

        var newFavState = !currentFav.Value;

        // Rule 3: Bulk update directly via ExecuteUpdateAsync
        await _tripRepo.GetQueryable()
            .Where(t => t.Id == tripId && t.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsFavorite, newFavState));

        var message = newFavState ? "Trip added to favorites." : "Trip removed from favorites.";
        return new ServiceResponse<bool>(true, message);
    }

    public async Task<ServiceResponse<bool>> DeleteTripAsync(int tripId, string userId)
    {
        // Rule 3: Bulk delete directly via ExecuteDeleteAsync
        var rowsDeleted = await _tripRepo.GetQueryable()
            .Where(t => t.Id == tripId && t.UserId == userId)
            .ExecuteDeleteAsync();

        if (rowsDeleted == 0)
            return new ServiceResponse<bool>("Trip not found.");

        return new ServiceResponse<bool>(true, "Trip deleted successfully.");
    }
}
