using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Enums;

namespace Travio.Core.Domain.Entities.Account_Mangement
{
    public class UserCode
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public AuthCodeType CodeType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public bool IsUsed { get; set; } = false;
        public bool IsRevoked { get; set; } = false;

        public ApplicationUser User { get; set; } 
    }
}
