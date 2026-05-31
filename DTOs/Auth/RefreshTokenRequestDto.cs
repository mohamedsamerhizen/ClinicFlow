using System.ComponentModel.DataAnnotations;

namespace ClinicFlow.DTOs.Auth;

public class RefreshTokenRequestDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
