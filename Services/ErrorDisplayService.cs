using System;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace ProjectManager.Services
{
    /// <summary>
    /// 错误显示服务实现 - 使用 WpfUI MessageBox
    /// </summary>
    public class ErrorDisplayService : IErrorDisplayService
    {
        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="title">标题</param>
        public async Task ShowErrorAsync(string message, string title = "错误")
        {
            var messageBox = new MessageBox
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "确定",
                IsPrimaryButtonEnabled = true,
                IsSecondaryButtonEnabled = false,
                CloseButtonText = "取消"
            };

            await messageBox.ShowDialogAsync();
        }

        /// <summary>
        /// 显示异常信息
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="title">标题</param>
        public async Task ShowExceptionAsync(Exception exception, string title = "错误")
        {
            var message = exception.InnerException != null 
                ? $"{exception.Message}\n详细信息: {exception.InnerException.Message}"
                : exception.Message;

            await ShowErrorAsync(message, title);
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        /// <param name="message">警告消息</param>
        /// <param name="title">标题</param>
        public async Task ShowWarningAsync(string message, string title = "警告")
        {
            var messageBox = new MessageBox
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "确定",
                IsPrimaryButtonEnabled = true,
                CloseButtonText = "关闭"
            };

            await messageBox.ShowDialogAsync();
        }

        /// <summary>
        /// 显示信息消息
        /// </summary>
        /// <param name="message">信息消息</param>
        /// <param name="title">标题</param>
        public async Task ShowInfoAsync(string message, string title = "信息")
        {
            var messageBox = new MessageBox
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "确定",
                IsPrimaryButtonEnabled = true,
                CloseButtonText = "关闭"
            };

            await messageBox.ShowDialogAsync();
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认消息</param>
        /// <param name="title">标题</param>
        /// <returns>用户选择的结果</returns>
        public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            var messageBox = new MessageBox
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "确定",
                IsPrimaryButtonEnabled = true,
                IsSecondaryButtonEnabled = false,
                CloseButtonText = "取消"
            };

            var result = await messageBox.ShowDialogAsync();
            return result == MessageBoxResult.Primary;
        }
    }
}
