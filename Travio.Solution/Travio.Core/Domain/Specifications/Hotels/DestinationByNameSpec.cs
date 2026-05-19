using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Hotelbeds;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Travio.Core.Domain.Specifications.Hotels
{
    public class DestinationByNameSpec : Specification<HotelDestination>
    {
        public DestinationByNameSpec(string destinationName)
        {
           
            Query.Where(d => d.Name.ToLower().Contains(destinationName.ToLower()));
        }
    }
}
