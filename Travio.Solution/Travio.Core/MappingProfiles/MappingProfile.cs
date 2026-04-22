using Mapster;
using Travio.Core.Domain.Entities.Community;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.DTOs.CommunityDTO;
using Travio.Core.DTOs.DestinationDTO;

namespace Travio.Application.Mappings;

public class MappingProfile : IRegister
{


    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Destination, DestinationDto>()

           .Map(dest => dest.CityName, src => src.City.Name)


           .Map(dest => dest.ImageUrls, src =>
               src.Images.Select(img => img.ImageURL).ToList())


           .Map(dest => dest.Interests, src =>
               src.DestinationInterests.Select(di => di.Interest).ToList());

           
        config.NewConfig<Interest, InterestDto>();

        config.NewConfig<DestinationReview, DestinationReviewDto>()
            .Map(dest => dest.ReviewDateUtc, src => src.CreatedAtUtc)
            .Map(dest => dest.ReviewerName, src => src.User.FirstName + " " + src.User.LastName)
            .Map(dest => dest.ReviewerImageUrl, src => src.User.ProfilePictureURL)
            .Map(dest => dest.HelpfulVotes, src => src.HelpfulVotes);

        config.NewConfig<Post, PostResponseDTO>()
            .Map(dest => dest.AutherId, src => src.UserId)
            .Map(dest => dest.AutherName, src => src.User.FirstName + ' ' + src.User.LastName)
            .Map(dest => dest.AuthorProfilePictureUrl, src => src.User.ProfilePictureURL)

            .Map(dest => dest.LikesCount, src => src.Likes != null ? src.Likes.Count : 0)
            .Map(dest => dest.CommentsCount, src => src.Comments != null ? src.Comments.Count : 0)
            .Map(dest => dest.PostImagesUrls, src => src.Images != null ? src.Images.Select(image => image.ImageUrl) : new List<string>())

            .Map(dest => dest.IsLikedByCurrentUser, src=>false);
    }
}