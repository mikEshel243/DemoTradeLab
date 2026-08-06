using DemoTradeLab.Api.Contracts.DemoProfiles;
using DemoTradeLab.Core.DemoProfiles;
using Microsoft.AspNetCore.Mvc;

namespace DemoTradeLab.Api.Controllers;

[ApiController]
[Route("api/demo-profiles")]
public sealed class DemoProfilesController(DemoProfileService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DemoProfileResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DemoProfileResponse>>> List(
        CancellationToken cancellationToken)
    {
        var profiles = await service.ListAsync(cancellationToken);

        return Ok(profiles.Select(profile => profile.ToResponse()).ToArray());
    }
}
