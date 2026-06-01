using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.DTOs.DuffelHotelsDTOs;
using Travio.Core.DTOs.DuffelHotelsDTOs.Requests;
using Travio.Core.DTOs.GenericResponse;

namespace Travio.Core.Contracts.Services.DuffelHotels
{
    public interface IDuffelHotelsService
    {
        Task<ServiceResponse<List<HotelSearchResultDto>>> SearchHotelsAsync(HotelSearchRequestDto request);
        Task<ServiceResponse<HotelDetailsDto>> GetHotelDetailsAsync(string searchResultId);
    }
}
