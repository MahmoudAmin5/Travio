using Microsoft.EntityFrameworkCore;
using Travio.Core.Contracts.Services.Destination;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.Helpers;

namespace Travio.Core.Services.Destinations;

public class UserFavoriteService : IUserFavoriteService
{
    private readonly IGenericRepository<UserFavorite> _favRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UserFavoriteService(IGenericRepository<UserFavorite> favRepo, IUnitOfWork unitOfWork)
    {
        _favRepo = favRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResponse<bool>> AddFavoriteAsync(string userId, int destinationId)
    {
        return await _unitOfWork.ExecuteAsync(async () =>
        {
            var exists = await _favRepo.GetQueryable()
                .AsNoTracking()
                .AnyAsync(uf => uf.UserId == userId && uf.DestinationId == destinationId);

            if (exists)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "This destination is already in your favorites."
                };
            }

            var addFav = new UserFavorite
            {
                UserId = userId,
                DestinationId = destinationId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _favRepo.AddAsync(addFav);

            return new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Favorite added successfully."
            };
        });
    }

    public async Task<ServiceResponse<bool>> DeleteFavoriteAsync(string userId, int destinationId)
    {
        // Rule 3: Bulk delete directly via ExecuteDeleteAsync
        var rowsDeleted = await _favRepo.GetQueryable()
            .Where(uf => uf.UserId == userId && uf.DestinationId == destinationId)
            .ExecuteDeleteAsync();

        if (rowsDeleted == 0)
        {
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "This destination is not found in your favorites."
            };
        }

        return new ServiceResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "Favorite deleted successfully."
        };
    }

    public async Task<ServiceResponse<Pagination<GetFavDestinationResponse>>> GetUserFavoritesAsync(int pageIndex, int pageSize, string userId)
    {
        int skip = (pageIndex - 1) * pageSize;
        var baseQuery = _favRepo.GetQueryable()
            .AsNoTracking()
            .Where(uf => uf.UserId == userId);

        var totalCount = await baseQuery.CountAsync();

        // Rule 6: Direct projection to DTO via .Select(...)
        var result = await baseQuery
            .OrderByDescending(uf => uf.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(uf => new GetFavDestinationResponse
            {
                DestinationID = uf.DestinationId,
                Name = uf.Destination.Name,
                Description = uf.Destination.Description,
                Rating = uf.Destination.Rating,
                CityName = uf.Destination.City.Name,
                ImageUrls = uf.Destination.Images.Select(i => i.ImageURL).ToList()
            })
            .ToListAsync();

        var data = new Pagination<GetFavDestinationResponse>(pageIndex, pageSize, totalCount, result);
        return new ServiceResponse<Pagination<GetFavDestinationResponse>>
        {
            Success = true,
            Data = data,
            Message = "Favorites retrieved successfully."
        };
    }
}
