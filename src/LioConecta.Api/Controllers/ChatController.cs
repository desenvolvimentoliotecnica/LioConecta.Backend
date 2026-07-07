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
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(ChatBootstrapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatBootstrapDto>> GetBootstrap(CancellationToken cancellationToken)
    {
        var bootstrap = await chatService.GetBootstrapAsync(cancellationToken);
        return Ok(bootstrap);
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(ChatStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await chatService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("link-account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LinkAccount(
        [FromBody] LinkTeamsAccountRequest request,
        CancellationToken cancellationToken)
    {
        await chatService.LinkAccountAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("link-account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnlinkAccount(CancellationToken cancellationToken)
    {
        await chatService.UnlinkAccountAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("conversations")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatConversationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ChatConversationDto>>> GetConversations(
        CancellationToken cancellationToken)
    {
        var conversations = await chatService.GetConversationsAsync(cancellationToken);
        return Ok(conversations);
    }

    [HttpPost("conversations")]
    [ProducesResponseType(typeof(ChatConversationDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ChatConversationDto>> CreateConversation(
        [FromBody] CreateChatConversationRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = await chatService.CreateConversationAsync(request, cancellationToken);
        return Created(string.Empty, conversation);
    }

    [HttpGet("conversations/{conversationId}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(
        string conversationId,
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

    [HttpPost("conversations/{conversationId}/messages")]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ChatMessageDto>> SendMessage(
        string conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var message = await chatService.SendMessageAsync(conversationId, request, cancellationToken);
        return Created(string.Empty, message);
    }
}
