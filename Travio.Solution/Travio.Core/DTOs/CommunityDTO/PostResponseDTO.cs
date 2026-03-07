using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.CommunityDTO
{
    public class PostResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreationDate { get; set; }
        //Auther
        public string AutherId { get; set; }
        public string AutherName { get; set; }
        public string AuthorProfilePictureUrl { get; set; }
        //Engagments
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }

        public List<string> PostImagesUrls { get; set; }

        public bool IsLikedByCurrentUser { get; set; }


    }
}
