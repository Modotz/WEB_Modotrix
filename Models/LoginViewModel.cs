using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username wajib diisi.")]
    [Display(Name = "Login Sebagai")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password wajib diisi.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    /// <summary>Hanya terisi di environment Development (dropdown "Login sebagai").</summary>
    public List<DevUserViewModel> DevUsers { get; set; } = new();
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password wajib diisi.")]
    [MinLength(6, ErrorMessage = "Password minimal 6 karakter.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konfirmasi password wajib diisi.")]
    [Compare(nameof(Password), ErrorMessage = "Konfirmasi password tidak cocok.")]
    [DataType(DataType.Password)]
    public string PasswordConfirm { get; set; } = string.Empty;
}
