namespace AdmissionProcessDAL.Models;

public class Phase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DefaultEnableCondition { get; set; }
    public string? DefaultVisibilityCondition { get; set; }
}
