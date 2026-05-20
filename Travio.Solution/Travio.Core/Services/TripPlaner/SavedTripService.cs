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
    private readonly IGenericRepository<SavedTripDay> _dayRepo;
    private readonly IGenericRepository<SavedTripActivity> _activityRepo;
    private readonly IGenericRepository<SavedTripHotel> _hotelRepo;

    public SavedTripService(
        IGenericRepository<SavedTrip> tripRepo,
        IGenericRepository<SavedTripDay> dayRepo,
        IGenericRepository<SavedTripActivity> activityRepo,
        IGenericRepository<SavedTripHotel> hotelRepo)
    {
        _tripRepo = tripRepo;
        _dayRepo = dayRepo;
        _activityRepo = activityRepo;
        _hotelRepo = hotelRepo;
    }

    public async Task<ServiceResponse<Pagination<SavedTripSummaryDto>>> GetUserTripsAsync(int pageIndex, int pageSize, string userId)
    {
        var allTrips = await _tripRepo.ListAsync();
        var userTrips = allTrips
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        var totalCount = userTrips.Count;
        var pagedTrips = userTrips
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new SavedTripSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                DestinationName = t.DestinationName,
                TotalDays = t.TotalDays,
                IsFavorite = t.IsFavorite,
                CreatedAt = t.CreatedAt
            })
            .ToList();

        var pagination = new Pagination<SavedTripSummaryDto>(pageIndex, pageSize, totalCount, pagedTrips);
        return new ServiceResponse<Pagination<SavedTripSummaryDto>>(pagination, "Trips retrieved successfully.");
    }

    public async Task<ServiceResponse<Pagination<SavedTripSummaryDto>>> GetUserFavoriteTripsAsync(int pageIndex, int pageSize, string userId)
    {
        var allTrips = await _tripRepo.ListAsync();
        var favTrips = allTrips
            .Where(t => t.UserId == userId && t.IsFavorite)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        var totalCount = favTrips.Count;
        var pagedTrips = favTrips
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new SavedTripSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                DestinationName = t.DestinationName,
                TotalDays = t.TotalDays,
                IsFavorite = t.IsFavorite,
                CreatedAt = t.CreatedAt
            })
            .ToList();

        var pagination = new Pagination<SavedTripSummaryDto>(pageIndex, pageSize, totalCount, pagedTrips);
        return new ServiceResponse<Pagination<SavedTripSummaryDto>>(pagination, "Favorite trips retrieved successfully.");
    }

    public async Task<ServiceResponse<SavedTripDetailDto>> GetTripByIdAsync(int tripId, string userId)
    {
        var trip = await _tripRepo.GetByIdAsync(tripId);
        if (trip == null || trip.UserId != userId)
            return new ServiceResponse<SavedTripDetailDto>("Trip not found.");

        // Load related data
        var allDays = await _dayRepo.ListAsync();
        var tripDays = allDays.Where(d => d.SavedTripId == tripId).OrderBy(d => d.DayNumber).ToList();

        var allActivities = await _activityRepo.ListAsync();
        var allHotels = await _hotelRepo.ListAsync();

        var dto = new SavedTripDetailDto
        {
            Id = trip.Id,
            Title = trip.Title,
            DestinationName = trip.DestinationName,
            TotalDays = trip.TotalDays,
            IsFavorite = trip.IsFavorite,
            CreatedAt = trip.CreatedAt,
            Days = tripDays.Select(d => new SavedTripDayDto
            {
                DayNumber = d.DayNumber,
                Theme = d.Theme,
                Activities = allActivities
                    .Where(a => a.SavedTripDayId == d.Id)
                    .Select(a => new SavedTripActivityDto
                    {
                        ActivityType = a.ActivityType,
                        PlaceName = a.PlaceName,
                        SuggestedTime = a.SuggestedTime,
                        Description = a.Description,
                        Address = a.Address,
                        FeaturedImage = a.FeaturedImage
                    }).ToList()
            }).ToList(),
            Hotels = allHotels
                .Where(h => h.SavedTripId == tripId)
                .Select(h => new SavedTripHotelDto
                {
                    Name = h.Name,
                    Description = h.Description,
                    Rating = h.Rating,
                    Address = h.Address,
                    Link = h.Link,
                    FeaturedImage = h.FeaturedImage
                }).ToList()
        };

        return new ServiceResponse<SavedTripDetailDto>(dto, "Trip retrieved successfully.");
    }

    public async Task<ServiceResponse<bool>> ToggleFavoriteAsync(int tripId, string userId)
    {
        var trip = await _tripRepo.GetByIdAsync(tripId);
        if (trip == null || trip.UserId != userId)
            return new ServiceResponse<bool>("Trip not found.");

        trip.IsFavorite = !trip.IsFavorite;
        await _tripRepo.UpdateAsync(trip);

        var message = trip.IsFavorite ? "Trip added to favorites." : "Trip removed from favorites.";
        return new ServiceResponse<bool>(true, message);
    }

    public async Task<ServiceResponse<bool>> DeleteTripAsync(int tripId, string userId)
    {
        var trip = await _tripRepo.GetByIdAsync(tripId);
        if (trip == null || trip.UserId != userId)
            return new ServiceResponse<bool>("Trip not found.");

        await _tripRepo.DeleteAsync(trip);
        return new ServiceResponse<bool>(true, "Trip deleted successfully.");
    }
}
