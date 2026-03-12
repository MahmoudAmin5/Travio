using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services.Community;
using Travio.Core.Domain.Entities.Community;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Domain.Specifications.Community;
using Travio.Core.DTOs.CommunityDTO;
using Travio.Core.DTOs.GenericResponse;

namespace Travio.Core.Services.Community
{
    public class CommunityService : ICommunityService
    {
        private readonly IGenericRepository<Post> _postRepo;

        public CommunityService(IGenericRepository<Post> postRepo)
        {
            _postRepo = postRepo;
        }
        public async Task<ServiceResponse<PostResponseDTO>> CreatePostAsync(string UerId, CreatePostDTO model)
        {
            var newPost = model.Adapt<Post>();

            newPost.UserId = UerId;
            newPost.CreatedOn = DateTime.Now;

            var Result = await _postRepo.AddAsync(newPost);
            if(Result is null)
            {
                return new ServiceResponse<PostResponseDTO>
                {
                    Success = false,
                    Message = "Failed to create the Post. Please try again."
                };
            }
            
            var spec = new PostWithDetailsSpec(newPost.Id);
            var completedPost = await _postRepo.FirstOrDefaultAsync(spec);

             var responseModel = completedPost.Adapt<PostResponseDTO>();
            return new ServiceResponse<PostResponseDTO>
            {
                Success = true,
                Message = "Post created successfully.",
                Data = responseModel

            };
            
        }

        public async Task<ServiceResponse<IEnumerable<PostResponseDTO>>> GetCommunityFeedAsync(string CurrentUserId)
        {
            try
            {
                var spec = new CommunityFeedSpec();
                var posts = await _postRepo.ListAsync(spec);

                var feed = new List<PostResponseDTO>();

                foreach (var post in posts)
                {
                    var dto = post.Adapt<PostResponseDTO>();

                    if (post.Likes is not null)
                    {
                        dto.IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == CurrentUserId);
                    }
                    feed.Add(dto);
                }
                return new ServiceResponse<IEnumerable<PostResponseDTO>>
                {
                    Success = true,
                    Message = "Feed retrieved successfully.",
                    Data = feed
                };
            }
            catch (Exception ex)
            {
                // Log the exception in a real scenario
                return new ServiceResponse<IEnumerable<PostResponseDTO>>
                {
                    Success = false,
                    Message = "An error occurred while fetching the community feed."
                };
            }
        }
    }
}
