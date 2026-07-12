using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/feedback")]
[Authorize]
public sealed class FeedbackController(IFeedbackService feedbackService) : ControllerBase
{
    [HttpPost]
    [RequirePermission("feedback.submit")]
    public async Task<ActionResult<FeedbackSubmissionDto>> Create(
        CreateFeedbackRequest request,
        CancellationToken cancellationToken) =>
        CreatedAtAction(nameof(ListMine), await feedbackService.CreateAsync(request, cancellationToken));

    [HttpGet("mine")]
    [RequirePermission("feedback.submit")]
    public async Task<ActionResult<IReadOnlyList<FeedbackSubmissionDto>>> ListMine(
        CancellationToken cancellationToken) =>
        Ok(await feedbackService.ListMineAsync(cancellationToken));

    [HttpGet]
    [RequirePermission("feedback.triage")]
    public async Task<ActionResult<IReadOnlyList<FeedbackSubmissionDto>>> List(
        [FromQuery] FeedbackStatus? status,
        CancellationToken cancellationToken) =>
        Ok(await feedbackService.ListAsync(status, cancellationToken));

    [HttpPatch("{id:guid}")]
    [RequirePermission("feedback.triage")]
    public async Task<ActionResult<FeedbackSubmissionDto>> Update(
        Guid id,
        UpdateFeedbackRequest request,
        CancellationToken cancellationToken) =>
        Ok(await feedbackService.UpdateAsync(id, request, cancellationToken));
}
