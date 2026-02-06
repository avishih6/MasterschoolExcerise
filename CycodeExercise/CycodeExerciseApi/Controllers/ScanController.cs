using CycodeExerciseApi.Interfaces;
using CycodeExerciseApi.Models.Requests;
using CycodeExerciseApi.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CycodeExerciseApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ScanController : ControllerBase
{
    private readonly IScanService _scanService;
    private readonly ILogger<ScanController> _logger;

    public ScanController(IScanService scanService, ILogger<ScanController> logger)
    {
        _scanService = scanService;
        _logger = logger;
    }

    /// <summary>
    /// Scans a project definition file for vulnerable packages.
    /// </summary>
    /// <param name="request">The scan request containing ecosystem and base64-encoded file content</param>
    /// <returns>List of vulnerable packages with their vulnerabilities</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ScanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Scan([FromBody] ScanRequest request)
    {
        _logger.LogInformation("Received scan request for ecosystem: {Ecosystem}", request.Ecosystem);

        try
        {
            var result = await _scanService.ScanAsync(request.Ecosystem, request.FileContent);
            
            _logger.LogInformation("Scan completed. Found {Count} vulnerable packages", 
                result.VulnerablePackages.Count);
            
            return Ok(result);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Unsupported ecosystem requested");
            return BadRequest(new ErrorResponse 
            { 
                Error = "Unsupported ecosystem",
                Details = ex.Message 
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            return BadRequest(new ErrorResponse 
            { 
                Error = "Invalid request",
                Details = ex.Message 
            });
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid file format");
            return BadRequest(new ErrorResponse 
            { 
                Error = "Invalid file format",
                Details = ex.Message 
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error communicating with vulnerability provider");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse 
            { 
                Error = "Vulnerability service unavailable",
                Details = "Could not connect to vulnerability data provider. Please try again later."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during scan");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse 
            { 
                Error = "Internal server error",
                Details = "An unexpected error occurred. Please try again later."
            });
        }
    }
}
