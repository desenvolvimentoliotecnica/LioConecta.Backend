using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/comunicados/hero-images")]
[Authorize]
public sealed class ComunicadoHeroImagesController(
    IComunicadoHeroImageService heroImageService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IReadOnlyList<ComunicadoHeroTemplateDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ComunicadoHeroTemplateDto>> GetTemplates()
    {
        return Ok(heroImageService.GetTemplates());
    }

    [HttpGet("uploads")]
    [ProducesResponseType(typeof(IReadOnlyList<ComunicadoHeroUploadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ComunicadoHeroUploadDto>>> GetRecentUploads(
        [FromQuery] int limit = 24,
        CancellationToken cancellationToken = default)
    {
        var uploads = await heroImageService.GetRecentUploadsAsync(limit, cancellationToken);
        return Ok(uploads);
    }

    [HttpPost("upload")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(UploadComunicadoHeroResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<UploadComunicadoHeroResponseDto>> Upload(
        IFormFile file,
        [FromForm] Guid? assetId,
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
            var result = await heroImageService.UploadAsync(
                new ComunicadoHeroUploadRequest(
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    assetId),
                uploadedById,
                cancellationToken);

            return Created(result.Url, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
