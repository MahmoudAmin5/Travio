using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.DTOs.CommunityDTO;

namespace Travio.Core.Validators
{
        public class AddPostImageDtoValidator : AbstractValidator<UploadPostImageDTO>
        {
            public AddPostImageDtoValidator()
            {
            RuleFor(dto => dto.Images)
                .NotNull().WithMessage("An image file is required.");
            RuleForEach(dto => dto.Images)
            .Must(BeAValidSize).WithMessage("Each image must not exceed 5MB.")
            .Must(BeAValidExtension).WithMessage("Only .jpg, .jpeg, and .png files are allowed.");

        }

            private bool BeAValidSize(IFormFile file)
            {
                if (file == null) return false;

                var maxSizeInBytes = 5 * 1024 * 1024;
                return file.Length <= maxSizeInBytes;
            }

            private bool BeAValidExtension(IFormFile file)
            {
                if (file == null) return false;

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = System.IO.Path.GetExtension(file.FileName).ToLower();

                return allowedExtensions.Contains(fileExtension);
            }
        }
    }