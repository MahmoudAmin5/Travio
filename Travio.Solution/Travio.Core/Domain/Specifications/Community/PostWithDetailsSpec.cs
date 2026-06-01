using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Community;

namespace Travio.Core.Domain.Specifications.Community
{
    public class PostWithDetailsSpec : SingleResultSpecification<Post>
    {
        public PostWithDetailsSpec(int postId)
        {
            Query.Where(post => post.Id == postId)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Include(p => p.Images)
                .AsNoTracking();
        }

    }
}
