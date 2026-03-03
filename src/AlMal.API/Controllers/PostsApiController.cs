using System.Security.Claims;
using AlMal.Application.DTOs.Api;
using AlMal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMal.API.Controllers;

[ApiController]
[Route("api/v1/posts")]
public class PostsApiController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsApiController(IPostService postService)
    {
        _postService = postService;
    }

    /// <summary>
    /// GET /api/v1/posts — Paginated feed (general or following)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPosts(
        [FromQuery] string tab = "general",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _postService.GetFeedAsync(currentUserId, tab, page, pageSize, ct);

        var pagination = new PaginationInfo
        {
            Page = result.Page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };

        return Ok(ApiResponse<List<PostDto>>.Ok(result.Posts, pagination));
    }

    /// <summary>
    /// GET /api/v1/posts/{id} — Single post with details
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetPost(long id, CancellationToken ct = default)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var post = await _postService.GetPostByIdAsync(id, currentUserId, ct);

        if (post is null)
            return NotFound(ApiResponse<PostDto>.Fail("POST_NOT_FOUND", "المنشور غير موجود"));

        return Ok(ApiResponse<PostDto>.Ok(post));
    }

    /// <summary>
    /// POST /api/v1/posts — Create a new post
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<PostDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        var result = await _postService.CreatePostAsync(
            userId, dto.Content,
            null, null, null, null, ct);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Fail(result.ErrorCode!, result.ErrorMessage!));

        // Fetch the created post to return full DTO
        var post = await _postService.GetPostByIdAsync(result.PostId, userId, ct);

        return Ok(ApiResponse<PostDto>.Ok(post!));
    }

    /// <summary>
    /// POST /api/v1/posts/repost — Repost an existing post
    /// </summary>
    [Authorize]
    [HttpPost("repost")]
    public async Task<IActionResult> Repost([FromBody] RepostDto dto, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var result = await _postService.RepostAsync(userId, dto.OriginalPostId, dto.Comment, ct);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Fail(result.ErrorCode!, result.ErrorMessage!));

        var post = await _postService.GetPostByIdAsync(result.PostId, userId, ct);

        return Ok(ApiResponse<PostDto>.Ok(post!));
    }

    /// <summary>
    /// POST /api/v1/posts/{id}/like — Toggle like on a post
    /// </summary>
    [Authorize]
    [HttpPost("{id:long}/like")]
    public async Task<IActionResult> ToggleLike(long id, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var result = await _postService.ToggleLikeAsync(userId, id, ct);

        if (!result.Success)
            return NotFound(ApiResponse<object>.Fail(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<object>.Ok(new
        {
            isLiked = result.IsLiked,
            likeCount = result.LikeCount
        }));
    }

    /// <summary>
    /// POST /api/v1/posts/{id}/comments — Add a comment to a post
    /// </summary>
    [Authorize]
    [HttpPost("{id:long}/comments")]
    public async Task<IActionResult> AddComment(long id, [FromBody] CreateCommentDto dto, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<CommentDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        var result = await _postService.AddCommentAsync(userId, id, dto.Content, dto.ParentCommentId, ct);

        if (!result.Success)
            return BadRequest(ApiResponse<CommentDto>.Fail(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<CommentDto>.Ok(result.Comment!));
    }

    /// <summary>
    /// GET /api/v1/posts/{id}/comments — Paginated comments for a post
    /// </summary>
    [HttpGet("{id:long}/comments")]
    public async Task<IActionResult> GetComments(long id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _postService.GetCommentsAsync(id, page, pageSize, ct);

        if (!result.Success)
            return NotFound(ApiResponse<List<CommentDto>>.Fail(result.ErrorCode!, result.ErrorMessage!));

        var pagination = new PaginationInfo
        {
            Page = result.Page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };

        return Ok(ApiResponse<List<CommentDto>>.Ok(result.Comments, pagination));
    }

    /// <summary>
    /// DELETE /api/v1/posts/{id} — Soft delete a post (owner only)
    /// </summary>
    [Authorize]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeletePost(long id, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var result = await _postService.DeletePostAsync(userId, id, ct);

        if (!result.Success)
        {
            if (result.ErrorCode == "FORBIDDEN")
                return StatusCode(403, ApiResponse<object>.Fail(result.ErrorCode, result.ErrorMessage!));
            return NotFound(ApiResponse<object>.Fail(result.ErrorCode!, result.ErrorMessage!));
        }

        return Ok(ApiResponse<object>.Ok(new { message = "تم حذف المنشور بنجاح" }));
    }

    /// <summary>
    /// POST /api/v1/posts/{id}/report — Report a post (flags it for moderation)
    /// </summary>
    [Authorize]
    [HttpPost("{id:long}/report")]
    public async Task<IActionResult> ReportPost(long id, [FromBody] ReportPostDto? dto = null, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var result = await _postService.ReportPostAsync(userId, id, dto?.Reason, ct);

        if (!result.Success)
            return NotFound(ApiResponse<object>.Fail(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<object>.Ok(new { message = "تم الإبلاغ عن المنشور بنجاح" }));
    }
}
