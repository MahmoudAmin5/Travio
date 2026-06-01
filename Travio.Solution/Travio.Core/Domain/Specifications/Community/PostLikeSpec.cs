using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Community;

namespace Travio.Core.Domain.Specifications.Community
{
    public class PostLikeSpec : SingleResultSpecification<PostLike>
    {
        public PostLikeSpec(int postId , string userId)
        {
            Query.Where(x => x.PostId == postId && x.UserId == userId);
        }
    }
}
