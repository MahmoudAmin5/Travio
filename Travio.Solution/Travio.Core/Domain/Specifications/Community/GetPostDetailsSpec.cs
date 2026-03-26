using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Community;
using Travio.Core.DTOs.CommunityDTO;

namespace Travio.Core.Domain.Specifications.Community
{
    public class GetPostDetailsSpec : SingleResultSpecification<Post, PostDetailsResponseDTO>
    {
        public GetPostDetailsSpec(int postId , string userId)
        {
            Query.Where(p => p.Id == postId)
                .AsNoTracking();
            Query.Select(post => new PostDetailsResponseDTO
            {
                Id = post.Id,
                Location = post.Location,
                Description = post.Content,
                CreationDate = post.CreatedOn,

                AuthorId = post.UserId,
                AuthorName = post.User.FirstName + " " + post.User.LastName,
                AuthorProfilePictureUrl = post.User.ProfilePictureURL,

                LikesCount = post.Likes.Count(),
                IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == userId),

                ImageUrls = post.Images.Select(i => i.ImageUrl).ToList(),

               
                Comments = post.Comments
                      .OrderByDescending(c => c.CreatedOn) 
                      .Select(c => new CommentResponseDTO
                      {
                          Id = c.Id,
                          Content = c.Content,
                          CreationDate = c.CreatedOn,
                          AuthorId = c.UserId,
                          AuthorName = c.User.FirstName + " " + c.User.LastName,
                          AuthorProfilePictureUrl = c.User.ProfilePictureURL
                      }).ToList()
            });
        }
    }
}
