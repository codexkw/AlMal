using System.Security.Claims;
using AlMal.Application.DTOs.Auth;
using AlMal.Application.Interfaces;
using AlMal.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AlMal.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            PhoneNumber = request.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                success = false,
                error = new
                {
                    code = "REGISTRATION_FAILED",
                    message = "فشل إنشاء الحساب",
                    details = result.Errors.Select(e => new { field = e.Code, message = e.Description })
                }
            });
        }

        await _userManager.AddToRoleAsync(user, "User");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token in user claims (placeholder approach)
        await _userManager.SetAuthenticationTokenAsync(user, "AlMal", "RefreshToken", refreshToken);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserInfo
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email!,
                UserType = user.UserType.ToString(),
                AvatarUrl = user.AvatarUrl
            }
        };

        return Ok(new { success = true, data = response });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return Unauthorized(new
            {
                success = false,
                error = new
                {
                    code = "INVALID_CREDENTIALS",
                    message = "البريد الإلكتروني أو كلمة المرور غير صحيحة"
                }
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                success = false,
                error = new
                {
                    code = "INVALID_CREDENTIALS",
                    message = "البريد الإلكتروني أو كلمة المرور غير صحيحة"
                }
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token (placeholder approach)
        await _userManager.SetAuthenticationTokenAsync(user, "AlMal", "RefreshToken", refreshToken);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserInfo
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email!,
                UserType = user.UserType.ToString(),
                AvatarUrl = user.AvatarUrl
            }
        };

        return Ok(new { success = true, data = response });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        // Find user by the stored refresh token (placeholder approach)
        // In production, store refresh tokens in a dedicated table with expiry
        var users = _userManager.Users.ToList();
        ApplicationUser? foundUser = null;

        foreach (var u in users)
        {
            var storedToken = await _userManager.GetAuthenticationTokenAsync(u, "AlMal", "RefreshToken");
            if (storedToken == request.RefreshToken)
            {
                foundUser = u;
                break;
            }
        }

        if (foundUser == null)
        {
            return Unauthorized(new
            {
                success = false,
                error = new
                {
                    code = "INVALID_REFRESH_TOKEN",
                    message = "رمز التحديث غير صالح"
                }
            });
        }

        var roles = await _userManager.GetRolesAsync(foundUser);
        var newAccessToken = _tokenService.GenerateAccessToken(foundUser, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        // Update stored refresh token
        await _userManager.SetAuthenticationTokenAsync(foundUser, "AlMal", "RefreshToken", newRefreshToken);

        var response = new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserInfo
            {
                Id = foundUser.Id,
                DisplayName = foundUser.DisplayName,
                Email = foundUser.Email!,
                UserType = foundUser.UserType.ToString(),
                AvatarUrl = foundUser.AvatarUrl
            }
        };

        return Ok(new { success = true, data = response });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new
            {
                success = false,
                error = new
                {
                    code = "UNAUTHORIZED",
                    message = "غير مصرح"
                }
            });
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new
            {
                success = false,
                error = new
                {
                    code = "USER_NOT_FOUND",
                    message = "المستخدم غير موجود"
                }
            });
        }

        var userInfo = new UserInfo
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email!,
            UserType = user.UserType.ToString(),
            AvatarUrl = user.AvatarUrl
        };

        return Ok(new { success = true, data = userInfo });
    }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = null!;
}
