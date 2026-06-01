using Mapster;
using Microsoft.AspNetCore.Http;
using Travio.Core.Contracts.Services.Community;
using Travio.Core.Domain.Entities.Account_Mangement;
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
        private readonly IGenericRepository<PostLike> _postLikeRepo;
        private readonly IGenericRepository<Comment> _postCommentRepo;
        private readonly IGenericRepository<ApplicationUser> _userRepo;

        public CommunityService(IGenericRepository<Post> postRepo,
            IGenericRepository<PostImage> postImageRepo,
            IGenericRepository<PostLike> postLikeRepo,
            IGenericRepository<Comment> postCommentRepo,
            IGenericRepository<ApplicationUser> userRepo)
        {
            _postRepo = postRepo;
            _postImageRepo = postImageRepo;
            _postLikeRepo = postLikeRepo;
            _postCommentRepo = postCommentRepo;
            _userRepo = userRepo;
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

        public async Task<ServiceResponse<List<string>>> AddPostImageAsync(int postId, string UserId, List<IFormFile> images)
        {
            try
            {
                var postSpec = new GetPostByIdSpec(postId);
                var post = await _postRepo.FirstOrDefaultAsync(postSpec);

                if (post == null) return new ServiceResponse<List<string>> { Success = false, Message = "Post not found." };
                if (post.UserId != UserId) return new ServiceResponse<List<string>> { Success = false, Message = "Unauthorized." };

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "posts");
                Directory.CreateDirectory(uploadsFolder);

                var uploadedUrls = new List<string>();
                var postImagesToSave = new List<PostImage>();


                foreach (var image in images)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);


                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    var imageUrl = $"/images/posts/{uniqueFileName}";
                    uploadedUrls.Add(imageUrl);


                    postImagesToSave.Add(new PostImage
                    {
                        PostId = postId,
                        ImageUrl = imageUrl
                    });
                }


                await _postImageRepo.AddRangeAsync(postImagesToSave);

                return new ServiceResponse<List<string>>
                {
                    Success = true,
                    Message = "Images uploaded successfully.",
                    Data = uploadedUrls
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<string>> { Success = false, Message = "An error occurred during upload." };
            }
        }

        public async Task<ServiceResponse<bool>> ToggleLikeAsync(int postId, string userId)
        {
            try
            {

                var postFound = await _postRepo.FirstOrDefaultAsync(new GetPostByIdSpec(postId));
                if (postFound is null) return new ServiceResponse<bool> { Success = false, Message = "Post Not Found ." };

                var spec = new PostLikeSpec(postId, userId);
                var isLike = await _postLikeRepo.FirstOrDefaultAsync(spec);
                if (isLike is not null)
                {
                    await _postLikeRepo.DeleteAsync(isLike);
                    return new ServiceResponse<bool>
                    {
                        Success = true,
                        Message = "Post unliked successfully.",
                        Data = false
                    };
                }
                else
                {
                    var newLike = new PostLike
                    {
                        PostId = postId,
                        UserId = userId,

                    };
                    await _postLikeRepo.AddAsync(newLike);
                    return new ServiceResponse<bool>
                    {
                        Success = true,
                        Message = "Post liked successfully.",
                        Data = true
                    };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while toggling the like status."
                };
            }
        }

        public async Task<ServiceResponse<PostDetailsResponseDTO>> GetPostByIdAsync(int postId, string userId)
        {
            try
            {
                var spec = new GetPostDetailsSpec(postId, userId);
                var postDetails = await _postRepo.FirstOrDefaultAsync(spec);
                if (postDetails is null) return new ServiceResponse<PostDetailsResponseDTO>() { Success = false, Message = "Post not found." };
                return new ServiceResponse<PostDetailsResponseDTO>()
                {
                    Success = true,
                    Message = "Post retrieved successfully.",
                    Data = postDetails
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<PostDetailsResponseDTO>()
                {
                    Success = false,
                    Message = "An error occurred while fetching the post details."
                };
            }

        }

        public async Task<ServiceResponse<CommentResponseDTO>> AddCommentAsync(int postId, string userId, CreateCommentDTO dto)
        {
            try
            {
                var postExists = await _postRepo.FirstOrDefaultAsync(new GetPostByIdSpec(postId));
                if (postExists is null) return new ServiceResponse<CommentResponseDTO>() { Success = false, Message = "Post Not Found" };
                var user = await _userRepo.GetByIdAsync(userId);
                var comment = new Comment
                {
                    PostId = postId,
                    UserId = userId,
                    Content = dto.Content,
                    CreatedOn = DateTime.UtcNow
                };
                await _postCommentRepo.AddAsync(comment);
                var responseDto = new CommentResponseDTO
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    CreationDate = comment.CreatedOn,
                    AuthorId = user.Id,
                    AuthorName = user.FirstName + " " + user.LastName,
                    AuthorProfilePictureUrl = user.ProfilePictureURL
                };
                return new ServiceResponse<CommentResponseDTO>
                {
                    Success = true,
                    Message = "Comment added successfully.",
                    Data = responseDto
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<CommentResponseDTO> { Success = false, Message = "An error occurred while adding the comment." };
            }
        }
        public async Task<ServiceResponse<bool>> DeleteCommentAsync(int commentId, string userId)
        {
            try
            {
                var spec = new GetCommentByIdSpec(commentId);
                var comment = await _postCommentRepo.FirstOrDefaultAsync(spec);

                // Safety Check 1: Does it exist?
                if (comment == null)
                {
                    return new ServiceResponse<bool> { Success = false, Message = "Comment not found." };
                }

                // Safety Check 2: Does this user own the comment?
                if (comment.UserId != userId)
                {
                    return new ServiceResponse<bool> { Success = false, Message = "You are not authorized to delete this comment." };
                }

                await _postCommentRepo.DeleteAsync(comment);

                return new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Comment deleted successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<bool> { Success = false, Message = "An error occurred while deleting the comment." };
            }
        }
    }
}


