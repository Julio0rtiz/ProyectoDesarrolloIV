namespace TodoApi.Services
{
    public interface IOverdueTaskServices
    {
        Task<int> CheckAndNotifyOverdueTaskAsync(string? userId = null);

    }
}