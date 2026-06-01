using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travio.API.Errors;
using Travio.Core.Contracts.Services.Survey;
using Travio.Core.DTOs.SurveyDTO;
using Travio.Core.Helpers;
using Travio.Core.Validators;

namespace Travio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SurveyController : ControllerBase
{
    private readonly ISurveyService _surveyService;

    public SurveyController(ISurveyService surveyService)
    {
        _surveyService = surveyService;
    }

    [HttpGet("onboarding")]
    public ActionResult<List<SurveyQuestion>> GetSurvey()
    {
        return Ok(SurveyConfiguration.OnboardingSurvey);
    }

    [HttpPost("user-preferences")]
    [Authorize]
    public async Task<ActionResult> SubmitSurvey([FromBody] List<UserPreferenceDto> preferences)
    {
        var validator = new UserPreferencesValidator();
        var validationResult = validator.Validate(preferences);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiResponse(401, "InvalidToken"));
        }

        var mappedPreferences = preferences.Select(p => (p.CategoryId, p.OptionId)).ToList();

        await _surveyService.AddUserPreferencesAsync(userId, mappedPreferences);
        return Ok(new { Message = "Survey submitted successfully" });
    }
}