namespace EnterpriseFlow.Domain.Enums;

/// <summary>
/// Named <c>ProjectTaskStatus</c> (not <c>TaskStatus</c>) to avoid clashing with
/// <see cref="System.Threading.Tasks.TaskStatus"/>, which every file in this codebase can
/// otherwise pull in via an unrelated <c>using System.Threading.Tasks;</c>.
/// </summary>
public enum ProjectTaskStatus
{
    Todo = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
}
