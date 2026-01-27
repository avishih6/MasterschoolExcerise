using AdmissionProcessApi.Models.DTOs;
using AdmissionProcessApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdmissionProcessApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IStatusService _statusService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService, 
        IStatusService statusService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _statusService = statusService;
        _logger = logger;
    }

    [HttpPost("CreateUser")]
    public async Task<ActionResult<CreateUserResponse>> CreateUserAsync([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request.Email).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create user: {Error}", result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        
        return Ok(new CreateUserResponse { UserId = result.Data! });
    }

    [HttpGet("{userId}/Status")]
    public async Task<ActionResult<StatusResponse>> GetStatusAsync(string userId)
    {
        var result = await _statusService.GetStatusAsync(userId).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to get status for user {UserId}: {Error}", userId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        
        return Ok(result.Data);
    }
}
