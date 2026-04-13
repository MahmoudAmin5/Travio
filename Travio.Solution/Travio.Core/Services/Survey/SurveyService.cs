
using Travio.Core.Contracts.Services.Survey;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Infrastructure.Contract;

namespace Travio.Core.Services.Survey;

public class SurveyService : ISurveyService
{
    private readonly IGenericRepository<UserPreference> _repo;

    public SurveyService(IGenericRepository<UserPreference> repo)
    {
        _repo = repo;
    }

    public async Task AddUserPreferencesAsync(string userId, List<(int CategoryId, int OptionId)> preferences)
    {
        var userPreferences = preferences.Select(p => new UserPreference
        {
            UserID = userId,
            CategoryID = p.CategoryId,
            OptionId = p.OptionId
        }).ToList();

        foreach (var pref in userPreferences)
        {
            await _repo.AddAsync(pref);
        }
    }

}
