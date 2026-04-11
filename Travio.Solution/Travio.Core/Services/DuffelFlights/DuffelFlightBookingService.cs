using Duffel.ApiClient;
using Duffel.ApiClient.Models.Requests;
using Duffel.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services.DuffelFlights;
using Travio.Core.DTOs.DuffelFlightsDTOs;
using Travio.Core.DTOs.GenericResponse;

namespace Travio.Core.Services.DuffelFlights
{
    public class DuffelFlightBookingService : IDuffelFlightBookingService
    {
        private readonly IDuffelApiClient _duffelClient;

        public DuffelFlightBookingService(IDuffelApiClient duffelClient)
        {
            _duffelClient = duffelClient;
        }
        public async Task<ServiceResponse<List<FlightSearchResponseDto>>> SearchFlightsAsync(FlightSearchRequestDto request)
        {
            try
            {
                // 1. Build the Duffel Request
                var duffelRequest = new OffersRequest
                {
                    Passengers = new List<Passenger>(),
                    Slices = new List<Slice>
                    {
                        new Slice
                        {
                            Origin = request.Origin,
                            Destination = request.Destination,
                            DepartureDate = request.DepartureDate
                        }
                    },
                    CabinClass = CabinClass.Economy
                };

                // Add the requested number of adult passengers
                for (int i = 0; i < request.NumberOfAdults; i++)
                {
                    duffelRequest.Passengers.Add(new Passenger { PassengerType = PassengerType.Adult });
                }

                // 2. Call the live Duffel API
                var offerResponse = await _duffelClient.OfferRequests.Create(duffelRequest);

                // 3. Map the response to our clean DTO
                var flights = new List<FlightSearchResponseDto>();

                foreach (var offer in offerResponse.Offers)
                {
                    // For a simple one-way flight, we just look at the first "Slice" and first "Segment"
                    var firstSlice = offer.Slices.FirstOrDefault();
                    var firstSegment = firstSlice?.Segments.FirstOrDefault();

                    if (firstSlice != null && firstSegment != null)
                    {
                        flights.Add(new FlightSearchResponseDto
                        {
                            OfferId = offer.Id,
                            AirlineName = offer.Owner.AirlineName,
                            Origin = firstSlice.Origin.IataCode,
                            Destination = firstSlice.Destination.IataCode,
                            DepartureTime = firstSegment.DepartingAt,
                            ArrivalTime = firstSegment.ArrivingAt,
                            TotalPrice = decimal.Parse(offer.TotalAmount),
                            Currency = offer.TotalCurrency
                        });
                    }
                }

                // 4. Return the list, sorted by cheapest flight first!
                return new ServiceResponse<List<FlightSearchResponseDto>>
                {
                    Success = true,
                    Message = "Flights retrieved successfully.",
                    Data = flights.OrderBy(f => f.TotalPrice).ToList()
                };
            }
            catch (Duffel.ApiClient.Exceptions.ApiException ex)
            {
                // Duffel returns a list of errors. We grab the first one to see what we did wrong!
                var realErrorMessage = ex.Errors.FirstOrDefault()?.Message ?? "Unknown API Error";

                return new ServiceResponse<List<FlightSearchResponseDto>>
                {
                    Success = false,
                    Message = $"Duffel rejected the search: {realErrorMessage}"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<FlightSearchResponseDto>>
                {
                    Success = false,
                    Message = "Failed to fetch flights from the provider."
                };
            }
        }
    }
    
}
