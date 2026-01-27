using AdmissionProcessApi.Models.DTOs;
using AdmissionProcessApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdmissionProcessApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlowController : ControllerBase
{
    private readonly IFlowService _flowService;
    private readonly IProgressService _progressService;
    private readonly ILogger<FlowController> _logger;

    public FlowController(
        IFlowService flowService, 
        IProgressService progressService,
        ILogger<FlowController> logger)
    {
        _flowService = flowService;
        _progressService = progressService;
        _logger = logger;
    }

    [HttpGet("GetFlowForUser")]
    public async Task<ActionResult<FlowResponse>> GetFlowForUserAsync([FromQuery] string userId)
    {
        var result = await _flowService.GetFlowForUserAsync(userId).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to get flow for user {UserId}: {Error}", userId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        
        return Ok(result.Data);
    }

    [HttpGet("GetCurrentStepForUser")]
    public async Task<ActionResult<CurrentProgressResponse>> GetCurrentStepForUserAsync([FromQuery] string userId)
    {
        var result = await _progressService.GetCurrentProgressAsync(userId).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to get current step for user {UserId}: {Error}", userId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        
        return Ok(result.Data);
    }

    [HttpPut("Complete")]
    public async Task<IActionResult> CompleteStepAsync([FromBody] CompleteStepRequest request)
    {
        var result = await _progressService.CompleteStepAsync(
            request.UserId, 
            request.StepName, 
            request.StepPayload).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to complete step for user {UserId}: {Error}", request.UserId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        
        return NoContent();
    }
}
