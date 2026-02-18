using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs
{
        public class UpdateProfileDto
        {
            [Required(ErrorMessage = "First Name is required")]
            [MaxLength(50, ErrorMessage = "First Name cannot exceed 50 characters")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Last Name is required")]
            [MaxLength(50, ErrorMessage = "Last Name cannot exceed 50 characters")]
            public string LastName { get; set; }
        }
    }
}

