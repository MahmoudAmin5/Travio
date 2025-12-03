using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Domain.Entities.Enums
{
    public enum AuthCodeType
    {
        [EnumMember(Value = "PasswordReset")]
        PasswordReset = 1,
        [EnumMember(Value = "EmailVerification")]
        EmailVerification = 2
    }
}
