using FluentValidation;
using Travio.Core.DTOs.SurveyDTO;
using Travio.Core.Helpers;
using System.Linq;
using System.Collections.Generic;

namespace Travio.Core.Validators
{
    public class UserPreferencesValidator : AbstractValidator<List<UserPreferenceDto>>
    {
        public UserPreferencesValidator()
        {
            RuleFor(x => x)
                .NotEmpty().WithMessage("Preferences payload cannot be empty.");

            RuleForEach(x => x).SetValidator(new UserPreferenceDtoValidator());
        }
    }

    public class UserPreferenceDtoValidator : AbstractValidator<UserPreferenceDto>
    {
        public UserPreferenceDtoValidator()
        {
            RuleFor(x => x).Must(BeAValidPreference)
                .WithMessage(x => $"Invalid CategoryId ({x.CategoryId}) or OptionId ({x.OptionId}).");
        }

        private bool BeAValidPreference(UserPreferenceDto preference)
        {
            var category = SurveyConfiguration.OnboardingSurvey.FirstOrDefault(c => c.CategoryId == preference.CategoryId);
            if (category == null) return false;

            return category.Options.Any(o => o.Id == preference.OptionId);
        }
    }
}