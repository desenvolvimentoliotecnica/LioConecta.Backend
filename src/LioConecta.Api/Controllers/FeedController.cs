using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/feed")]
[Authorize]
public sealed class FeedController(
    IFeedService feedService,
    IPostMediaService postMediaService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeed(
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var page = await feedService.GetFeedAsync(
            new CursorPageRequest { Cursor = cursor, Limit = limit },
            cancellationToken);

        return Ok(page);
    }

    [HttpGet("posts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPost(Guid id, CancellationToken cancellationToken)
    {
        var post = await feedService.GetPostAsync(id, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost("posts")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePost(
        [FromBody] CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await feedService.CreatePostAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("posts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await feedService.DeletePostAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
    }

    [HttpPost("posts/media/upload")]
    [ProducesResponseType(typeof(UploadPostMediaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<UploadPostMediaResponseDto>> UploadPostMedia(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Nenhum arquivo enviado." });
        }

        try
        {
            var uploadedById = await currentUserService.GetPersonIdAsync(cancellationToken);
            await using var stream = file.OpenReadStream();
            var result = await postMediaService.UploadAsync(
                new PostMediaUploadRequest(
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length),
                uploadedById,
                cancellationToken);

            return Created(result.Url, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("posts/{postId:guid}/comments")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddComment(
        Guid postId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await feedService.AddCommentAsync(postId, request, cancellationToken);
        return Created(string.Empty, comment);
    }

    [HttpGet("posts/{postId:guid}/media/comments")]
    [ProducesResponseType(typeof(PostMediaCommentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPostMediaComments(
        Guid postId,
        [FromQuery] string mediaUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            return BadRequest(new { message = "mediaUrl é obrigatório." });
        }

        try
        {
            var comments = await feedService.GetPostMediaCommentsAsync(postId, mediaUrl, cancellationToken);
            return Ok(new PostMediaCommentsResponse(comments));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("posts/{postId:guid}/media/comments")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPostMediaComment(
        Guid postId,
        [FromBody] CreatePostMediaCommentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var comment = await feedService.AddPostMediaCommentAsync(postId, request, cancellationToken);
            return Created(string.Empty, comment);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("posts/{postId:guid}/reactions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> React(
        Guid postId,
        [FromBody] ReactionRequest request,
        CancellationToken cancellationToken)
    {
        await feedService.ReactAsync(postId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("posts/{postId:guid}/poll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPoll(Guid postId, CancellationToken cancellationToken)
    {
        var poll = await feedService.GetPollAsync(postId, cancellationToken);
        return poll is null ? NotFound() : Ok(poll);
    }

    [HttpPost("posts/{postId:guid}/poll/vote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> VotePoll(
        Guid postId,
        [FromBody] VotePollRequest request,
        CancellationToken cancellationToken)
    {
        await feedService.VotePollAsync(postId, request, cancellationToken);
        return NoContent();
    }
}
