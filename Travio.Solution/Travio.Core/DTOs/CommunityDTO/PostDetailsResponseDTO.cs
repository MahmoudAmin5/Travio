using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.CommunityDTO
{
    public class PostDetailsResponseDTO
    {
        public int Id { get; set; }
        public string Location { get; set; }
        public string Content { get; set; }
        public DateTime CreationDate { get; set; }

        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorProfilePictureUrl { get; set; }

        public int LikesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public List<string> ImageUrls { get; set; }

       
        public List<CommentResponseDTO> Comments { get; set; }
    }
}
