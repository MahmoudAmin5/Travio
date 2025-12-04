using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Enums;

namespace Travio.Core.DTOs
{
    public record SendOtpRequestDto(string Target); // Target = email
    public record SendOtpResponseDto(VerifyOtpStatus Status, string Message, DateTime? ExpiresOn);
    public record VerifyOtpRequestDto(string Target, string Otp); // Target = email 
    public record VerifyOtpResponseDto(VerifyOtpStatus status, string Message);
}
