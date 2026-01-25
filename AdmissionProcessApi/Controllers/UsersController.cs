using Microsoft.AspNetCore.Mvc;
using AdmissionProcessApi.Models.DTOs;
using AdmissionProcessApi.Services;

namespace AdmissionProcessApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IFlowService _flowService;
    private readonly IProgressService _progressService;

    public UsersController(
        IUserService userService,
        IFlowService flowService,
        IProgressService progressService)
    {
        _userService = userService;
        _flowService = flowService;
        _progressService = progressService;
    }

    /// <summary>
    /// Create a new user in the system
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateUserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email is required");
        }

        var response = await _userService.CreateUserAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Get the current step and task for a specific user
    /// </summary>
    [HttpGet("{userId}/progress")]
    [ProducesResponseType(typeof(UserProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProgressResponse>> GetUserProgress(string userId)
    {
        try
        {
            var response = await _progressService.GetUserProgressAsync(userId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Mark a step as completed for a specific user
    /// </summary>
    [HttpPut("{userId}/steps/{stepName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteStep(string userId, string stepName, [FromBody] Dictionary<string, object> stepPayload)
    {
        try
        {
            var request = new CompleteStepRequest
            {
                UserId = userId,
                StepName = stepName,
                StepPayload = stepPayload
            };

            await _progressService.CompleteStepAsync(request);
            return Ok(new { message = "Step completed successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Check whether a user is accepted, rejected, or still in progress
    /// </summary>
    [HttpGet("{userId}/status")]
    [ProducesResponseType(typeof(UserStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserStatusResponse>> GetUserStatus(string userId)
    {
        try
        {
            var response = await _progressService.GetUserStatusAsync(userId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
