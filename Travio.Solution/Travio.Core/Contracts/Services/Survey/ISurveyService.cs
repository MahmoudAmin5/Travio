namespace Travio.Core.Contracts.Services.Survey;

public interface ISurveyService
{
    Task AddUserPreferencesAsync(string userId, List<(int CategoryId, int OptionId)> preferences);
}
