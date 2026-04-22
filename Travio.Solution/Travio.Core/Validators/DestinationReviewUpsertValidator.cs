using FluentValidation;
using Travio.Core.DTOs.DestinationDTO;

namespace Travio.Core.Validators;

public class DestinationReviewUpsertValidator : AbstractValidator<DestinationReviewUpsertDto>
{
    public DestinationReviewUpsertValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .WithMessage("Comment cannot exceed 500 characters.");
    }
}
