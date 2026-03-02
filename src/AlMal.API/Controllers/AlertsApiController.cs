using System.Security.Claims;
using AlMal.Application.DTOs.Api;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.API.Controllers;

[ApiController]
[Route("api/v1/alerts")]
[Authorize]
public class AlertsApiController : ControllerBase
{
    private readonly AlMalDbContext _db;

    public AlertsApiController(AlMalDbContext db)
    {
        _db = db;
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// GET /api/v1/alerts — List user's alerts.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAlerts()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<List<AlertDto>>.Fail("UNAUTHORIZED", "غير مصرح"));

        var alerts = await _db.Alerts
            .AsNoTracking()
            .Include(a => a.Stock)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AlertDto
            {
                Id = a.Id,
                Type = a.Type.ToString(),
                StockId = a.StockId,
                StockSymbol = a.Stock != null ? a.Stock.Symbol : null,
                StockNameAr = a.Stock != null ? a.Stock.NameAr : null,
                Condition = a.Condition,
                TargetValue = a.TargetValue,
                Channel = a.Channel.ToString(),
                IsActive = a.IsActive,
                LastTriggered = a.LastTriggered,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<AlertDto>>.Ok(alerts));
    }

    /// <summary>
    /// POST /api/v1/alerts — Create a new alert.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAlert([FromBody] CreateAlertDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<AlertDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        // Validate stock for price/volume/disclosure alerts
        if (dto.Type is AlertType.Price or AlertType.Volume or AlertType.Disclosure)
        {
            if (!dto.StockId.HasValue)
                return BadRequest(ApiResponse<AlertDto>.Fail("VALIDATION_ERROR", "يرجى اختيار السهم"));
        }

        if (dto.Type == AlertType.Price && !dto.TargetValue.HasValue)
            return BadRequest(ApiResponse<AlertDto>.Fail("VALIDATION_ERROR", "يرجى تحديد السعر المستهدف"));

        var alert = new Alert
        {
            UserId = userId,
            Type = dto.Type,
            StockId = dto.StockId,
            Condition = dto.Condition,
            TargetValue = dto.TargetValue,
            Channel = dto.Channel,
            IsActive = true
        };

        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync();

        // Load stock for response
        if (alert.StockId.HasValue)
        {
            await _db.Entry(alert).Reference(a => a.Stock).LoadAsync();
        }

        var result = new AlertDto
        {
            Id = alert.Id,
            Type = alert.Type.ToString(),
            StockId = alert.StockId,
            StockSymbol = alert.Stock?.Symbol,
            StockNameAr = alert.Stock?.NameAr,
            Condition = alert.Condition,
            TargetValue = alert.TargetValue,
            Channel = alert.Channel.ToString(),
            IsActive = alert.IsActive,
            LastTriggered = alert.LastTriggered,
            CreatedAt = alert.CreatedAt
        };

        return Ok(ApiResponse<AlertDto>.Ok(result));
    }

    /// <summary>
    /// DELETE /api/v1/alerts/{id} — Delete an alert.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAlert(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var alert = await _db.Alerts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (alert == null)
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "التنبيه غير موجود"));

        _db.Alerts.Remove(alert);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }

    /// <summary>
    /// GET /api/v1/notifications — List user's notifications.
    /// </summary>
    [HttpGet("/api/v1/notifications")]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<List<NotificationDto>>.Fail("UNAUTHORIZED", "غير مصرح"));

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();

        var notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                Type = n.Type.ToString(),
                ReferenceId = n.ReferenceId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        var pagination = new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<List<NotificationDto>>.Ok(notifications, pagination));
    }

    /// <summary>
    /// POST /api/v1/notifications/{id}/read — Mark a notification as read.
    /// </summary>
    [HttpPost("/api/v1/notifications/{id}/read")]
    public async Task<IActionResult> MarkNotificationRead(long id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "الإشعار غير موجود"));

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { read = true }));
    }
}
