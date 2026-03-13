using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Storage.Json;
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
        private readonly IGenericRepository<PostImage> _postImageRepo;

        public CommunityService(IGenericRepository<Post> postRepo, IGenericRepository<PostImage> postImageRepo)
        {
            _postRepo = postRepo;
            _postImageRepo = postImageRepo;
        }
        public async Task<ServiceResponse<PostResponseDTO>> CreatePostAsync(string UerId, CreatePostDTO model)
        {
            var newPost = model.Adapt<Post>();

            newPost.UserId = UerId;
            newPost.CreatedOn = DateTime.Now;

            var Result = await _postRepo.AddAsync(newPost);
            if (Result is null)
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

        public async Task<ServiceResponse<IEnumerable<PostResponseDTO>>> GetCommunityFeedAsync(string CurrentUserId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var spec = new CommunityFeedSpec(CurrentUserId, pageNumber, pageSize);
                var feed = await _postRepo.ListAsync(spec);

                return new ServiceResponse<IEnumerable<PostResponseDTO>>
                {
                    Success = true,
                    Message = "Feed retrieved successfully.",
                    Data = feed
                };
            }
            catch (Exception ex)
            {

                return new ServiceResponse<IEnumerable<PostResponseDTO>>
                {
                    Success = false,
                    Message = "An error occurred while fetching the community feed."
                };
            }
        }

        public async Task<ServiceResponse<bool>> DeletePostAsync(int postId, string userId)
        {
            try
            {
                var spec = new GetPostByIdSpec(postId);
                var post = await _postRepo.FirstOrDefaultAsync(spec);
                if (post is null) return new ServiceResponse<bool>()
                {
                    Success = false,
                    Message = "Post Not Found"
                };

                if (post.UserId != userId) return new ServiceResponse<bool>()
                {
                    Success = false,
                    Message = "You are not Authorized to delete this post "
                };

                await _postRepo.DeleteAsync(post);

                return new ServiceResponse<bool>()
                {
                    Success = true,
                    Message = "Post deleted successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<bool>()
                {
                    Success = false,
                    Message = "An error occurred while attempting to delete the post."
                };
            }


        }

        public async Task<ServiceResponse<string>> AddPostImageAsync(int postId, string UserId, IFormFile image)
        {
            try
            {
                var postSpec = new GetPostByIdSpec(postId);
                var post = await _postRepo.FirstOrDefaultAsync(postSpec);
                if (post is null) return new ServiceResponse<string>()
                {
                    Success = false,
                    Message = "Post Not Found ."
                };
                if (post.UserId != UserId) return new ServiceResponse<string>() { Success = false, Message = "UnAuthrized" };
                if (image is null || image.Length == 0) return new ServiceResponse<string>() { Success = false, Message = "No image file provided." };
                var fileExtension = Path.GetExtension(image.FileName);
                var uniqueFileName = $"{Guid.CreateVersion7()}{fileExtension}";

                // Define where the file will be saved on the server
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "posts");
                Directory.CreateDirectory(uploadsFolder); // Ensures the folder exists

                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Actually copy the file to the hard drive
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // 4. Save the URL to the Database
                // In a real app, you'd use your actual domain (e.g., https://api.travio.com/images/posts/...)
                // For now, we save the relative path so the frontend can append the base URL
                var imageUrl = $"/images/posts/{uniqueFileName}";

                var postImage = new PostImage
                {
                    PostId = postId,
                    ImageUrl = imageUrl
                };

                await _postImageRepo.AddAsync(postImage); // Ardalis handles the SaveChanges!

                return new ServiceResponse<string>
                {
                    Success = true,
                    Message = "Image uploaded successfully.",
                    Data = imageUrl // Return the URL so the Flutter app can display it immediately
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while uploading the image."
                };
            }

        }
    }
}
