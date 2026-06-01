using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.DTOs.CommunityDTO;
using Travio.Core.DTOs.GenericResponse;

namespace Travio.Core.Contracts.Services.Community
{
    public interface ICommunityService
    {
        Task<ServiceResponse<PostResponseDTO>> CreatePostAsync(string UerId, CreatePostDTO model);
        Task<ServiceResponse<IEnumerable<PostResponseDTO>>> GetCommunityFeedAsync(string CurrentUserId, int pageNumber = 1, int pageSize = 10);
        Task<ServiceResponse<bool>> DeletePostAsync(int postId, string userId);
        Task<ServiceResponse<List<string>>> AddPostImageAsync(int postId, string UserId, List<IFormFile> image);
        Task<ServiceResponse<bool>> ToggleLikeAsync(int postId, string userId);
        Task<ServiceResponse<PostDetailsResponseDTO>> GetPostByIdAsync(int postId, string userId);
        Task<ServiceResponse<CommentResponseDTO>> AddCommentAsync(int postId, string userId, CreateCommentDTO dto);
        Task<ServiceResponse<bool>> DeleteCommentAsync(int commentId, string userId);

    }
}
