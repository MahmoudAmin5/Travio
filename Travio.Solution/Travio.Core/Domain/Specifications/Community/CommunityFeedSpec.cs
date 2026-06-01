using Ardalis.Specification;
using Travio.Core.DTOs.CommunityDTO;
using Travio.Core.Domain.Entities.Community;

public class CommunityFeedSpec
    : Specification<Post, PostResponseDTO>
{
    public CommunityFeedSpec(
        string currentUserId,
        int pageNumber,
        int pageSize)
    {
        Query
            .OrderByDescending(post => post.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
        .AsNoTracking();

        Query.Select(post => new PostResponseDTO
        {
            Id = post.Id,
            Location = post.Location,
            Content = post.Content,
            CreationDate = post.CreatedOn,

            AutherId = post.UserId,
            AutherName = post.User.FirstName + " " + post.User.LastName,
            AuthorProfilePictureUrl = post.User.ProfilePictureURL,

            LikesCount = post.Likes.Count,
            CommentsCount = post.Comments.Count,

             PostImagesUrls = post.Images.Select(i => i.ImageUrl).ToList(),

            IsLikedByCurrentUser = post.Likes
                .Any(l => l.UserId == currentUserId)
        });
    }
}