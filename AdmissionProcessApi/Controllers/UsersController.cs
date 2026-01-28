using AdmissionProcessApi.DTOs;
using AdmissionProcessBL;
using AdmissionProcessBL.Interfaces;
using AdmissionProcessModels.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AdmissionProcessApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserLogic _userLogic;
    private readonly IStatusLogic _statusLogic;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserLogic userLogic, 
        IStatusLogic statusLogic,
        ILogger<UsersController> logger)
    {
        _userLogic = userLogic;
        _statusLogic = statusLogic;
        _logger = logger;
    }

    // 0
    [HttpPost("CreateUser")]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request)
    {
        var result = await _userLogic.CreateUserAsync(request.Email).ConfigureAwait(false);
        
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

    // 4
    [HttpGet("{userId}/GetUserStatus")]
    public async Task<IActionResult> GetUserStatusAsync(string userId)
    {
        var result = await _statusLogic.GetUserStatusAsync(userId).ConfigureAwait(false);
        
        if (!result.IsSuccess)
        {
            _logger.LogError($"GetUserStatusAsync failed for user {userId}: {result.ErrorMessage}");
            return BadRequest(new ErrorResponse { Error = result.ErrorMessage ?? "Failed to get status" });
        }
        
        return Ok(result.Data);
    }
}
