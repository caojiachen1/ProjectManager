using System.Diagnostics;
using System.Text.RegularExpressions;
using ProjectManager.Models;

namespace ProjectManager.Services
{
    public interface IGitService
    {
        Task<GitInfo> GetGitInfoAsync(string projectPath);
        Task<bool> InitializeRepositoryAsync(string projectPath);
        Task<bool> AddAllAsync(string projectPath);
        Task<bool> CommitAsync(string projectPath, string message);
        Task<bool> PushAsync(string projectPath);
        Task<bool> PullAsync(string projectPath);
        Task<bool> CreateBranchAsync(string projectPath, string branchName);
        Task<bool> SwitchBranchAsync(string projectPath, string branchName);
        Task<List<string>> GetBranchesAsync(string projectPath);
        Task<string> GetRemoteUrlAsync(string projectPath);
        Task<bool> SetRemoteUrlAsync(string projectPath, string remoteUrl);
        Task<(bool IsSuccess, string ErrorMessage)> CloneRepositoryAsync(string remoteUrl, string localPath, IProgress<string>? progress = null);
        Task<bool> IsValidGitUrlAsync(string url);
        Task<string> GetRepositoryNameFromUrlAsync(string url);
    }

    public class GitService : IGitService
    {
        /// <summary>
        /// 获取项目的Git信息
        /// </summary>
        public async Task<GitInfo> GetGitInfoAsync(string projectPath)
        {
            var gitInfo = new GitInfo();

            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return gitInfo;

            try
            {
                // 检查是否是Git仓库
                var gitDir = Path.Combine(projectPath, ".git");
                if (!Directory.Exists(gitDir))
                    return gitInfo;

                gitInfo.IsGitRepository = true;

                // 获取当前分支
                gitInfo.CurrentBranch = await GetCurrentBranchAsync(projectPath);

                // 获取所有分支
                gitInfo.Branches = await GetBranchesAsync(projectPath);

                // 获取远程URL
                gitInfo.RemoteUrl = await GetRemoteUrlAsync(projectPath);

                // 获取状态信息
                await UpdateGitStatusAsync(projectPath, gitInfo);

                // 获取最后一次提交信息
                await UpdateLastCommitInfoAsync(projectPath, gitInfo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取Git信息失败: {ex.Message}");
            }

            return gitInfo;
        }

        /// <summary>
        /// 初始化Git仓库
        /// </summary>
        public async Task<bool> InitializeRepositoryAsync(string projectPath)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, "init");
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 添加所有文件到暂存区
        /// </summary>
        public async Task<bool> AddAllAsync(string projectPath)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, "add .");
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 提交更改
        /// </summary>
        public async Task<bool> CommitAsync(string projectPath, string message)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, $"commit -m \"{message}\"");
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 推送到远程仓库
        /// </summary>
        public async Task<bool> PushAsync(string projectPath)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, "push");
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从远程仓库拉取
        /// </summary>
        public async Task<bool> PullAsync(string projectPath)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, "pull");
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 创建新分支
        /// </summary>
        public async Task<bool> CreateBranchAsync(string projectPath, string branchName)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, $"checkout -b {branchName}");
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 切换分支
        /// </summary>
        public async Task<bool> SwitchBranchAsync(string projectPath, string branchName)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, $"checkout {branchName}");
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取所有分支
        /// </summary>
        public async Task<List<string>> GetBranchesAsync(string projectPath)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, "branch -a");
                if (result.IsSuccess && !string.IsNullOrEmpty(result.Output))
                {
                    return result.Output
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(b => b.Trim().TrimStart('*').Trim())
                        .Where(b => !string.IsNullOrEmpty(b))
                        .ToList();
                }
            }
            catch { }

            return new List<string>();
        }

        /// <summary>
        /// 获取远程URL
        /// </summary>
        public async Task<string> GetRemoteUrlAsync(string projectPath)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, "remote get-url origin");
                return result.IsSuccess ? result.Output.Trim() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 设置远程URL
        /// </summary>
        public async Task<bool> SetRemoteUrlAsync(string projectPath, string remoteUrl)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, $"remote set-url origin {remoteUrl}");
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 克隆远程仓库到本地
        /// </summary>
        public async Task<(bool IsSuccess, string ErrorMessage)> CloneRepositoryAsync(string remoteUrl, string localPath, IProgress<string>? progress = null)
        {
            try
            {
                // 确保本地路径存在
                var parentDir = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                // 如果目标目录已存在且不为空，返回错误
                if (Directory.Exists(localPath) && Directory.GetFileSystemEntries(localPath).Length > 0)
                {
                    return (false, "目标目录已存在且不为空");
                }

                progress?.Report("开始克隆仓库...");

                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"clone \"{remoteUrl}\" \"{localPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var output = new List<string>();
                var error = new List<string>();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.Add(e.Data);
                        progress?.Report(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.Add(e.Data);
                        progress?.Report(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    progress?.Report("克隆完成！");
                    return (true, string.Empty);
                }
                else
                {
                    var errorMessage = string.Join("\n", error);
                    return (false, string.IsNullOrEmpty(errorMessage) ? "克隆失败" : errorMessage);
                }
            }
            catch (Exception ex)
            {
                return (false, $"克隆过程中发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证Git URL是否有效
        /// </summary>
        public async Task<bool> IsValidGitUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                // 检查URL格式
                var gitUrlPatterns = new[]
                {
                    @"^https://github\.com/[\w\-\.]+/[\w\-\.]+(?:\.git)?/?$",
                    @"^git@github\.com:[\w\-\.]+/[\w\-\.]+(?:\.git)?$",
                    @"^https://gitlab\.com/[\w\-\.]+/[\w\-\.]+(?:\.git)?/?$",
                    @"^git@gitlab\.com:[\w\-\.]+/[\w\-\.]+(?:\.git)?$",
                    @"^https://bitbucket\.org/[\w\-\.]+/[\w\-\.]+(?:\.git)?/?$",
                    @"^git@bitbucket\.org:[\w\-\.]+/[\w\-\.]+(?:\.git)?$",
                    @"^https://.*\.git$",
                    @"^git@.*:.*\.git$"
                };

                var isValidFormat = gitUrlPatterns.Any(pattern => Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase));
                if (!isValidFormat)
                    return false;

                // 尝试执行 git ls-remote 来验证仓库是否可访问
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"ls-remote --heads \"{url}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();
                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从Git URL中提取仓库名称
        /// </summary>
        public Task<string> GetRepositoryNameFromUrlAsync(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return Task.FromResult(string.Empty);

                // 移除 .git 后缀
                var cleanUrl = url.EndsWith(".git") ? url[..^4] : url;
                
                // 提取最后一部分作为仓库名
                var parts = cleanUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    var repoName = parts[^1];
                    // 如果是SSH格式，可能包含冒号
                    if (repoName.Contains(':'))
                    {
                        repoName = repoName.Split(':')[^1];
                    }
                    return Task.FromResult(repoName);
                }

                return Task.FromResult(string.Empty);
            }
            catch
            {
                return Task.FromResult(string.Empty);
            }
        }

        #region Private Methods

        private async Task<string> GetCurrentBranchAsync(string projectPath)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, "branch --show-current");
                return result.IsSuccess ? result.Output.Trim() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task UpdateGitStatusAsync(string projectPath, GitInfo gitInfo)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, "status --porcelain");
                if (result.IsSuccess)
                {
                    var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    gitInfo.UncommittedChanges = lines.Length;

                    if (lines.Length == 0)
                    {
                        gitInfo.Status = GitStatus.Clean;
                    }
                    else if (lines.Any(l => l.StartsWith("UU") || l.StartsWith("AA")))
                    {
                        gitInfo.Status = GitStatus.Conflicted;
                    }
                    else if (lines.Any(l => l[0] != ' ' && l[0] != '?'))
                    {
                        gitInfo.Status = GitStatus.Staged;
                    }
                    else if (lines.Any(l => l.StartsWith("??")))
                    {
                        gitInfo.Status = GitStatus.Untracked;
                    }
                    else
                    {
                        gitInfo.Status = GitStatus.Modified;
                    }
                }

                // 获取未推送的提交数量
                var unpushedResult = await RunGitCommandAsync(projectPath, "log @{u}..HEAD --oneline");
                if (unpushedResult.IsSuccess)
                {
                    gitInfo.UnpushedCommits = unpushedResult.Output
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
                }
            }
            catch { }
        }

        private async Task UpdateLastCommitInfoAsync(string projectPath, GitInfo gitInfo)
        {
            try
            {
                var result = await RunGitCommandAsync(projectPath, "log -1 --pretty=format:\"%H|%s|%an|%ad\" --date=iso");
                if (result.IsSuccess && !string.IsNullOrEmpty(result.Output))
                {
                    var parts = result.Output.Split('|');
                    if (parts.Length >= 4)
                    {
                        gitInfo.LastCommitMessage = parts[1];
                        gitInfo.LastCommitAuthor = parts[2];
                        
                        if (DateTime.TryParse(parts[3], out var commitDate))
                        {
                            gitInfo.LastCommitDate = commitDate;
                        }
                    }
                }
            }
            catch { }
        }

        private async Task<(bool IsSuccess, string Output)> RunGitCommandAsync(string workingDirectory, string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();

                return (process.ExitCode == 0, string.IsNullOrEmpty(error) ? output : error);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        #endregion
    }
}