using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjectManager.Models
{
    /// <summary>
    /// 终端会话模型
    /// </summary>
    public partial class TerminalSession : ObservableObject
    {
        [ObservableProperty]
        private string _sessionId = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _projectName = string.Empty;

        [ObservableProperty]
        private string _projectPath = string.Empty;

        [ObservableProperty]
        private string _command = string.Empty;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private DateTime _startTime = DateTime.Now;

        [ObservableProperty]
        private ObservableCollection<string> _outputLines = new();

        [ObservableProperty]
        private string _status = "已停止";

        [ObservableProperty]
        private Dictionary<string, string> _environmentVariables = new();

        private Process? _process;
        private readonly object _lockObject = new();

        [JsonIgnore]
        public Process? Process
        {
            get => _process;
            set => _process = value;
        }

        /// <summary>
        /// 添加输出行
        /// </summary>
        /// <param name="line">输出内容</param>
        public void AddOutputLine(string line)
        {
            lock (_lockObject)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OutputLines.Add($"[{DateTime.Now:HH:mm:ss}] {line}");
                });
            }
        }

        /// <summary>
        /// 追加原始输出片段（不添加时间戳，不强制换行）。
        /// 保留控制字符（如 \r、\b、ESC 序列），用于更真实的终端渲染。
        /// </summary>
        /// <param name="fragment">原始输出片段</param>
        public void AddOutputRaw(string fragment)
        {
            if (fragment == null) return;
            lock (_lockObject)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OutputLines.Add(fragment);
                });
            }
        }

        /// <summary>
        /// 清空输出
        /// </summary>
        public void ClearOutput()
        {
            lock (_lockObject)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OutputLines.Clear();
                });
            }
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        /// <param name="status">状态文本</param>
        /// <param name="isRunning">是否运行中</param>
        public void UpdateStatus(string status, bool isRunning)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Status = status;
                IsRunning = isRunning;
            });
        }
    }
}