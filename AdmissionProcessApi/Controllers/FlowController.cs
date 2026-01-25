using Microsoft.AspNetCore.Mvc;
using AdmissionProcessApi.Models.DTOs;
using AdmissionProcessApi.Services;

namespace AdmissionProcessApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlowController : ControllerBase
{
    private readonly IFlowService _flowService;

    public FlowController(IFlowService flowService)
    {
        _flowService = flowService;
    }

    /// <summary>
    /// Retrieve the entire flow (enabling us to inform the user "You are on step 3 of 8" and display remaining steps)
    /// </summary>
    /// <param name="userId">Optional user ID to filter visible tasks based on user progress</param>
    [HttpGet]
    [ProducesResponseType(typeof(FlowResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FlowResponse>> GetFlow([FromQuery] string? userId = null)
    {
        var response = await _flowService.GetFlowAsync(userId);
        return Ok(response);
    }
}
