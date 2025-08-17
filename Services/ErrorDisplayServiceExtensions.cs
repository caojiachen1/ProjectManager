using System;
using System.Threading.Tasks;

namespace ProjectManager.Services
{
    /// <summary>
    /// IErrorDisplayService 便捷扩展方法
    /// </summary>
    public static class ErrorDisplayServiceExtensions
    {
        public static Task ShowIfExceptionAsync(this IErrorDisplayService service, Exception? ex, string? title = null)
        {
            if (ex == null) return Task.CompletedTask;
            return service.ShowExceptionAsync(ex, title ?? "错误");
        }
    }
}
