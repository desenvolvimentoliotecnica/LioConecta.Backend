using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/chat")]
[Authorize]
public sealed class ChatController(IChatService chatService) : ControllerBase
{
    [HttpGet("conversations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ChatConversationDto>>> GetConversations(
        CancellationToken cancellationToken)
    {
        var conversations = await chatService.GetConversationsAsync(cancellationToken);
        return Ok(conversations);
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var page = await chatService.GetMessagesAsync(
            conversationId,
            new CursorPageRequest { Cursor = cursor, Limit = limit },
            cancellationToken);

        return Ok(page);
    }

    [HttpPost("conversations/{conversationId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ChatMessageDto>> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var message = await chatService.SendMessageAsync(conversationId, request, cancellationToken);
        return Created(string.Empty, message);
    }
}
