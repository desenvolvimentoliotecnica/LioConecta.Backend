using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Api.Authorization;
using LioConecta.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/people")]
[Authorize]
public sealed class PeopleController(
    IPersonService personService,
    IFeedService feedService,
    IFeedRepository feedRepository,
    ICurrentUserService currentUserService,
    AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PersonSummaryDto>>> Search(
        [FromQuery] string? q,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var people = await personService.SearchAsync(q ?? string.Empty, limit, cancellationToken);
        return Ok(people);
    }

    [HttpGet("directory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonDirectoryDto>> GetDirectory(
        [FromQuery] string? q,
        [FromQuery] string? department,
        CancellationToken cancellationToken = default)
    {
        var directory = await personService.GetDirectoryAsync(q, department, cancellationToken);
        return Ok(directory);
    }

    [HttpGet("org-chart")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrgChartDto>> GetOrgChart(CancellationToken cancellationToken)
    {
        var orgChart = await personService.GetOrgChartAsync(cancellationToken);
        return Ok(orgChart);
    }

    [HttpGet("new-hires")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PersonSummaryDto>>> GetNewHires(
        [FromQuery] int days = 90,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));
        var people = await dbContext.People
            .AsNoTracking()
            .Include(p => p.Department)
            .Where(p => p.IsActive && p.HireDate != null && p.HireDate >= cutoff)
            .OrderByDescending(p => p.HireDate)
            .ToListAsync(cancellationToken);

        return Ok(people.Select(p => PersonMapper.ToSummary(p)).ToList());
    }

    [HttpGet("birthdays")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BirthdayPersonDto>>> GetBirthdays(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var people = await dbContext.People
            .AsNoTracking()
            .Include(p => p.Department)
            .Where(p => p.IsActive && p.BirthDate != null)
            .ToListAsync(cancellationToken);

        // Inclui: próximos `days` dias OU aniversários já ocorridos no mês corrente
        // (ex.: dia 06/07 ainda aparece em "Este mês" quando hoje é 08/07).
        var upcoming = people
            .Where(p =>
            {
                var birthDate = p.BirthDate!.Value;
                var birthdayThisYear = new DateOnly(today.Year, birthDate.Month, birthDate.Day);
                var alreadyCelebratedThisMonth =
                    birthdayThisYear.Month == today.Month && birthdayThisYear <= today;

                var nextBirthday = birthdayThisYear < today
                    ? birthdayThisYear.AddYears(1)
                    : birthdayThisYear;
                var withinUpcomingWindow = nextBirthday.DayNumber - today.DayNumber <= days;

                return alreadyCelebratedThisMonth || withinUpcomingWindow;
            })
            .OrderBy(p =>
            {
                var birthDate = p.BirthDate!.Value;
                // Ordena pelo aniversário deste ano (passados do mês primeiro, depois futuros).
                return new DateOnly(today.Year, birthDate.Month, birthDate.Day);
            })
            .ToList();

        var currentPersonId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var congratulatedIds = await feedRepository.GetCelebratedPersonIdsByAuthorInYearAsync(
            currentPersonId,
            today.Year,
            cancellationToken);

        var result = upcoming
            .Select(p =>
            {
                var summary = PersonMapper.ToSummary(p);
                return new BirthdayPersonDto(
                    summary.Id,
                    summary.Slug,
                    summary.Name,
                    summary.Title,
                    summary.PhotoUrl,
                    summary.DepartmentName,
                    summary.Location,
                    summary.ManagerSlug,
                    summary.IsActive,
                    summary.BirthDate,
                    congratulatedIds.Contains(p.Id));
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("{slug}/hierarchy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonHierarchyDto>> GetHierarchy(string slug, CancellationToken cancellationToken)
    {
        var hierarchy = await personService.GetHierarchyAsync(slug, cancellationToken);
        return hierarchy is null ? NotFound() : Ok(hierarchy);
    }

    [HttpGet("{slug}/profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonProfileDto>> GetProfile(string slug, CancellationToken cancellationToken)
    {
        var profile = await personService.GetProfileAsync(slug, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("{slug}/post-media")]
    [RequirePermission("feed.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<PersonPostMediaItemDto>>> GetPostMedia(
        string slug,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 40,
        CancellationToken cancellationToken = default)
    {
        var page = await feedService.GetAuthorPostMediaAsync(
            slug,
            new CursorPageRequest { Cursor = cursor, Limit = limit },
            cancellationToken);

        return page is null ? NotFound() : Ok(page);
    }

    [HttpPatch("{personKey}/profile/avatar")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonProfileDto>> UpdateAvatar(
        string personKey,
        [FromBody] UpdateProfileAvatarRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await personService.UpdatePersonAvatarAsync(personKey, request.PhotoUrl, cancellationToken);
            return Ok(profile);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrado", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
