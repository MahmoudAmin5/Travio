using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Enums;

namespace Travio.Core.DTOs
{
    public record SendOtpRequestDto(string Email); 
    public record SendOtpResponseDto(VerifyOtpStatus Status, string Message, DateTime? ExpiresOn);
    public record VerifyOtpRequestDto(string Email, string Otp);
    public record VerifyOtpResponseDto(VerifyOtpStatus status, string Message,string? ResetToken = null);
}
