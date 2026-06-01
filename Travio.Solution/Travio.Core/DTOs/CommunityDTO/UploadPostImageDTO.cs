using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.CommunityDTO
{
    public record UploadPostImageDTO (List<IFormFile> Images);
   
}
