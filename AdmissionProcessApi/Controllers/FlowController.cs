using AdmissionProcessApi.DTOs;
using AdmissionProcessBL.Services.Interfaces;
using AdmissionProcessModels.DTOs;
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

    // 2
    [HttpGet("GetEntireFlowForUser")]
    public async Task<IActionResult> GetEntireFlowForUserAsync([FromQuery] string userId)
    {
        var result = await _flowService.GetEntireFlowForUserAsync(userId).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogError($"GetEntireFlowForUserAsync failed for user {userId}: {result.ErrorMessage}");
            return BadRequest(new ErrorResponse { Error = result.ErrorMessage ?? "Failed to get flow" });
        }
        
        return Ok(result.Data);
    }

    // 3
    [HttpGet("GetCurrentStepAndTaskForUser")]
    public async Task<IActionResult> GetCurrentStepAndTaskForUserAsync([FromQuery] string userId)
    {
        var result = await _progressService.GetCurrentStepAndTaskForUserAsync(userId).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogError($"GetCurrentStepAndTaskForUserAsync failed for user {userId}: {result.ErrorMessage}");
            return BadRequest(new ErrorResponse { Error = result.ErrorMessage ?? "Failed to get current step" });
        }
        
        return Ok(result.Data);
    }

    // 4
    [HttpPut("CompleteStep")]
    public async Task<IActionResult> CompleteStepAsync([FromBody] CompleteStepRequest request)
    {
        var result = await _progressService.CompleteStepAsync(
            request.UserId, 
            request.StepName, 
            request.StepPayload).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogError($"CompleteStepAsync failed for user {request.UserId}: {result.ErrorMessage}");
            
            if (result.HttpStatusCode == 404)
                return NotFound(new ErrorResponse { Error = result.ErrorMessage ?? "Step not found" });
            
            return BadRequest(new ErrorResponse { Error = result.ErrorMessage ?? "Failed to complete step" });
        }
        
        return NoContent();
    }
}
