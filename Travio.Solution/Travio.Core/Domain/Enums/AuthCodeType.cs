using System.Runtime.Serialization;

namespace Travio.Core.Domain.Enums
{
    public enum AuthCodeType
    {
        [EnumMember(Value = "PasswordReset")]
        PasswordReset = 1,
        [EnumMember(Value = "EmailVerification")]
        EmailVerification = 2
    }
}
