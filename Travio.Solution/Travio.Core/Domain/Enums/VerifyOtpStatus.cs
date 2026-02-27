using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Domain.Enums
{
    public enum VerifyOtpStatus
    {
        Success,
        UserNotFound,
        CodeExpired,
        CodeInvalid,
        Invalid
    }
}
