using AdmissionProcessApi.DTOs;
using AdmissionProcessBL.Interfaces;
using AdmissionProcessModels.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AdmissionProcessApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlowController : ControllerBase
{
    private readonly IFlowLogic _flowLogic;
    private readonly IProgressLogic _progressLogic;
    private readonly ILogger<FlowController> _logger;

    public FlowController(
        IFlowLogic flowLogic, 
        IProgressLogic progressLogic,
        ILogger<FlowController> logger)
    {
        _flowLogic = flowLogic;
        _progressLogic = progressLogic;
        _logger = logger;
    }

    // 1
    [HttpGet("GetEntireFlowForUser")]
    public async Task<IActionResult> GetEntireFlowForUserAsync([FromQuery] string userId)
    {
        var result = await _flowLogic.GetEntireFlowForUserAsync(userId).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogError($"GetEntireFlowForUserAsync failed for user {userId}: {result.ErrorMessage}");
            return StatusCode(result.HttpStatusCode ?? 500, new ErrorResponse { Error = result.ErrorMessage ?? "Failed to get flow" });
        }
        
        return Ok(result.Data);
    }

    // 2
    [HttpGet("GetCurrentStepAndTaskForUser")]
    public async Task<IActionResult> GetCurrentStepAndTaskForUserAsync([FromQuery] string userId)
    {
        var result = await _progressLogic.GetCurrentStepAndTaskForUserAsync(userId).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogError($"GetCurrentStepAndTaskForUserAsync failed for user {userId}: {result.ErrorMessage}");
            return StatusCode(result.HttpStatusCode ?? 500, new ErrorResponse { Error = result.ErrorMessage ?? "Failed to get current step" });
        }
        
        return Ok(result.Data);
    }

    // 3
    [HttpPut("CompleteStep")]
    public async Task<IActionResult> CompleteStepAsync([FromBody] CompleteStepRequest request)
    {
        var result = await _progressLogic.CompleteStepAsync(
            request.UserId, 
            request.StepName, 
            request.StepPayload).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogError($"CompleteStepAsync failed for user {request.UserId}: {result.ErrorMessage}");
            return StatusCode(result.HttpStatusCode ?? 500, new ErrorResponse { Error = result.ErrorMessage ?? "Failed to complete step" });
        }
        
        return NoContent();
    }
}
