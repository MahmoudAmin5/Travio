using Org.BouncyCastle.Utilities.IO.Pem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Domain.Entities.Community
{
    public class PostImage
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public int PostId { get; set; }
        public string ImageUrl { get; set; }

        public Post Post { get; set; }
    }
}
