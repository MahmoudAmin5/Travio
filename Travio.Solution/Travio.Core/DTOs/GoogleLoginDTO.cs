using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs
{
    public class GoogleLoginDTO
    {
        [Required]
        public string IdToken { get; set; }
    }
}
