using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Community;

namespace Travio.Core.Domain.Specifications.Community
{
    public class CommunityFeedSpec : Specification<Post>
    {
        public CommunityFeedSpec(int pageNumber, int pageSize)
        {
            Query.Include(x => x.User)
                .Include(x => x.Images)
                .Include(x => x.Comments)
                .Include(x => x.Likes)
                .OrderByDescending(x => x.CreatedOn)
                .Skip((pageNumber-1) * pageSize)
                .Take(pageSize)
                .AsNoTracking();
        }
    }
}
