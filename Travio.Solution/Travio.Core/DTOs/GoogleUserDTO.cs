using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs
{
    public class GoogleUserDTO
    {
        public string ProviderKey { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string? PictureURL { get; set; }
    }
}
