using System.ComponentModel.DataAnnotations;

namespace Travio.Core.DTOs;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; }
}
