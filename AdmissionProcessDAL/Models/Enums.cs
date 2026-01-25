namespace AdmissionProcessDAL.Models;

public enum NodeRole : byte
{
    Step = 1,
    Task = 2
}

public enum ScopeLevel : byte
{
    Global = 1,
    Country = 2,
    University = 3
}

public enum ProgressStatus : byte
{
    NotStarted = 0,
    Passed = 1,
    Failed = 2
}
