using System.Security.Claims;
using AlMal.Application.Interfaces;
using AlMal.Domain.Entities;
using AlMal.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AlMal.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWhatsAppService _whatsAppService;

    // In-memory verification codes (production should use Redis/DB)
    private static readonly Dictionary<string, (string Code, DateTime Expiry)> _verificationCodes = new();

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IWhatsAppService whatsAppService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _whatsAppService = whatsAppService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "تم قفل الحساب بسبب محاولات تسجيل دخول متعددة فاشلة. حاول مرة أخرى لاحقاً.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
        return View(model);
    }

    [HttpGet]
    [Authorize]
    public IActionResult Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return RedirectToAction("Index", "Profile", new { id = userId });
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "كلمتا المرور غير متطابقتين.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            PhoneNumber = model.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var arabicMessage = error.Code switch
                {
                    "DuplicateEmail" => "البريد الإلكتروني مسجل مسبقاً.",
                    "DuplicateUserName" => "اسم المستخدم مسجل مسبقاً.",
                    "PasswordTooShort" => "كلمة المرور قصيرة جداً.",
                    "PasswordRequiresNonAlphanumeric" => "كلمة المرور يجب أن تحتوي على رمز خاص.",
                    "PasswordRequiresDigit" => "كلمة المرور يجب أن تحتوي على رقم.",
                    "PasswordRequiresUpper" => "كلمة المرور يجب أن تحتوي على حرف كبير.",
                    "PasswordRequiresLower" => "كلمة المرور يجب أن تحتوي على حرف صغير.",
                    _ => error.Description
                };
                ModelState.AddModelError(string.Empty, arabicMessage);
            }
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "User");
        await _signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToAction("Index", "Market");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Market");
    }

    /// <summary>
    /// POST /Account/WhatsAppOptIn — Enter phone number, send verification code.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WhatsAppOptIn(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            TempData["WhatsAppError"] = "يرجى إدخال رقم الهاتف";
            return RedirectToAction("Profile");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        // Generate 6-digit code
        var code = new Random().Next(100000, 999999).ToString();
        _verificationCodes[user.Id] = (code, DateTime.UtcNow.AddMinutes(10));

        // Save phone number temporarily
        user.WhatsAppNumber = phoneNumber;
        await _userManager.UpdateAsync(user);

        // Send verification code
        var sent = await _whatsAppService.SendVerificationCodeAsync(phoneNumber, code);

        if (sent)
        {
            TempData["WhatsAppMessage"] = "تم إرسال رمز التحقق إلى واتساب";
            TempData["WhatsAppPendingVerification"] = "true";
        }
        else
        {
            TempData["WhatsAppError"] = "فشل إرسال رمز التحقق. تأكد من صحة الرقم وحاول مرة أخرى";
        }

        return RedirectToAction("Profile");
    }

    /// <summary>
    /// POST /Account/WhatsAppVerify — Verify code, set WhatsAppOptIn = true.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WhatsAppVerify(string verificationCode)
    {
        if (string.IsNullOrWhiteSpace(verificationCode))
        {
            TempData["WhatsAppError"] = "يرجى إدخال رمز التحقق";
            return RedirectToAction("Profile");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        if (!_verificationCodes.TryGetValue(user.Id, out var stored))
        {
            TempData["WhatsAppError"] = "لم يتم إرسال رمز تحقق. حاول مرة أخرى";
            return RedirectToAction("Profile");
        }

        if (DateTime.UtcNow > stored.Expiry)
        {
            _verificationCodes.Remove(user.Id);
            TempData["WhatsAppError"] = "انتهت صلاحية رمز التحقق. أعد الإرسال";
            return RedirectToAction("Profile");
        }

        if (stored.Code != verificationCode.Trim())
        {
            TempData["WhatsAppError"] = "رمز التحقق غير صحيح";
            return RedirectToAction("Profile");
        }

        // Verification successful
        _verificationCodes.Remove(user.Id);
        user.WhatsAppOptIn = true;
        await _userManager.UpdateAsync(user);

        TempData["WhatsAppMessage"] = "تم تفعيل إشعارات واتساب بنجاح";
        return RedirectToAction("Profile");
    }

    /// <summary>
    /// POST /Account/WhatsAppOptOut — Disable WhatsApp notifications.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WhatsAppOptOut()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        user.WhatsAppOptIn = false;
        user.WhatsAppNumber = null;
        await _userManager.UpdateAsync(user);

        TempData["WhatsAppMessage"] = "تم إلغاء إشعارات واتساب";
        return RedirectToAction("Profile");
    }
}
