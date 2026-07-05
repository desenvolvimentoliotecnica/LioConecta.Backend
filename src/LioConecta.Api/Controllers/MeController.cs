using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController(IPersonService personService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var me = await personService.GetMeAsync(cancellationToken);
        return Ok(me);
    }

    [HttpPatch("profile/about")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PersonProfileDto>> UpdateAbout(
        [FromBody] UpdateProfileAboutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await personService.UpdateOwnAboutAsync(request.AboutMe, cancellationToken);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("profile/skills")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonProfileDto>> UpdateSkills(
        [FromBody] UpdateProfileSkillsRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await personService.UpdateOwnSkillsAsync(request.Skills, cancellationToken);
        return Ok(profile);
    }

    [HttpPatch("profile/languages")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonProfileDto>> UpdateLanguages(
        [FromBody] UpdateProfileLanguagesRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await personService.UpdateOwnLanguagesAsync(request.Languages, cancellationToken);
        return Ok(profile);
    }

    [HttpPatch("profile/links")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonProfileDto>> UpdateLinks(
        [FromBody] UpdateProfileLinksRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await personService.UpdateOwnLinksAsync(request.Links, cancellationToken);
        return Ok(profile);
    }

    [HttpPatch("profile/pronouns")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonProfileDto>> UpdatePronouns(
        [FromBody] UpdateProfilePronounsRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await personService.UpdateOwnPronounsAsync(request.Pronouns, cancellationToken);
        return Ok(profile);
    }

    [HttpPatch("profile/availability")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonProfileDto>> UpdateAvailability(
        [FromBody] UpdateProfileAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await personService.UpdateOwnAvailabilityAsync(request.Availability, cancellationToken);
        return Ok(profile);
    }

    [HttpPatch("profile/mentorship")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonProfileDto>> UpdateMentorship(
        [FromBody] UpdateProfileMentorshipRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await personService.UpdateOwnMentorshipAsync(
            request.Mentor,
            request.Buddy,
            cancellationToken);
        return Ok(profile);
    }

    [HttpPatch("profile/projects")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonProfileDto>> UpdateProjects(
        [FromBody] UpdateProfileProjectsRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await personService.UpdateOwnProjectsAsync(request.Projects, cancellationToken);
        return Ok(profile);
    }

    [HttpPatch("profile/education")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonProfileDto>> UpdateEducation(
        [FromBody] UpdateProfileEducationRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await personService.UpdateOwnEducationAsync(request.Education, cancellationToken);
        return Ok(profile);
    }

    [HttpPatch("profile/certifications")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonProfileDto>> UpdateCertifications(
        [FromBody] UpdateProfileCertificationsRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await personService.UpdateOwnCertificationsAsync(request.Certifications, cancellationToken);
        return Ok(profile);
    }

    [HttpPatch("profile/career-history")]
    [ProducesResponseType(typeof(PersonProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonProfileDto>> UpdateCareerHistory(
        [FromBody] UpdateProfileCareerHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await personService.UpdateOwnCareerHistoryAsync(request.CareerHistory, cancellationToken);
        return Ok(profile);
    }
}