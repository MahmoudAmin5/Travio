using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Enums;

namespace Travio.Core.DTOs
{
    public class VerifyResetPasswordOtp
    {
        public record VerifyResetPasswordOtpResponse(VerifyOtpStatus Status, string Message, string ResetToken);
    }
}
