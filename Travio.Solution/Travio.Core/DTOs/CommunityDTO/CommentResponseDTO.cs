using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.CommunityDTO
{
    public class CommentResponseDTO
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreationDate { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorProfilePictureUrl { get; set; }
    }
}
