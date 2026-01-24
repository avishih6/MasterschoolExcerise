using Microsoft.AspNetCore.Mvc;
using MasterschoolExercise.Models;
using MasterschoolExercise.Models.DTOs;
using MasterschoolExercise.Services;

namespace MasterschoolExercise.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskManagementService _taskService;

    public TasksController(ITaskManagementService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Get all tasks
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<FlowTask>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FlowTask>>> GetAllTasks()
    {
        var tasks = await _taskService.GetAllTasksAsync();
        return Ok(tasks);
    }

    /// <summary>
    /// Get a task by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FlowTask), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FlowTask>> GetTask(int id)
    {
        var task = await _taskService.GetTaskAsync(id);
        if (task == null)
            return NotFound();
        return Ok(task);
    }

    /// <summary>
    /// Create a new task
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FlowTask), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FlowTask>> CreateTask([FromBody] CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Task name is required");

        var task = await _taskService.CreateTaskAsync(request);
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    /// <summary>
    /// Update a task
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(FlowTask), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FlowTask>> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        try
        {
            var task = await _taskService.UpdateTaskAsync(id, request);
            return Ok(task);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Delete (deactivate) a task
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var deleted = await _taskService.DeleteTaskAsync(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Assign a task to a step
    /// </summary>
    [HttpPost("{taskId}/steps/{stepId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignTaskToStep(int taskId, int stepId, [FromBody] AssignTaskToStepRequest? request = null)
    {
        var order = request?.Order ?? 1;
        var isRequired = request?.IsRequired ?? true;
        
        await _taskService.AssignTaskToStepAsync(stepId, taskId, order, isRequired);
        return Ok(new { message = "Task assigned to step successfully" });
    }

    /// <summary>
    /// Remove a task from a step
    /// </summary>
    [HttpDelete("{taskId}/steps/{stepId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTaskFromStep(int taskId, int stepId)
    {
        var removed = await _taskService.RemoveTaskFromStepAsync(stepId, taskId);
        if (!removed)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Assign a task to a specific user
    /// </summary>
    [HttpPost("{taskId}/users/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignTaskToUser(int taskId, string userId)
    {
        await _taskService.AssignTaskToUserAsync(userId, taskId);
        return Ok(new { message = "Task assigned to user successfully" });
    }

    /// <summary>
    /// Remove a task from a user
    /// </summary>
    [HttpDelete("{taskId}/users/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTaskFromUser(int taskId, string userId)
    {
        var removed = await _taskService.RemoveTaskFromUserAsync(userId, taskId);
        if (!removed)
            return NotFound();
        return NoContent();
    }
}

public class AssignTaskToStepRequest
{
    public int Order { get; set; } = 1;
    public bool IsRequired { get; set; } = true;
}
