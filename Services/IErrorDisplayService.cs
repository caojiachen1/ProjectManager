using System;
using System.Threading.Tasks;

namespace ProjectManager.Services
{
    /// <summary>
    /// 错误显示服务接口
    /// </summary>
    public interface IErrorDisplayService
    {
        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="title">标题</param>
        Task ShowErrorAsync(string message, string title = "错误");

        /// <summary>
        /// 显示异常信息
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="title">标题</param>
        Task ShowExceptionAsync(Exception exception, string title = "错误");

        /// <summary>
        /// 显示警告消息
        /// </summary>
        /// <param name="message">警告消息</param>
        /// <param name="title">标题</param>
        Task ShowWarningAsync(string message, string title = "警告");

        /// <summary>
        /// 显示信息消息
        /// </summary>
        /// <param name="message">信息消息</param>
        /// <param name="title">标题</param>
        Task ShowInfoAsync(string message, string title = "信息");

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认消息</param>
        /// <param name="title">标题</param>
        /// <returns>用户选择的结果</returns>
        Task<bool> ShowConfirmationAsync(string message, string title = "确认");
    }
}
