using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Account_Mangement;

namespace Travio.Core.Domain.Entities.Community
{
    public class PostLike
    {
        public string UserId { get; set; }
        public int PostId { get; set; }

        public Post Post { get; set; }
        public ApplicationUser User { get; set; }

    }
}
