using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.DTOs.DuffelFlightsDTOs;
using Travio.Core.DTOs.DuffelFlightsDTOs.Requests;
using Travio.Core.DTOs.GenericResponse;

namespace Travio.Core.Contracts.Services.DuffelFlights
{
    public interface IDuffelFlightBookingService
    {
        Task<ServiceResponse<List<FlightSearchResponseDto>>> SearchFlightsAsync(FlightSearchRequestDto request);
        Task<ServiceResponse<List<TopFlightOfferDto>>> GetTopOffersAsync();
        Task<ServiceResponse<FlightDetailsDto>> GetFlightDetailsAsync(string offerId);
        Task<ServiceResponse<FlightOrderResponseDto>> CreateOrderAsync(FlightOrderRequestDto request);
        Task<ServiceResponse<CheckoutResponseDto>> CreateCheckoutSessionAsync(CheckoutRequestDto request);
    }
}
