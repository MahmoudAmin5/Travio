using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.CommunityDTO
{
    public class CreatePostDTO
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
    }
}
