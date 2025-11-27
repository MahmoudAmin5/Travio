using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Email or Username Is Required")]
        public string Email_or_username { get; set; }
        [Required(ErrorMessage = "Password Is Required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
