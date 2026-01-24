using MasterschoolExercise.Models;

namespace MasterschoolExercise.Configuration;

public interface IFlowConfiguration
{
    List<Step> GetFlow();
    Step? GetStepByName(string stepName);
    Task? GetTaskByName(string stepName, string taskName);
}
