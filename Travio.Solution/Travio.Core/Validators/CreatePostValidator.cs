using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.DTOs.CommunityDTO;

namespace Travio.Core.Validators
{
    public class CreatePostValidator : AbstractValidator<CreatePostDTO>
    {
        public CreatePostValidator()
        {
            RuleFor(x => x.Title)
                 .NotEmpty().WithMessage("Post title is required.");
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Post content is required.");
        }
    }
}
