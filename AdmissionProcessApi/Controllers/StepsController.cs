using Microsoft.AspNetCore.Mvc;
using AdmissionProcessDAL.Models;
using AdmissionProcessApi.Models.DTOs;
using AdmissionProcessApi.Services;

namespace AdmissionProcessApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StepsController : ControllerBase
{
    private readonly IStepManagementService _stepService;

    public StepsController(IStepManagementService stepService)
    {
        _stepService = stepService;
    }

    /// <summary>
    /// Get all steps
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Step>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Step>>> GetAllSteps()
    {
        var steps = await _stepService.GetAllStepsAsync();
        return Ok(steps);
    }

    /// <summary>
    /// Get a step by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Step), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Step>> GetStep(int id)
    {
        var step = await _stepService.GetStepAsync(id);
        if (step == null)
            return NotFound();
        return Ok(step);
    }

    /// <summary>
    /// Create a new step
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Step), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Step>> CreateStep([FromBody] CreateStepRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Step name is required");

        var step = await _stepService.CreateStepAsync(request);
        return CreatedAtAction(nameof(GetStep), new { id = step.Id }, step);
    }

    /// <summary>
    /// Update a step
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Step), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Step>> UpdateStep(int id, [FromBody] UpdateStepRequest request)
    {
        try
        {
            var step = await _stepService.UpdateStepAsync(id, request);
            return Ok(step);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Delete (deactivate) a step
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStep(int id)
    {
        var deleted = await _stepService.DeleteStepAsync(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}
