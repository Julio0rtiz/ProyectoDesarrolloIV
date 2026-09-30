namespace TodoApi.Services
{
    public interface IOverdueTaskService
    {
        Task<int> CheckAndNotifyOverdueTaskAsync(string? userId = null);
    }
}