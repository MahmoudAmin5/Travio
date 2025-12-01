using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Domain.Entities.Account_Mangement
{
    [Owned]
    public class RefreshToken
    {
        public string TokenHash { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public ApplicationUser? User { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresOn { get; set; }
        public DateTime? RevokedOn { get; set; }
        public string? RevokedByIp { get; set; }
        public string? RevokeReason { get; set; }

        public bool IsActive => RevokedOn == null && ExpiresOn > DateTime.UtcNow;
    }
}
