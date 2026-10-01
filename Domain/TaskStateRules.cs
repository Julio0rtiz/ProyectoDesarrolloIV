using TodoApi.Models;

namespace TodoApi.Domain
{

    public static class TaskStateRules
    {
        private static readonly IReadOnlyDictionary<TaskState, TaskState[]> AllowedTransitions =
            new Dictionary<TaskState, TaskState[]>
            {
                [TaskState.Pending]    = new[] { TaskState.InProgress, TaskState.Cancelled },
                [TaskState.InProgress] = new[] { TaskState.Completed, TaskState.Cancelled },
                [TaskState.Completed]  = Array.Empty<TaskState>(),
                [TaskState.Cancelled]  = Array.Empty<TaskState>()
            };

        public static bool IsFinal(TaskState state) =>
            state == TaskState.Completed || state == TaskState.Cancelled;


        public static void EnsureCanChange(TodoItem item, TaskState target, bool force, DateTime utcNow)
        {
            var current = item.State;

            if (!AllowedTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(target))
            {
                var reason = IsFinal(current)
                    ? $"'{current}' is a final status."
                    : $"Allowed transitions from '{current}': {string.Join(", ", allowed ?? Array.Empty<TaskState>())}.";

                throw new InvalidStateTransitionException(
                    $"Invalid status transition from '{current}' to '{target}'. {reason}");
            }
            
            if (target == TaskState.Completed
                && !force
                && item.DueDate.HasValue
                && item.DueDate.Value < utcNow)
            {
                throw new InvalidStateTransitionException(
                    $"The task is overdue (due date: {item.DueDate.Value:u}). " +
                    "Set 'force' to true to complete it anyway.");
            }
        }
    }
}
