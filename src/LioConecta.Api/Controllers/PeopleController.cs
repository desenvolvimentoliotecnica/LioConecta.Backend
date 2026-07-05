using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
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
    public async Task<ActionResult<IReadOnlyList<PersonSummaryDto>>> GetBirthdays(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var people = await dbContext.People
            .AsNoTracking()
            .Include(p => p.Department)
            .Where(p => p.IsActive && p.BirthDate != null)
            .ToListAsync(cancellationToken);

        var upcoming = people
            .Where(p =>
            {
                var birthDate = p.BirthDate!.Value;
                var nextBirthday = new DateOnly(today.Year, birthDate.Month, birthDate.Day);
                if (nextBirthday < today)
                {
                    nextBirthday = nextBirthday.AddYears(1);
                }

                return nextBirthday.DayNumber - today.DayNumber <= days;
            })
            .OrderBy(p =>
            {
                var birthDate = p.BirthDate!.Value;
                var nextBirthday = new DateOnly(today.Year, birthDate.Month, birthDate.Day);
                return nextBirthday < today ? nextBirthday.AddYears(1) : nextBirthday;
            })
            .Select(p => PersonMapper.ToSummary(p))
            .ToList();

        return Ok(upcoming);
    }

    [HttpGet("{slug}/profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonProfileDto>> GetProfile(string slug, CancellationToken cancellationToken)
    {
        var profile = await personService.GetProfileAsync(slug, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }
}
