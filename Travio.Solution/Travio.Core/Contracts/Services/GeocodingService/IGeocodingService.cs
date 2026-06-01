using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Contracts.Services.GeocodingService
{
    public interface IGeocodingService
    {
        Task<(decimal Lat, decimal Lng)?> GetCoordinatesAsync(string address, CancellationToken ct = default);
    }
}
