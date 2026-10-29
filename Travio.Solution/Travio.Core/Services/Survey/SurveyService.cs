using Travio.Core.Contracts.Services.Survey;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Infrastructure.Contract;

namespace Travio.Core.Services.Survey;

public class SurveyService : ISurveyService
{
    private readonly IGenericRepository<UserPreference> _repo;
    private readonly IUnitOfWork _unitOfWork;

    public SurveyService(IGenericRepository<UserPreference> repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task AddUserPreferencesAsync(string userId, List<(int CategoryId, int OptionId)> preferences)
    {
        var userPreferences = preferences.Select(p => new UserPreference
        {
            UserID = userId,
            CategoryID = p.CategoryId,
            OptionId = p.OptionId
        }).ToList();

        await _unitOfWork.ExecuteAsync(async () =>
        {
            await _repo.AddRangeAsync(userPreferences);
        });
    }
}
