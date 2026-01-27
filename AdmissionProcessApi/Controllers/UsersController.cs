using AdmissionProcessApi.DTOs;
using AdmissionProcessBL.Services;
using AdmissionProcessBL.Services.Interfaces;
using AdmissionProcessModels.DTOs;
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

    // 1
    [HttpPost("CreateUser")]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request.Email).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            if (result.HttpStatusCode == 409)
            {
                _logger.LogInformation($"CreateUserAsync: user already exists with email {request.Email}");
                return Conflict(new ErrorResponse 
                { 
                    Error = result.ErrorMessage ?? "User already exists",
                    ExistingUserId = result.Data?.UserId
                });
            }
            
            _logger.LogError($"CreateUserAsync failed: {result.ErrorMessage}");
            return BadRequest(new ErrorResponse { Error = result.ErrorMessage ?? "Failed to create user" });
        }
        
        return Ok(result.Data);
    }

    // 5
    [HttpGet("{userId}/GetUserStatus")]
    public async Task<IActionResult> GetUserStatusAsync(string userId)
    {
        var result = await _statusService.GetUserStatusAsync(userId).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogError($"GetUserStatusAsync failed for user {userId}: {result.ErrorMessage}");
            return BadRequest(new ErrorResponse { Error = result.ErrorMessage ?? "Failed to get status" });
        }
        
        return Ok(result.Data);
    }
}
