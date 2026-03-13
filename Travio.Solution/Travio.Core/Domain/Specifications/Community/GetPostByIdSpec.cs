using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Community;
using Travio.Core.DTOs.CommunityDTO;

namespace Travio.Core.Domain.Specifications.Community
{
    public class GetPostByIdSpec : SingleResultSpecification<Post>
    {
        public GetPostByIdSpec(int postId)
        {
            Query.Where(p => p.Id == postId);
        }
    }
}
