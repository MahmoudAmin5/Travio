
using Mapster;
using Travio.Core.Domain.Entities.Destinations;
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
    }
}