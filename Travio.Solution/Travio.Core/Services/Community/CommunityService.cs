using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Travio.Core.Contracts.Services.Community;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Entities.Community;
using Travio.Core.Domain.Infrastructure.Contract;
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
        private readonly IUnitOfWork _unitOfWork;

        public CommunityService(
            IGenericRepository<Post> postRepo,
            IGenericRepository<PostImage> postImageRepo,
            IGenericRepository<PostLike> postLikeRepo,
            IGenericRepository<Comment> postCommentRepo,
            IGenericRepository<ApplicationUser> userRepo,
            IUnitOfWork unitOfWork)
        {
            _postRepo = postRepo;
            _postImageRepo = postImageRepo;
            _postLikeRepo = postLikeRepo;
            _postCommentRepo = postCommentRepo;
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResponse<PostResponseDTO>> CreatePostAsync(string UerId, CreatePostDTO model)
        {
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var newPost = model.Adapt<Post>();
                newPost.UserId = UerId;
                newPost.CreatedOn = DateTime.Now;

                await _postRepo.AddAsync(newPost);
                await _unitOfWork.SaveChangesAsync();

                // Rule 6: Project directly to DTO via .Select(...)
                var responseModel = await _postRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(post => post.Id == newPost.Id)
                    .Select(post => new PostResponseDTO
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
                        IsLikedByCurrentUser = false
                    })
                    .FirstOrDefaultAsync();

                if (responseModel is null)
                {
                    return new ServiceResponse<PostResponseDTO>
                    {
                        Success = false,
                        Message = "Failed to create the Post. Please try again."
                    };
                }

                return new ServiceResponse<PostResponseDTO>
                {
                    Success = true,
                    Message = "Post created successfully.",
                    Data = responseModel
                };
            });
        }

        public async Task<ServiceResponse<IEnumerable<PostResponseDTO>>> GetCommunityFeedAsync(string CurrentUserId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                // Rule 6: Direct DTO projection via .Select(...)
                var feed = await _postRepo.GetQueryable()
                    .AsNoTracking()
                    .OrderByDescending(post => post.CreatedOn)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(post => new PostResponseDTO
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
                        IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == CurrentUserId)
                    })
                    .ToListAsync();

                return new ServiceResponse<IEnumerable<PostResponseDTO>>
                {
                    Success = true,
                    Message = "Feed retrieved successfully.",
                    Data = feed
                };
            }
            catch (Exception)
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
                var postOwner = await _postRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(p => p.Id == postId)
                    .Select(p => new { p.UserId })
                    .FirstOrDefaultAsync();

                if (postOwner is null)
                {
                    return new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Post Not Found"
                    };
                }

                if (postOwner.UserId != userId)
                {
                    return new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "You are not Authorized to delete this post "
                    };
                }

                // Rule 3: Bulk delete via ExecuteDeleteAsync
                await _postRepo.GetQueryable()
                    .Where(p => p.Id == postId && p.UserId == userId)
                    .ExecuteDeleteAsync();

                return new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Post deleted successfully.",
                    Data = true
                };
            }
            catch (Exception)
            {
                return new ServiceResponse<bool>
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
                var postOwner = await _postRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(p => p.Id == postId)
                    .Select(p => new { p.UserId })
                    .FirstOrDefaultAsync();

                if (postOwner == null) return new ServiceResponse<List<string>> { Success = false, Message = "Post not found." };
                if (postOwner.UserId != UserId) return new ServiceResponse<List<string>> { Success = false, Message = "Unauthorized." };

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

                await _unitOfWork.ExecuteAsync(async () =>
                {
                    await _postImageRepo.AddRangeAsync(postImagesToSave);
                });

                return new ServiceResponse<List<string>>
                {
                    Success = true,
                    Message = "Images uploaded successfully.",
                    Data = uploadedUrls
                };
            }
            catch (Exception)
            {
                return new ServiceResponse<List<string>> { Success = false, Message = "An error occurred during upload." };
            }
        }

        public async Task<ServiceResponse<bool>> ToggleLikeAsync(int postId, string userId)
        {
            try
            {
                var postExists = await _postRepo.GetQueryable()
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == postId);

                if (!postExists) return new ServiceResponse<bool> { Success = false, Message = "Post Not Found ." };

                // Rule 3: Attempt ExecuteDeleteAsync first; if 0 rows deleted, insert new like inside UoW
                var deletedCount = await _postLikeRepo.GetQueryable()
                    .Where(x => x.PostId == postId && x.UserId == userId)
                    .ExecuteDeleteAsync();

                if (deletedCount > 0)
                {
                    return new ServiceResponse<bool>
                    {
                        Success = true,
                        Message = "Post unliked successfully.",
                        Data = false
                    };
                }

                await _unitOfWork.ExecuteAsync(async () =>
                {
                    var newLike = new PostLike
                    {
                        PostId = postId,
                        UserId = userId
                    };
                    await _postLikeRepo.AddAsync(newLike);
                });

                return new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Post liked successfully.",
                    Data = true
                };
            }
            catch (Exception)
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
                // Rule 6: Direct projection to DTO via .Select(...)
                var postDetails = await _postRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(p => p.Id == postId)
                    .Select(post => new PostDetailsResponseDTO
                    {
                        Id = post.Id,
                        Location = post.Location,
                        Content = post.Content,
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
                    })
                    .FirstOrDefaultAsync();

                if (postDetails is null) return new ServiceResponse<PostDetailsResponseDTO> { Success = false, Message = "Post not found." };

                return new ServiceResponse<PostDetailsResponseDTO>
                {
                    Success = true,
                    Message = "Post retrieved successfully.",
                    Data = postDetails
                };
            }
            catch (Exception)
            {
                return new ServiceResponse<PostDetailsResponseDTO>
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
                var postExists = await _postRepo.GetQueryable()
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == postId);

                if (!postExists) return new ServiceResponse<CommentResponseDTO> { Success = false, Message = "Post Not Found" };

                var user = await _userRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Id, u.FirstName, u.LastName, u.ProfilePictureURL })
                    .FirstOrDefaultAsync();

                if (user is null) return new ServiceResponse<CommentResponseDTO> { Success = false, Message = "User Not Found" };

                var comment = new Comment
                {
                    PostId = postId,
                    UserId = userId,
                    Content = dto.Content,
                    CreatedOn = DateTime.UtcNow
                };

                await _unitOfWork.ExecuteAsync(async () =>
                {
                    await _postCommentRepo.AddAsync(comment);
                });

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
            catch (Exception)
            {
                return new ServiceResponse<CommentResponseDTO> { Success = false, Message = "An error occurred while adding the comment." };
            }
        }

        public async Task<ServiceResponse<bool>> DeleteCommentAsync(int commentId, string userId)
        {
            try
            {
                var commentOwner = await _postCommentRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(c => c.Id == commentId)
                    .Select(c => new { c.UserId })
                    .FirstOrDefaultAsync();

                if (commentOwner == null)
                {
                    return new ServiceResponse<bool> { Success = false, Message = "Comment not found." };
                }

                if (commentOwner.UserId != userId)
                {
                    return new ServiceResponse<bool> { Success = false, Message = "You are not authorized to delete this comment." };
                }

                // Rule 3: Bulk delete via ExecuteDeleteAsync
                await _postCommentRepo.GetQueryable()
                    .Where(c => c.Id == commentId && c.UserId == userId)
                    .ExecuteDeleteAsync();

                return new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Comment deleted successfully.",
                    Data = true
                };
            }
            catch (Exception)
            {
                return new ServiceResponse<bool> { Success = false, Message = "An error occurred while deleting the comment." };
            }
        }
    }
}
