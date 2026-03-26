using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Travio.Core.Domain.Entities.Community;

namespace Travio.Core.Domain.Specifications.Community
{
    public class GetCommentByIdSpec : SingleResultSpecification<Comment>
    {
        public GetCommentByIdSpec(int commentId)
        {
            Query.Where(c => c.Id == commentId);
            
        }
    }
    
    
}
